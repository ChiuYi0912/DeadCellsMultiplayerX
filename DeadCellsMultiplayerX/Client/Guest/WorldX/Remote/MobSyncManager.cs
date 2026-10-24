using System;
using System.Collections.Generic;
using dc.en;
using DeadCellsMultiplayerX.Client.Guest.WorldX.Snapshot;
using DeadCellsMultiplayerX.Common.Data.Snapshot;
using DeadCellsMultiplayerX.Common.Tools;
using Serilog;

namespace DeadCellsMultiplayerX.Client.Guest.WorldX.Remote
{
    /// <summary>
    /// 客户端怪物同步装置
    /// 消费世界帧中的怪物数据并驱动真实 Mob 表现
    /// </summary>
    internal sealed class MobSyncManager
    {
        private static readonly ILogger Logger = Log.ForContext("SourceContext", "MobSyncManager");

        private readonly GuestClientSession session;
        private readonly WorldFrameManager frames;
        private readonly ClientMobReceiver receiver;
        private readonly MobAffectStateManager affectState;

        /// <summary>
        /// 记录mobid
        /// </summary>
        private readonly HashSet<uint> frameNetIds = [];

        private int lastEpoch = -1;
        private bool loggedFirstMob;

        public MobSyncManager(
            GuestClientSession session,
            RemoteWorld remoteWorld,
            WorldFrameManager frames)
        {
            this.session = session;
            this.frames = frames;
            receiver = new ClientMobReceiver(session, remoteWorld);
            affectState = new MobAffectStateManager();
        }

        public ClientMobReceiver Receiver => receiver;

        public bool IsRemoteMob(Mob mob) => receiver.IsRemoteMob(mob);

        public bool IsDeathSuppressed(Mob mob) => receiver.IsDeathSuppressed(mob);

        /// <summary>
        /// 应用一个连续世界帧中的怪物数据
        /// </summary>
        public void ApplyFrame(WorldFrame frame)
        {
            if (frame == null) return;

            // epoch 变化时清空旧怪物与缓存
            if (frame.Epoch != lastEpoch)
            {
                lastEpoch = frame.Epoch;
                receiver.Clear();
                affectState.Reset();
            }

            receiver.RetryPending();

            if (frame.SpawnedMobs != null)
            {
                foreach (var spawn in frame.SpawnedMobs)
                    receiver.OnSpawned(spawn);
            }

            if (frame.DespawnedMobs != null)
            {
                foreach (var netId in frame.DespawnedMobs)
                {
                    receiver.Remove(netId);
                    affectState.Remove(netId);
                }
            }

            // 以服务端当前帧的集合为准清理本地多余怪物
            frameNetIds.Clear();
            if (frame.Mobs != null)
            {
                foreach (var mob in frame.Mobs)
                {
                    if (mob == null) continue;
                    frameNetIds.Add(mob.NetId);

                    // 有动态状态但没有本地实体也没有待生成数据时请求重同步
                    if (!receiver.HasOrPending(mob.NetId))
                        frames.RequestResync("mob spawn missing");
                }
            }

            var localMobs = new List<Mob>(receiver.All);
            foreach (var mob in localMobs)
            {
                if (mob == null || mob.destroyed) continue;
                if (!receiver.TryGetNetId(mob, out var netId)) continue;
                if (!frameNetIds.Contains(netId))
                {
                    receiver.Remove(netId);
                    affectState.Remove(netId);
                }
            }

            if (!loggedFirstMob && frame.Mobs != null && frame.Mobs.Count > 0)
            {
                loggedFirstMob = true;
                Logger.Information(
                    "First mob frame: epoch={Epoch} seq={Sequence} mobs={Mobs}",
                    frame.Epoch, frame.Sequence, frame.Mobs.Count);
            }
        }

        /// <summary>
        /// 同步mobs循环
        /// </summary>
        /// <param name="nowMs"></param>
        public void Tick(long nowMs)
        {
            receiver.RetryPending();

            double nowSec = session.SyncedTimeMs / 1000.0;

            foreach (var mob in receiver.All)
            {
                if (mob == null || mob.destroyed) continue;
                if (!receiver.TryGetNetId(mob, out var netId)) continue;

                if (!frames.TryGetMobBracket(
                        netId, nowSec,
                        out var prev, out var next,
                        out double alpha, out double renderTimeSec))
                {
                    affectState.ExpireIfDue(netId, mob, nowSec);
                    continue;
                }

                RemoteHeroMotion.Apply(mob, prev.Pos, next.Pos, alpha);
                RemoteMobAnimator.Apply(mob, prev.Anim, next.Anim, renderTimeSec);

                // next 是更新的权威快照 空列表也代表服务端已清空
                // affect 使用同步时间校准 不能用落后的动画插值时间
                affectState.ApplySnapshot(
                    netId, mob, next.Affects ?? prev.Affects, nowSec);

                int life = (int)Math.Round(prev.Life + (next.Life - prev.Life) * alpha);
                mob.life = life;
                mob.maxLife = next.MaxLife;
            }
        }

        public void Clear()
        {
            receiver.Clear();
            affectState.Reset();
        }
    }
}
