using dc;
using dc.h2d;
using dc.hl.types;
using dc.hxd;
using dc.libs;
using dc.pr;
using dc.ui;
using DeadCellsMultiplayerX.Client.Guest;
using DeadCellsMultiplayerX.Client.Host;
using DeadCellsMultiplayerX.Common;
using Hashlink.Virtuals;
using HaxeProxy.Runtime;
using ModCore.Modules;
using ModCore.Utilities;
using Serilog;

namespace DeadCellsMultiplayerX.Client.Lobby
{
    internal class LobbyMenu : dc.ui.Process
    {
        private readonly GuestClient guest;
        private readonly HostClient? host;
        private readonly ILogger logger;
        //private readonly LobbyInfo lobby;


        private  Flow mainFlow=null!;
        private dc.ui.Text titleText=null!;

        public LobbyMenu(GuestClient guest) : base(null)
        {
            this.guest = guest;
            this.host = ClientMain.Instance.CurrentHostClient;
            //this.lobby = guest.LobbyInfo!;
            this.logger = Log.ForContext(GetType());
            
            createRootInLayers(Main.Class.ME.root, Const.Class.ROOT_DP_MAIN);
            CreateUI();
        }

        private void CreateUI()
        {
            mainFlow = new(root);
            this.titleText =Assets.Class.makeText("title".AsHaxeString(),null,null, mainFlow);
        }

        public override void onResize()
        {
            base.onResize();
            mainFlow.reflow();
            var w = dc.hxd.Window.Class.getInstance().get_width();
            var h = dc.hxd.Window.Class.getInstance().get_height();

            root.x = (w - mainFlow.get_outerWidth()) / 2;
            root.y = (h - mainFlow.get_outerHeight()) / 2;

            root.posChanged = true;
        }

        public override void onDispose()
        {
            base.onDispose();
        }


        public void Refresh()
        {
            //lobby = guest.LobbyInfo!;
        }


        private void ToggleReady(Event e)
        {
        }

        private void  StartGame(Event e)
        {
        }
    }
}
