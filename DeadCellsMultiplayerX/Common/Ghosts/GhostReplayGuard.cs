using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using dc;
using dc.en;
using dc.pr;
using DeadCellsMultiplayerX.Common.Serializers;

namespace DeadCellsMultiplayerX.Common.Ghosts
{
    /// <summary>
    /// 标记由技能武器重放产生的实体
    /// </summary>
    public static class GhostReplayGuard
    {
        private static readonly ConditionalWeakTable<Entity, object> Tags = new();

        /// <summary>
        /// 当前武器重放批次内已被表现命中的 mob
        /// 避免只表现时生命不变导致 fallback 重复触发 onDamage
        /// </summary>
        [ThreadStatic]
        private static HashSet<Mob>? visualHitMobs;

        /// <summary>
        /// 开始一次武器重放的表现批次
        /// </summary>
        public static void BeginVisualBatch()
        {
            visualHitMobs = [];
        }

        /// <summary>
        /// 结束当前表现批次
        /// </summary>
        public static void EndVisualBatch()
        {
            visualHitMobs = null;
        }

        /// <summary>
        /// 记录本次批次内已经触发过受击表现的mob
        /// </summary>
        public static void MarkVisualHit(Mob? mob)
        {
            if (mob != null) visualHitMobs?.Add(mob);
        }

        /// <summary>
        /// 判断该mob在本次批次内是否已经触发过受击表现
        /// </summary>
        public static bool WasVisualHit(Mob mob)
            => visualHitMobs?.Contains(mob) == true;

        /// <summary>
        /// 标记实体来自Ghost重放
        /// </summary>
        public static void Mark(Entity? entity)
        {
            if (entity == null) return;
            Tags.GetValue(entity, static _ => new object());
        }

        /// <summary>
        /// 判断实体是否来自Ghost重放
        /// </summary>
        public static bool IsMarked(object? value)
            => value is Entity entity && Tags.TryGetValue(entity, out _);

        /// <summary>
        /// oldCount 之后新增的实体全部标记
        /// </summary>
        public static void MarkNewEntities(Level? level, int oldCount)
        {
            if (level?.entities == null) return;

            int start = System.Math.Max(0, oldCount);
            for (int i = start; i < level.entities.length; i++)
            {
                var entity = AffectCodec.ToObject<Entity>(level.entities.getDyn(i));
                Mark(entity);
            }
        }
    }
}
