using System.Runtime.CompilerServices;
using dc;
using dc.en;
using dc.hl.types;
using DeadCellsMultiplayerX.Common.Data;
using Hashlink.Proxy;
using Hashlink.Virtuals;
using HaxeProxy.Runtime;
using ModCore.Utilities;

namespace DeadCellsMultiplayerX.Common.Serializers
{
    public static class AffectCodec
    {
        private const int MaxOuterIndex = 256;

        private static readonly ConditionalWeakTable<Entity, Dictionary<AffectKey, double>> startCache = new();

        public static List<AffectEntry> Collect(Entity hero, double snapSec, double baseFps)
        {
            var result = new List<AffectEntry>();
            var affects = hero.affects;
            if (affects == null) return result;

            if (!startCache.TryGetValue(hero, out var t0map))
            {
                t0map = new Dictionary<AffectKey, double>();
                startCache.Add(hero, t0map);
            }

            var seen = new HashSet<AffectKey>();

            for (int i = 0; i < affects.length; i++)
            {
                // 数组元素是 HashlinkSharp 的原始 HashlinkObject 代理 需要转成强类型代理
                var inner = ToObject<ArrayObj>(affects.array[i]);

                if (inner == null) continue;

                for (int j = 0; j < inner.length; j++)
                {
                    var v = ToVirtual<virtual_a_t_uniqId_val_>(inner.array[j]);

                    if (v == null) continue;

                    var key = new AffectKey(v.a, v.uniqId, v.val);
                    seen.Add(key);

                    if (!t0map.TryGetValue(key, out double t0))
                    {
                        t0 = snapSec;
                        t0map[key] = t0;
                    }

                    result.Add(new AffectEntry
                    {
                        OuterIndex = i,
                        InnerIndex = j,
                        A = v.a,
                        T = v.t,
                        UniqId = v.uniqId,
                        Val = v.val,
                        StartSec = t0,
                        EndSec = t0 + v.t / baseFps,
                    });
                }
            }

            // 清理
            var stale = new List<AffectKey>();
            foreach (var k in t0map.Keys)
                if (!seen.Contains(k)) stale.Add(k);
            foreach (var k in stale) t0map.Remove(k);

            return result;
        }


        /// <summary>
        /// 把数组中的原始 HashlinkObject 转成强类型对象代理
        /// </summary>
        public static T? ToObject<T>(object? value) where T : HaxeObject
        {
            if (value is T typed) return typed;

            if (value is HashlinkObj obj)
            {
                try
                {
                    return obj.AsHaxe<T>();
                }
                catch (Exception ex) when (
                    ex is InvalidCastException or NotSupportedException or InvalidOperationException)
                {
                    return null;
                }
            }

            return null;
        }

        /// <summary>
        /// 把数组中的原始 HashlinkObject 转成强类型 Virtual 代理
        /// </summary>
        public static T? ToVirtual<T>(object? value) where T : HaxeVirtual
        {
            if (value is T typed) return typed;

            if (value is HashlinkObj obj)
            {
                try
                {
                    return obj.AsHaxe().ToVirtual<T>();
                }
                catch (Exception ex) when (
                    ex is InvalidCastException or NotSupportedException or InvalidOperationException)
                {
                    return null;
                }
            }

            return null;
        }

        public static void Apply(Entity hero, List<AffectEntry> entries, double renderSec)
        {
            if (entries == null || entries.Count == 0) return;

            var affects = hero.affects;
            if (affects == null) return;

            // 注入条目的 uniqId 也要占用本地序列 否则后续 setAffectS 会撞号
            int nextUniqId = hero.lastUniqId;

            foreach (var e in entries)
            {
                if (e.OuterIndex < 0 || e.OuterIndex >= MaxOuterIndex)
                    continue;

                if (e.UniqId >= nextUniqId)
                    nextUniqId = e.UniqId == int.MaxValue ? int.MaxValue : e.UniqId + 1;

                if (renderSec < e.StartSec || renderSec >= e.EndSec)
                    continue;

                while (affects.length <= e.OuterIndex)
                    affects.push((ArrayObj)ArrayUtils.CreateDyn().array);

                var inner = ToObject<ArrayObj>(affects.array[e.OuterIndex]);
                if (inner == null)
                {
                    inner = (ArrayObj)ArrayUtils.CreateDyn().array;
                    affects.array[e.OuterIndex] = inner;
                }
                else
                {
                    inner = CompactNulls(affects, e.OuterIndex, inner);
                }

                var value = new virtual_a_t_uniqId_val_
                {
                    a = e.A,
                    t = e.T,
                    uniqId = e.UniqId,
                    val = e.Val
                };

                // 不能按 InnerIndex 用 null 占位 游戏 preUpdate 会直接解引用 .t
                int slot = FindSlot(inner, e);
                if (slot >= 0)
                    inner.array[slot] = value;
                else
                    inner.push(value);
            }

            if (nextUniqId > hero.lastUniqId)
                hero.lastUniqId = nextUniqId;
        }

        /// <summary>
        /// 清理旧版本用 null 占位留下的空洞
        /// </summary>
        private static ArrayObj CompactNulls(ArrayObj affects, int outerIndex, ArrayObj inner)
        {
            bool hasNull = false;
            for (int i = 0; i < inner.length; i++)
            {
                if (ToVirtual<virtual_a_t_uniqId_val_>(inner.array[i]) == null)
                {
                    hasNull = true;
                    break;
                }
            }

            if (!hasNull) return inner;

            var rebuilt = (ArrayObj)ArrayUtils.CreateDyn().array;
            for (int i = 0; i < inner.length; i++)
            {
                var v = ToVirtual<virtual_a_t_uniqId_val_>(inner.array[i]);
                if (v != null) rebuilt.push(v);
            }

            affects.array[outerIndex] = rebuilt;
            return rebuilt;
        }

        private static int FindSlot(ArrayObj inner, AffectEntry e)
        {
            for (int i = 0; i < inner.length; i++)
            {
                var v = ToVirtual<virtual_a_t_uniqId_val_>(inner.array[i]);
                if (v != null
                    && v.a == e.A
                    && v.uniqId == e.UniqId
                    && v.val == e.Val)
                    return i;
            }

            return -1;
        }
    }
}