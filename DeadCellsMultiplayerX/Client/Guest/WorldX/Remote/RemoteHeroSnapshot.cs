using DeadCellsMultiplayerX.Common.Data;

namespace DeadCellsMultiplayerX.Client.Guest.WorldX.Remote
{
    /// <summary>
    /// 用于客户端本地存储远程hero的快照数据
    /// </summary>
    internal class RemoteHeroSnapshot
    {
        public PosVector Pos = new();
        public AnimInfo Anim = new();
        public List<AffectEntry> Affects = [];

        public RemoteHeroSnapshot Clone() => new()
        {
            Pos = Pos,
            Anim = Anim,
            Affects = Affects,
        };
    }
}