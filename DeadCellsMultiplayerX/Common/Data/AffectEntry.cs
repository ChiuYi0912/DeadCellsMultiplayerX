using MessagePack;

namespace DeadCellsMultiplayerX.Common.Data
{
    [MessagePackObject]
    public class AffectEntry
    {
        [Key(0)] public int OuterIndex;   // affects[i]
        [Key(1)] public int InnerIndex;   // affects[i][j]
        [Key(2)] public int A;
        [Key(3)] public double T;
        [Key(4)] public int UniqId;
        [Key(5)] public double Val;
    }
}