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

        private Flow mainFlow=null!;
        private dc.ui.Text titleText=null!;

        private int get_width()=> dc.hxd.Window.Class.getInstance().get_width();
        private int get_height()=> dc.hxd.Window.Class.getInstance().get_height();
        private double pixelScale{get=> get_pixelScale.Invoke();}

        public LobbyMenu(ClientMain client) : base(null)
        {
            this.client = client;
            this.logger = Log.ForContext(GetType());
        }

        public void AfterTitleScreen()
        {
            createRootInLayers(Main.Class.ME.root, Const.Class.ROOT_DP_MAIN);
            mainFlow = new(root);
            
            onResize();

            mainFlow.debugGraphics = new Graphics(mainFlow);
            mainFlow.debug = true;
            mainFlow.getProperties(mainFlow.debugGraphics).isAbsolute=true;
        }

        private void CreateUI()
        {
            titleText?.remove();
            titleText =Assets.Class.makeText("Hello Word".AsHaxeString(),null, null, mainFlow);
        }

        public override void onResize()
        {
            base.onResize();
            mainFlow.reflow();

            var w = get_width();
            var h = get_height();

            mainFlow.set_minWidth(w / 2);
            mainFlow.set_minHeight(h / 2);

            root.x = w - mainFlow.get_outerWidth();
            root.y = (h - mainFlow.get_outerHeight()) / 2;
            root.posChanged = true;
        }

        public override void onDispose()
        {
            base.onDispose();
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
                CreateUI();
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
