using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using dc.en;
using DeadCellsMultiplayerX.Client.Guest.WorldX.Snapshot;
using DeadCellsMultiplayerX.Common.Data;
using DeadCellsMultiplayerX.Common.Data.Snapshot;
using DeadCellsMultiplayerX.Common.Serializers;
using Serilog;

namespace DeadCellsMultiplayerX.Client.Guest.WorldX.Remote
{
    /// <summary>
    /// 客户端怪物状态机管理
    /// 由 Mob.fixedUpdate Hook 驱动 收集状态变化的远端怪物并上报服务端
    /// </summary>
    internal sealed class MobAffectManager
    {
        private const int MaxMobsPerReport = 256;
        private const int MaxAffectsPerMob = 256;
        private const long SendIntervalMs = 50;
        private const long RefreshIntervalMs = 1000;

        private static readonly ILogger Logger = Log.ForContext("SourceContext", "MobAffectManager");

        private readonly GuestClientSession session;
        private readonly ClientMobReceiver receiver;
        private readonly WorldFrameManager frames;

        private readonly Dictionary<uint, List<AffectEntry>> current = [];
        private readonly Dictionary<uint, List<AffectEntry>> lastSent = [];
        private readonly Dictionary<uint, List<AffectEntry>> pending = [];

        /// <summary>
        /// 各怪物 affect 上报静默截止时间 单位为同步秒
        /// </summary>
        private readonly Dictionary<uint, double> suppressUntil = [];

        private long nextSequence;
        private long lastSendMs;
        private long lastRefreshMs;
        private Task? sendTask;

        public MobAffectManager(
            GuestClientSession session,
            ClientMobReceiver receiver,
            WorldFrameManager frames)
        {
            this.session = session;
            this.receiver = receiver;
            this.frames = frames;
        }

        /// <summary>
        /// Hook 回调 收集单个远端怪物的当前状态
        /// </summary>
        public void OnMobFixedUpdate(Mob mob)
        {
            if (mob == null || mob.destroyed) return;
            if (!receiver.TryGetNetId(mob, out uint netId)) return;

            double nowSec = session.SyncedTimeMs / 1000.0;

            // 重放命中后的窗口内不采集 避免本地 onDamage 产生的 affect 被上传
            if (suppressUntil.TryGetValue(netId, out double until))
            {
                if (nowSec < until) return;
                suppressUntil.Remove(netId);
            }

            List<AffectEntry> entries;

            entries = AffectCodec.Collect(
                mob,
                nowSec,
                mob.cd?.baseFps ?? 60.0);

            if (entries.Count > MaxAffectsPerMob)
                entries.RemoveRange(MaxAffectsPerMob, entries.Count - MaxAffectsPerMob);

            current[netId] = entries;

            if (lastSent.TryGetValue(netId, out var last))
            {
                if (SameState(last, entries)) return;
            }
            else if (entries.Count == 0) return;

            if (pending.Count >= MaxMobsPerReport && !pending.ContainsKey(netId))
                return;

            lastSent[netId] = entries;
            pending[netId] = entries;
        }

        /// <summary>
        /// Ghost 重放命中后暂停该怪物的 affect 上报
        /// </summary>
        public void SuppressReplay(Mob mob, double seconds = 1.0)
        {
            if (mob == null || !receiver.TryGetNetId(mob, out uint netId)) return;

            double nowSec = session.SyncedTimeMs / 1000.0;
            double until = nowSec + Math.Max(0.0, seconds);

            if (suppressUntil.TryGetValue(netId, out double existing) && existing > until)
                until = existing;

            suppressUntil[netId] = until;

            // 不再重发重放前的旧状态 避免覆盖服务端的权威 affect
            current.Remove(netId);
            lastSent.Remove(netId);
            pending.Remove(netId);
        }

        /// <summary>
        /// 每帧推进 周期刷新与发送
        /// </summary>
        public void Tick(long nowMs)
        {
            Prune();

            if (nowMs - lastRefreshMs >= RefreshIntervalMs)
            {
                lastRefreshMs = nowMs;
                foreach (var kv in current)
                {
                    // 只刷新曾经上报过的怪物 避免给所有怪物发送空快照
                    if (!lastSent.TryGetValue(kv.Key, out var last) || last.Count == 0)
                        continue;

                    lastSent[kv.Key] = kv.Value;

                    if (pending.Count < MaxMobsPerReport || pending.ContainsKey(kv.Key))
                        pending[kv.Key] = kv.Value;
                }
            }

            if (pending.Count == 0) return;

            if (sendTask != null && !sendTask.IsCompleted) return;

            if (nowMs - lastSendMs < SendIntervalMs) return;

            int epoch = frames.Epoch;
            if (epoch < 0) return;

            var report = new MobAffectReport
            {
                Epoch = epoch,
                Sequence = ++nextSequence,
                ClientTimeMs = session.SyncedTimeMs,
            };

            foreach (var kv in pending)
            {
                report.Mobs.Add(new MobAffectState
                {
                    NetId = kv.Key,
                    Affects = kv.Value,
                });

                if (report.Mobs.Count >= MaxMobsPerReport) break;
            }

            pending.Clear();
            lastSendMs = nowMs;


            sendTask = session.Server.SubmitMobAffects(report);
            _ = sendTask.ContinueWith(
                static t => _ = t.Exception,
                TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>
        /// epoch 变化或世界清理时重置
        /// </summary>
        public void Reset()
        {
            current.Clear();
            lastSent.Clear();
            pending.Clear();
            suppressUntil.Clear();
            nextSequence = 0;
            lastSendMs = 0;
            lastRefreshMs = 0;
        }

        /// <summary>
        /// 清理已销毁怪物留下的缓存
        /// </summary>
        private void Prune()
        {
            if (suppressUntil.Count > 0)
            {
                double nowSec = session.SyncedTimeMs / 1000.0;
                List<uint>? staleSuppress = null;

                foreach (var kv in suppressUntil)
                {
                    if (kv.Value > nowSec && receiver.Get(kv.Key) != null) continue;
                    (staleSuppress ??= []).Add(kv.Key);
                }

                if (staleSuppress != null)
                {
                    foreach (var netId in staleSuppress)
                        suppressUntil.Remove(netId);
                }
            }

            if (current.Count == 0) return;

            List<uint>? stale = null;
            foreach (var netId in current.Keys)
            {
                if (receiver.Get(netId) != null) continue;
                (stale ??= []).Add(netId);
            }

            if (stale == null) return;

            foreach (var netId in stale)
            {
                current.Remove(netId);
                lastSent.Remove(netId);
                pending.Remove(netId);
            }
        }

        /// <summary>
        /// 结构比较 T 会逐帧递减 不参与比较
        /// </summary>
        private static bool SameState(List<AffectEntry> a, List<AffectEntry> b)
        {
            if (a.Count != b.Count) return false;

            for (int i = 0; i < a.Count; i++)
            {
                var x = a[i];
                var y = b[i];

                if (x.OuterIndex != y.OuterIndex
                    || x.InnerIndex != y.InnerIndex
                    || x.A != y.A
                    || x.UniqId != y.UniqId
                    || x.Val != y.Val)
                    return false;

                if (Math.Abs(x.EndSec - y.EndSec) > 0.05)
                    return false;
            }

            return true;
        }
    }
}
