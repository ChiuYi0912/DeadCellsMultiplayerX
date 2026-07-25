using MessagePack;

namespace DeadCellsMultiplayerX.Common.WorldData
{
    [MessagePackObject]
    public class ServerSpawnMobs
    {
        [Key(0)]
        public string GUID { get; set; } = string.Empty;

        [Key(1)]
        public int SubLevelId { get; set; }

        [Key(2)]
        public string Kind { get; set; } = string.Empty;

        [Key(3)]
        public int Cx { get; set; }

        [Key(4)]
        public int Cy { get; set; }

        [Key(5)]
        public int DmgTier { get; set; }

        [Key(6)]
        public int LifeTier { get; set; }

        [Key(7)]
        public bool Elite { get; set; }
        
        [Key(8)]
        public int? Dir { get; set; }
    }
}