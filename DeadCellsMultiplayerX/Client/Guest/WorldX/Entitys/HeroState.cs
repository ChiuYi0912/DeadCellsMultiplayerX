using DeadCellsMultiplayerX.Common.Data;

namespace DeadCellsMultiplayerX.Client.Guest.WorldX.Entitys
{
    internal class HeroState
    {
        public PosVector Pos = new();
        public AnimInfo Anim = new();
        public List<AffectEntry> Affects = [];

        public HeroState Clone() => new()
        {
            Pos = Pos,
            Anim = Anim,
            Affects = Affects,
        };
    }
}