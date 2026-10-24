using System;
using System.Collections.Generic;
using dc.en;
using DeadCellsMultiplayerX.Common.Data;
using DeadCellsMultiplayerX.Common.Serializers;

namespace DeadCellsMultiplayerX.Client.Guest.WorldX.Remote
{
    /// <summary>
    /// 客户端怪物状态机接收
    /// 按渲染同步时间校验并写入远端怪物
    /// </summary>
    internal sealed class MobAffectReceiver
    {
        private const int MaxEntries = 256;

        /// <summary>
        /// affects状态索引
        /// </summary>
        private const int MaxOuterIndex = 256;
        private const int MaxInnerIndex = 256;

        /// <summary>
        /// 当前所渲染的effect
        /// </summary>
        private readonly List<AffectEntry> active = [];

        /// <summary>
        /// 应用渲染时间落在effect内的条目
        /// </summary>
        public void Apply(Mob mob, List<AffectEntry>? entries, double renderTimeSec)
        {
            if (mob == null || mob.destroyed) return;
            if (entries == null || entries.Count == 0) return;
            if (!double.IsFinite(renderTimeSec)) return;

            active.Clear();

            int count = Math.Min(entries.Count, MaxEntries);
            for (int i = 0; i < count; i++)
            {
                var e = entries[i];
                if (e == null) continue;
                if (e.OuterIndex < 0 || e.OuterIndex >= MaxOuterIndex) continue;
                if (e.InnerIndex < 0 || e.InnerIndex >= MaxInnerIndex) continue;
                if (!double.IsFinite(e.T) || !double.IsFinite(e.Val)) continue;
                if (!double.IsFinite(e.StartSec) || !double.IsFinite(e.EndSec)) continue;
                if (e.EndSec <= e.StartSec) continue;
                if (renderTimeSec < e.StartSec || renderTimeSec >= e.EndSec) continue;

                active.Add(e);
            }

            if (active.Count > 0)
                AffectCodec.Apply(mob, active, renderTimeSec);
        }
    }
}
