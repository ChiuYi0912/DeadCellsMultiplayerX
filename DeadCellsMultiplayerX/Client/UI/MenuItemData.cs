using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dc.h2d;
using dc.ui;
using dc.uicore;

namespace DeadCellsMultiplayerX.Client.UI
{
    public class Page
    {
        public FlowBox mainFlow = null!;
        public double targetX;
        public double startX;
        public bool built;
    }

    public enum PageKind
    {
        Lobby, 

        Host,

        Client,

        GameMian,

        OnlineMian,

        SteamP2P,

        Settings
    }
}