using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MessagePack;

namespace DeadCellsMultiplayerX.Common.Data.Snapshot
{
    /// <summary>
    /// 每帧hero同步
    /// </summary>
    [MessagePackObject]
    public class HeroDynamic
    {
        public const uint BitPos = 1 << 0;
        public const uint BitAnim = 1 << 1;
        public const uint BitAffects = 1 << 2;
        public const uint BitLevel = 1 << 3;    //检查levelid和sublevelid是否可用

        public const uint FlagDespawn = 1 << 0;   // 销毁（进入子关卡或者以切换关卡）
        public const uint FlagRespawn = 1 << 1;   // 重新生成

        [Key(0)] public string GUID = string.Empty;
        [Key(1)] public uint ChangeMask;
        [Key(2)] public PosVector Pos = new();
        [Key(3)] public AnimInfo Anim = new();
        [Key(5)] public List<AffectEntry> Affects = [];
        [Key(6)] public uint Flags;
        [Key(7)] public string LevelId = string.Empty;
        [Key(8)] public int SubLevelIndex = -1;

        public const uint BitAll = BitPos | BitAnim | BitAffects | BitLevel;


        [IgnoreMember]
        public bool HasPos => (ChangeMask & BitPos) != 0;

        [IgnoreMember]
        public bool HasAnim => (ChangeMask & BitAnim) != 0;

        [IgnoreMember]
        public bool HasAffects => (ChangeMask & BitAffects) != 0;

        [IgnoreMember] 
        public bool NeedDespawn => (Flags & FlagDespawn) != 0;

        [IgnoreMember] 
        public bool NeedRespawn => (Flags & FlagRespawn) != 0;

        [IgnoreMember] 
        public bool HasLevel => (ChangeMask & BitLevel) != 0;
    }
}