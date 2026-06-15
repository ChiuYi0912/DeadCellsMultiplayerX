using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeadCellsMultiplayerX.Client.UI
{
    public class MenuItemData
    {
        public MenuItemPage PageId { get ; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Color { get; set; } = 0xFFFFFF;
        public Action OnClick =null!;
        public bool Enabled;
    }

    public enum MenuItemPage
    {
        OnlineMian,
        CretaeROOM,
        JoinROOM
    }
}