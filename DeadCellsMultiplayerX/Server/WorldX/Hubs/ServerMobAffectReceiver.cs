using System;
using System.Collections.Generic;
using DeadCellsMultiplayerX.Common.Data;
using DeadCellsMultiplayerX.Common.Data.Snapshot;
using DeadCellsMultiplayerX.Common.Serializers;
using Serilog;

namespace DeadCellsMultiplayerX.Server.WorldX.Hubs
{
    /// <summary>
    /// 服务端怪物状态机接收与仲裁
    /// </summary>
    internal sealed class ServerMobAffectReceiver
    {
        private const int MaxMobsPerReport = 256;
        private const int MaxAffectsPerMob = 256;

        private const int MaxOuterIndex = 256;
        private const int MaxInnerIndex = 256;

        private const long InvalidLogIntervalMs = 5000;

        private static readonly ILogger Logger =
            Log.ForContext("SourceContext", "ServerMobAffectReceiver");

        /// <summary>
        /// 单个怪物的权威状态
        /// </summary>
        private sealed class MobAffectState
        {
            /// <summary>
            /// 条目 null 表示较新上报已移除
            /// </summary>
            public readonly Dictionary<AffectKey, AffectEntry?> Entries = [];

            /// <summary>
            /// 最近一次写入的客户端时间
            /// </summary>
            public readonly Dictionary<AffectKey, double> Times = [];

            /// <summary>
            /// 上报过该条目的客户端 只有见过的客户端才能移除
            /// </summary>
            public readonly Dictionary<AffectKey, HashSet<string>> SeenBy = [];
        }

        private readonly ServerMobRegistry registry;
        private readonly Dictionary<uint, MobAffectState> states = [];
        private readonly Dictionary<string, long> lastSequence = [];
        private readonly Dictionary<string, long> lastInvalidLogMs = [];
        private readonly List<AffectEntry> active = [];

        public int Epoch { get; private set; } = -1;

        public ServerMobAffectReceiver(ServerMobRegistry registry)
        {
            this.registry = registry;
        }

        /// <summary>
        /// 进入新的世界 epoch
        /// </summary>
        public void BeginEpoch(int epoch)
        {
            Epoch = epoch;
            states.Clear();
            lastSequence.Clear();
            lastInvalidLogMs.Clear();
        }

        public void Clear()
        {
            states.Clear();
            lastSequence.Clear();
            lastInvalidLogMs.Clear();
        }

        /// <summary>
        /// 接收单个客户端上报
        /// </summary>
        public void OnReport(string senderGuid, MobAffectReport? report)
        {
            if (string.IsNullOrEmpty(senderGuid) || report == null) return;
            if (report.Epoch != Epoch) return;
            if (!double.IsFinite(report.ClientTimeMs)) return;
            if (lastSequence.TryGetValue(senderGuid, out long last) && report.Sequence <= last)
                return;

            lastSequence[senderGuid] = report.Sequence;

            int mobCount = Math.Min(report.Mobs?.Count ?? 0, MaxMobsPerReport);
            for (int i = 0; i < mobCount; i++)
            {
                var mobState = report.Mobs![i];
                if (mobState == null || mobState.NetId == 0) continue;
                if (!registry.TryGet(mobState.NetId, out var mob) || mob == null || mob.destroyed)
                    continue;

                if (!states.TryGetValue(mobState.NetId, out var state))
                {
                    state = new MobAffectState();
                    states[mobState.NetId] = state;
                }

                var seen = new HashSet<AffectKey>();
                int count = Math.Min(mobState.Affects?.Count ?? 0, MaxAffectsPerMob);
                for (int j = 0; j < count; j++)
                {
                    var entry = mobState.Affects![j];
                    if (entry == null) continue;

                    var key = entry.Key;
                    seen.Add(key);

                    if (!IsValid(entry))
                    {
                        LogInvalid(senderGuid, entry);
                        continue;
                    }

                    if (!state.SeenBy.TryGetValue(key, out var clients))
                    {
                        clients = [];
                        state.SeenBy[key] = clients;
                    }
                    clients.Add(senderGuid);

                    if (state.Times.TryGetValue(key, out double t)
                        && report.ClientTimeMs < t)
                        continue;

                    state.Entries[key] = Clone(entry);
                    state.Times[key] = report.ClientTimeMs;
                }

                List<AffectKey>? removals = null;
                foreach (var kv in state.Entries)
                {
                    if (kv.Value == null || seen.Contains(kv.Key)) continue;
                    if (!state.SeenBy.TryGetValue(kv.Key, out var clients)
                        || !clients.Contains(senderGuid))
                        continue;
                    if (state.Times.TryGetValue(kv.Key, out double t)
                        && report.ClientTimeMs < t)
                        continue;

                    (removals ??= []).Add(kv.Key);
                }

                if (removals == null) continue;

                foreach (var key in removals)
                {
                    state.Entries[key] = null;
                    state.Times[key] = report.ClientTimeMs;
                }
            }
        }

        /// <summary>
        /// 把权威状态应用到服务端怪物
        /// </summary>
        public void ApplyTo(ServerMobEntry entry, long serverTimeMs)
        {
            if (entry?.Mob == null || entry.Mob.destroyed) return;
            if (!states.TryGetValue(entry.NetId, out var state)) return;

            double sec = serverTimeMs / 1000.0;
            active.Clear();

            foreach (var kv in state.Entries)
            {
                var e = kv.Value;
                if (e == null) continue;
                if (e.StartSec <= sec && sec < e.EndSec)
                    active.Add(e);
            }

            if (active.Count > 0)
                AffectCodec.Apply(entry.Mob, active, sec);
        }

        /// <summary>
        /// 合并服务端原生效果与客户端权威状态
        /// 权威活动条目覆盖服务端条目 墓碑屏蔽服务端条目
        /// </summary>
        public List<AffectEntry> MergeActive(
            uint netId,
            double serverSec,
            List<AffectEntry> serverEntries)
        {
            var merged = new Dictionary<AffectKey, AffectEntry>();
            if (serverEntries != null)
            {
                foreach (var e in serverEntries)
                {
                    if (e == null) continue;
                    merged[e.Key] = e;
                }
            }

            if (states.TryGetValue(netId, out var state))
            {
                foreach (var kv in state.Entries)
                {
                    var e = kv.Value;
                    if (e == null)
                    {
                        // 客户端显式移除的条目 不再补采
                        merged.Remove(kv.Key);
                        continue;
                    }

                    if (e.StartSec <= serverSec && serverSec < e.EndSec)
                        merged[e.Key] = e;
                }
            }

            return [.. merged.Values];
        }

        /// <summary>
        /// 怪物注销时清理权威状态
        /// </summary>
        public void Remove(uint netId) => states.Remove(netId);

        private void LogInvalid(string senderGuid, AffectEntry entry)
        {
            long now = Environment.TickCount64;
            if (lastInvalidLogMs.TryGetValue(senderGuid, out long last)
                && now - last < InvalidLogIntervalMs)
                return;

            lastInvalidLogMs[senderGuid] = now;
            Logger.Warning(
                "Invalid affect entry from {Guid}: a={A} uniq={UniqId} outer={OuterIndex} " +
                "inner={InnerIndex} t={T} val={Val} start={StartSec} end={EndSec}",
                senderGuid, entry.A, entry.UniqId, entry.OuterIndex, entry.InnerIndex,
                entry.T, entry.Val, entry.StartSec, entry.EndSec);
        }

        private static bool IsValid(AffectEntry? entry)
        {
            if (entry == null) return false;
            if (entry.A < 0 || entry.A >= MaxOuterIndex) return false;
            if (entry.UniqId < 0) return false;
            if (entry.OuterIndex < 0 || entry.OuterIndex >= MaxOuterIndex) return false;
            if (entry.InnerIndex < 0 || entry.InnerIndex >= MaxInnerIndex) return false;
            if (!double.IsFinite(entry.T) || !double.IsFinite(entry.Val)) return false;
            if (!double.IsFinite(entry.StartSec) || !double.IsFinite(entry.EndSec)) return false;
            return entry.EndSec > entry.StartSec;
        }

        private static AffectEntry Clone(AffectEntry source)
        {
            return new AffectEntry
            {
                OuterIndex = source.OuterIndex,
                InnerIndex = source.InnerIndex,
                A = source.A,
                T = source.T,
                UniqId = source.UniqId,
                Val = source.Val,
                StartSec = source.StartSec,
                EndSec = source.EndSec,
            };
        }
    }
}
