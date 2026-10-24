using System;
using System.Collections.Generic;
using DeadCellsMultiplayerX.Common.Data;
using DeadCellsMultiplayerX.Common.Data.Snapshot;
using DeadCellsMultiplayerX.Common.Serializers;
using DeadCellsMultiplayerX.Utils;
using Serilog;
using LevelMob = dc.level.Mob;

namespace DeadCellsMultiplayerX.Server.WorldX.Hubs
{
    /// <summary>
    /// 一次发布产生的怪物数据
    /// </summary>
    internal sealed class MobPublishResult
    {
        /// <summary>
        /// 新生成怪物
        /// </summary>
        public List<MobSpawn> Spawned { get; } = [];

        /// <summary>
        /// 当前存活怪物状态
        /// </summary>
        public List<MobDynamic> Mobs { get; } = [];

        /// <summary>
        /// 本帧移除的怪物
        /// </summary>
        public List<uint> Despawned { get; } = [];
    }

    /// <summary>
    /// 服务端怪物状态发布器
    /// 负责首次实体序列化 与每 tick 的位置 动画 生命状态采集
    /// </summary>
    internal sealed class ServerMobPublisher
    {
        private static readonly ILogger Logger = Log.ForContext("SourceContext", "ServerMobPublisher");

        private readonly ServerMobRegistry registry;
        private readonly ServerMobAffectReceiver mobAffects;

        public ServerMobPublisher(
            ServerMobRegistry registry,
            ServerMobAffectReceiver mobAffects)
        {
            this.registry = registry;
            this.mobAffects = mobAffects;
        }

        /// <summary>
        /// 采集本 tick 的生成 状态 与移除数据
        /// </summary>
        public MobPublishResult Collect(long serverTimeMs)
        {
            var result = new MobPublishResult();

            foreach (var entry in registry.Entries)
            {
                var mob = entry.Mob;
                if (mob == null || mob.destroyed) continue;

                if (!entry.SpawnSerialized)
                {
                    if (entry.Descriptor == null)
                    {
                        // 动态创建的怪物没有关卡描述 暂时无法生成
                        continue;
                    }

                    // 等待实体初始化完成后再序列化
                    if (!mob.initDone || mob.spr == null) continue;

                    entry.Spawn.EntityData =
                        DCMXSerializers.MessagePack.Serialize<LevelMob>(entry.Descriptor);

                    registry.UpdateLevel(entry, mob._level);
                    entry.SpawnSerialized = true;

                    Logger.Verbose(
                        "Mob spawn serialized: netId={NetId} level={LevelId}#{SubLevelIndex} bytes={Bytes}",
                        entry.NetId, entry.Spawn.LevelId, entry.Spawn.SubLevelIndex,
                        entry.Spawn.EntityData.Length);
                }

                if (!entry.SpawnBroadcast)
                {
                    result.Spawned.Add(entry.Spawn);
                    entry.SpawnBroadcast = true;
                }

                mobAffects.ApplyTo(entry, serverTimeMs);
                result.Mobs.Add(BuildDynamic(entry, serverTimeMs));
            }

            var removed = registry.DrainRemoved();
            result.Despawned.AddRange(removed);
            foreach (uint netId in removed)
                mobAffects.Remove(netId);

            return result;
        }

        /// <summary>
        /// 构建全量重同步数据
        /// </summary>
        public MobPublishResult BuildResync(long serverTimeMs)
        {
            var result = new MobPublishResult();

            foreach (var entry in registry.Entries)
            {
                if (!entry.SpawnSerialized) continue;
                if (entry.Mob == null || entry.Mob.destroyed) continue;

                mobAffects.ApplyTo(entry, serverTimeMs);
                result.Spawned.Add(entry.Spawn);
                result.Mobs.Add(BuildDynamic(entry, serverTimeMs));
            }

            return result;
        }

        /// <summary>
        /// 组装单个怪物的动态状态
        /// </summary>
        private MobDynamic BuildDynamic(ServerMobEntry entry, long serverTimeMs)
        {
            var mob = entry.Mob;
            double serverSec = serverTimeMs / 1000.0;

            // 服务端原生效果与客户端上报的权威状态按 (A, UniqId) 合并
            var serverEntries = AffectCodec.Collect(mob, serverSec, mob.cd?.baseFps ?? 60.0);
            var affects = mobAffects.MergeActive(entry.NetId, serverSec, serverEntries);

            var dyn = new MobDynamic
            {
                NetId = entry.NetId,
                Pos = new PosVector(
                    mob.cx, mob.cy, mob.xr, mob.yr, mob.dir,
                    mob.dx, mob.dy, mob.bdx, mob.bdy),
                Life = mob.life,
                MaxLife = mob.maxLife,
                LevelId = entry.Spawn.LevelId,
                SubLevelIndex = entry.Spawn.SubLevelIndex,
                Affects = affects,
            };

            if (mob.spr != null)
            {
                var tmp = new HeroDynamic();
                HeroUtils.FillAnimInfo(tmp, mob.spr, serverTimeMs);
                dyn.Anim = tmp.Anim;
            }

            return dyn;
        }
    }
}
