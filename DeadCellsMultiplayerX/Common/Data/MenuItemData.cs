using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeadCellsMultiplayerX.Common.Data
{
    public class MenuItemData
    {
        public MenuItemAllPage PageId { get ; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Color { get; set; } = 0xFFFFFF;
        public Action OnClick =null!;
        public bool Enabled;
    }

    public enum MenuItemAllPage
    {
        GameMian,
        OnlineMian,
        SteamP2P,
        CretaeROOM,
        JoinROOM,
        Settings
    }
}