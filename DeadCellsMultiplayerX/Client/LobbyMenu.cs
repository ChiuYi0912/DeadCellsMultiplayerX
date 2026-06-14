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

namespace DeadCellsMultiplayerX.Client
{
    internal class LobbyMenu : dc.ui.Process
    {
        private readonly ClientMain client;
        private readonly ILogger logger;
        //private readonly LobbyInfo lobby;


        private Flow mainFlow=null!;
        private dc.ui.Text titleText=null!;

        public LobbyMenu(ClientMain client) : base(null)
        {
            this.client = client;
            this.logger = Log.ForContext(GetType());
            
            createRootInLayers(Main.Class.ME.root, Const.Class.ROOT_DP_MAIN);

            CreateUI();
            onResize();
        }

        private void CreateUI()
        {
            mainFlow = new(root);
            titleText =Assets.Class.makeText("title".AsHaxeString(),null,null, mainFlow);
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

        public void OnlineMenu(TitleScreen screen)
        {
            screen.isMainMenu = false;
            screen.clearMenu();

#if DEBUG
            BuildMenuChild(T("TEST_Server"), () =>
            {
                Test.Start();

            });

            BuildMenuChild(T("TEST_Menu"), () =>
            {

            });
#endif

            BuildMenuChild(T("创建房间"), () =>
            {
                //ClientMain.Instance.StartHost("127.0.0.1", 44567);

            });

            BuildMenuChild(T("加入房间"), () =>
            {
                //ClientMain.Instance.StartGuest("127.0.0.1", 44567);

            });

            BuildMenuChild(T("返回"), screen.mainMenu);
        }


        public virtual_cb_help_inter_isEnable_t_<bool> BuildMenuChild(
            string text,
            HlAction callback,
            string? help = null,
            bool? isEnable = null,
            int color = 0xFFFFFF)
        {
            return TitleScreen.Class.ME.addMenu(
                text.AsHaxeString(),
                callback,
                help?.AsHaxeString(),
                isEnable,
                Ref<int>.From(ref color)
            );
        }

        private static string T(string key) =>
            GetText.Instance.GetString(key);
    }
}
