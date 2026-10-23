using MessagePack;

namespace DeadCellsMultiplayerX.Common.Data
{
    [MessagePackObject]
    public class AffectEntry
    {
        [Key(0)] public int OuterIndex { get; set; }   // affects[i]
        [Key(1)] public int InnerIndex { get; set; }   // affects[i][j]
        [Key(2)] public int A { get; set; }
        [Key(3)] public double T { get; set; }
        [Key(4)] public int UniqId { get; set; }
        [Key(5)] public double Val { get; set; }

        [Key(6)] public double StartSec { get; set; }   // 首次出现时间
        [Key(7)] public double EndSec { get; set; }     // 结束时间 StartSec + T / baseFps
    }
}