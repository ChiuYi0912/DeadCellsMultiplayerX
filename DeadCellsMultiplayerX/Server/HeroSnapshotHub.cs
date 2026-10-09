using System.Collections.Generic;
using DeadCellsMultiplayerX.Common;
using DeadCellsMultiplayerX.Common.Data;
using DeadCellsMultiplayerX.Common.Data.Snapshot;

namespace DeadCellsMultiplayerX.Server
{
    /// <summary>
    /// 全局的 hero 快照缓存中心
    /// 客户端上传Upload.缓存至此,并分发
    /// </summary>
    internal class HeroSnapshotHub : DisposableEventReceiver
    {
        private class HeroCache
        {
            public HeroSpawn Static = null!;
            public HeroDynamic Pending = null!;
            public HeroDynamic? LastSent;
            public long LastClientTime;
            public uint LastFrameId;
        }

        private readonly Dictionary<string, HeroCache> heroCaches = new();
        private readonly List<HeroSpawn> pendingSpawns = new();
        private readonly List<string> pendingDespawns = new();

        /// <summary>
        /// 客户端上传
        /// </summary>
        /// <param name="upload"></param>
        public void Upload(HeroUpload upload)
        {
            var dyn = upload.Dynamic;
            if (dyn == null) return;

            if (!heroCaches.TryGetValue(dyn.GUID, out var c))
            {
                // 首次必须带 spawn
                if (upload.Spawn == null) return;

                c = new HeroCache
                {
                    Static = upload.Spawn,
                    Pending = dyn,
                    LastClientTime = upload.ClientTime,
                    LastFrameId = upload.FrameId,
                };
                heroCaches[dyn.GUID] = c;
                pendingSpawns.Add(upload.Spawn);
                return;
            }

            // 静态变化才更新
            if (upload.Spawn != null && !c.Static.SameAs(upload.Spawn))
            {
                c.Static = upload.Spawn;
                pendingSpawns.Add(upload.Spawn);
            }

            c.Pending = dyn;
            c.LastClientTime = upload.ClientTime;
            c.LastFrameId = upload.FrameId;
        }

        public void Remove(string guid)
        {
            if (heroCaches.Remove(guid))
                pendingDespawns.Add(guid);
        }

        public bool HasPending()
            => pendingSpawns.Count > 0
            || pendingDespawns.Count > 0
            || heroCaches.Count > 0;


        /// <summary>
        /// 撰写世界快照
        /// </summary>
        /// <param name="serverTimeMs"></param>
        /// <returns></returns>
        public WorldSnapshot? Flush(double serverTimeMs)
        {
            // 提前判断有没有实际变化
            bool hasSpawn = pendingSpawns.Count > 0;
            bool hasDespawn = pendingDespawns.Count > 0;

            var snap = new WorldSnapshot { ServerTime = serverTimeMs };

            if (hasSpawn)
            {
                snap.Spawned.AddRange(pendingSpawns);
                pendingSpawns.Clear();
            }
            if (hasDespawn)
            {
                snap.Despawned.AddRange(pendingDespawns);
                pendingDespawns.Clear();
            }

            foreach (var (guid, c) in heroCaches)
            {
                var dyn = BuildDynamic(guid, c);
                if (dyn != null)
                    snap.Updated.Add(dyn);
            }

            // 整帧没变化不发送
            if (snap.Spawned.Count == 0
                && snap.Updated.Count == 0
                && snap.Despawned.Count == 0)
                return null;

            return snap;
        }

        /// <summary>
        /// 撰写每帧必须的动态包
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        private static HeroDynamic? BuildDynamic(string guid, HeroCache c)
        {
            if (c.Pending == null) return null;

            uint mask = 0;

            if (c.LastSent == null || !PosEq(c.LastSent.Pos, c.Pending.Pos))
                mask |= HeroDynamic.BitPos;
            if (c.LastSent == null || !AnimEq(c.LastSent.Anim, c.Pending.Anim))
                mask |= HeroDynamic.BitAnim;
            if (c.LastSent == null || !AffectEq(c.LastSent.Affects, c.Pending.Affects))
                mask |= HeroDynamic.BitAffects;

            if (mask == 0) return null;

            var dyn = new HeroDynamic
            {
                GUID = guid,
                ChangeMask = mask,
            };

            if ((mask & HeroDynamic.BitPos) != 0) dyn.Pos = c.Pending.Pos;
            if ((mask & HeroDynamic.BitAnim) != 0) dyn.Anim = c.Pending.Anim;
            if ((mask & HeroDynamic.BitAffects) != 0) dyn.Affects = c.Pending.Affects;

            c.LastSent = dyn;
            return dyn;
        }

        //比较位置是否需要更新
        private static bool PosEq(PosVector a, PosVector b)
            => a.Packed == b.Packed && a.PackedVel == b.PackedVel;

        //比较动画是否需要更新
        private static bool AnimEq(AnimInfo a, AnimInfo b)
            => a.PackedA == b.PackedA
            && a.PackedB == b.PackedB
            && a.PackedC == b.PackedC
            && a.PackedD == b.PackedD
            && a.GroupName == b.GroupName
            && a.StartTime == b.StartTime;

        //对比effect数组是否需要更新
        private static bool AffectEq(List<AffectEntry> a, List<AffectEntry> b)
        {
            if (a == null || b == null) return a == b;
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                var x = a[i];
                var y = b[i];
                if (x.UniqId != y.UniqId || x.A != y.A || x.T != y.T
                    || x.Val != y.Val || x.OuterIndex != y.OuterIndex
                    || x.InnerIndex != y.InnerIndex) return false;
            }
            return true;
        }
    }
}