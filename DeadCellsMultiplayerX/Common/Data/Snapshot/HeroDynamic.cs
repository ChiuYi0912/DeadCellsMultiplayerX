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

        [Key(0)] public string GUID = string.Empty;
        [Key(1)] public uint ChangeMask;
        [Key(2)] public PosVector Pos = new();
        [Key(3)] public AnimInfo Anim = new();
        [Key(5)] public List<AffectEntry> Affects = [];
        public const uint BitAll       = BitPos | BitAnim  | BitAffects;

        [IgnoreMember]
        public bool HasPos => (ChangeMask & BitPos) != 0;

        [IgnoreMember]
        public bool HasAnim => (ChangeMask & BitAnim) != 0;

        [IgnoreMember]
        public bool HasAffects => (ChangeMask & BitAffects) != 0;
    }
}