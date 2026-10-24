using System.Collections.Generic;
using MessagePack;

namespace DeadCellsMultiplayerX.Common.Data.Snapshot
{
    /// <summary>
    /// 单个怪物的状态机快照
    /// </summary>
    [MessagePackObject]
    public class MobAffectState
    {
        /// <summary>
        /// 网络实体 id
        /// </summary>
        [Key(0)] public uint NetId { get; set; }

        /// <summary>
        /// 状态机数据
        /// </summary>
        [Key(1)] public List<AffectEntry> Affects { get; set; } = [];
    }

    /// <summary>
    /// 客户端上报的怪物状态机变化
    /// </summary>
    [MessagePackObject]
    public class MobAffectReport
    {
        /// <summary>
        /// 回显最近观测到的服务端 epoch
        /// </summary>
        [Key(0)] public int Epoch { get; set; }

        /// <summary>
        /// 每个客户端独立 单调递增
        /// </summary>
        [Key(1)] public long Sequence { get; set; }

        /// <summary>
        /// 上报时的同步时间
        /// </summary>
        [Key(2)] public double ClientTimeMs { get; set; }

        /// <summary>
        /// 发生变化的怪物状态
        /// </summary>
        [Key(3)] public List<MobAffectState> Mobs { get; set; } = [];
    }
}
