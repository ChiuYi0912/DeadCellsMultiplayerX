using CoreLibrary.Utilities;
using dc;
using dc.hxd;
using dc.hxd.res;
using dc.libs.misc;
using dc.pr;
using dc.ui;
using Hashlink.Virtuals;
using HaxeProxy.Runtime;
using ModCore.Utilities;
using Serilog;

namespace DeadCellsMultiplayerX.Client.UI
{
    internal class LobbyMenu : dc.ui.Process
    {
        private readonly ClientMain client;
        private readonly ILogger logger;
        private readonly List<BasePageUI> gameModes = [];

        private BasePageUI? currentMode;
        private FlowBox? currentFlow;
        private double pageTargetX;
        private double pageStartX;

        public ModConfig config = null!;
        public ControllerHelperSuper<ModConfig> controllerSuper = null!;
        public TitleScreen titleScreen {get;private set;}=null!;

        /// <summary>
        /// 布局参数，BasePageUI 通过 Manager 访问
        /// </summary>
        internal int PanelW { get; private set; }
        internal int PanelY { get; private set; }
        internal int SlideX { get; private set; }
        internal int ScreenW { get; private set; }
        internal int ScreenH { get; private set; }

        private void CalcLayout()
        {
            ScreenW = dc.libs.Process.Class.CUSTOM_STAGE_WIDTH > 0
                ? dc.libs.Process.Class.CUSTOM_STAGE_WIDTH
                : dc.hxd.Window.Class.getInstance().get_width();
            ScreenH = dc.libs.Process.Class.CUSTOM_STAGE_HEIGHT > 0
                ? dc.libs.Process.Class.CUSTOM_STAGE_HEIGHT
                : dc.hxd.Window.Class.getInstance().get_height();
            PanelW = ScreenW / 3;
            PanelY = ScreenH / 2;
            SlideX = ScreenW + pixel(20);
        }

        public LobbyMenu(ClientMain client, TitleScreen titleScreen) : base(null)
        {
            this.client = client;
            this.logger = Log.ForContext(GetType());
            this.config = ModConfig.Config.Value;
            this.titleScreen =titleScreen;
        }

        /// <summary>
        /// 注册游戏模式
        /// </summary>
        /// <param name="mode">BasePageUI 子类实例</param>
        public void RegisterMode(BasePageUI mode) => gameModes.Add(mode);

        /// <summary>
        /// 构建标题画面左侧菜单：调用每个模式的 BuildLeftMenu
        /// </summary>
        public void BuildLeftMenu()
        {
            foreach (var m in gameModes)
               m.Menu();
        }

        /// <summary>
        /// 切换到指定模式的 Host 页面
        /// </summary>
        /// <param name="mode">目标游戏模式</param>
        public void ShowHost(BasePageUI mode)
        {
            if (root == null) onResize();
            AnimateOut();
            currentFlow = BuildFlow(mode, m => m.BuildRightHost());
            AnimateIn();
            currentMode = mode;
        }

        /// <summary>
        /// 切换到指定模式的 Client 页面
        /// </summary>
        /// <param name="mode"></param>
        public void ShowClient(BasePageUI mode)
        {
            if (root == null) onResize();
            AnimateOut();
            currentFlow = BuildFlow(mode, m => m.BuildRightClient());
            AnimateIn();
            currentMode = mode;
        }

        /// <summary>
        /// 隐藏当前页面
        /// </summary>
        public void Hide()
        {
            AnimateOut();
            currentMode = null;
            currentFlow = null;
        }

        private FlowBox BuildFlow(BasePageUI mode, Action<BasePageUI> build)
        {
            double padH = 8, padV = 8;
            var flow = FlowBox.Class.createBoxValidationWithBiomeParam(null,
                Ref<double>.In(padH), Ref<double>.In(padV));
            flow.set_isVertical(true);
            flow.set_minWidth(PanelW);
            flow.set_verticalSpacing(pixel(3));
            root.addChildAt(flow, 1);

            mode.Init(flow, PanelW);
            build(mode);

            flow.reflow();
            flow.y = PanelY;
            pageStartX = SlideX;
            pageTargetX = ScreenW - flow.get_outerWidth() + pixel(8);
            flow.x = pageStartX;
            return flow;
        }

        /// <summary>
        /// 当前页面向右滑出屏幕
        /// </summary>
        private void AnimateOut()
        {
            if (currentFlow == null) return;
            var flow = currentFlow;
            tw.create_(new HlFunc<double>(() => flow.x),
                new HlAction<double>(x => { flow.x = x; flow.posChanged = true; }),
                null, SlideX, new TType.TEaseOut(), 500, Ref<bool>.Null);
        }

        /// <summary>
        /// 新页面从右侧滑入
        /// </summary>
        private void AnimateIn()
        {
            if (currentFlow == null) return;
            var flow = currentFlow;
            flow.x = pageStartX;
            tw.create_(new HlFunc<double>(() => flow.x),
                new HlAction<double>(x => { flow.x = x; flow.posChanged = true; }),
                null, pageTargetX, new TType.TEaseOut(), 500, Ref<bool>.Null);
        }

        public override void onResize()
        {
            base.onResize();
            CalcLayout();
            root.x = 0;
            currentMode = null;
            currentFlow = null;
        }

        public override void onDispose() { base.onDispose(); }

        /// <summary>
        /// 在标题画面添加菜单条目
        /// </summary>
        /// <param name="text"></param>
        /// <param name="cb"></param>
        /// <param name="help"></param>
        /// <param name="isEnable"></param>
        /// <param name="color"></param>
        /// <returns>菜单条目</returns>
        public virtual_cb_help_inter_isEnable_t_<bool> BuildMenuChild(
            string text, HlAction cb, string? help = null, bool? isEnable = null, int color = 0xFFFFFF)
        {
            return TitleScreen.Class.ME.addMenu(text.AsHaxeString(), cb,
                help?.AsHaxeString(), isEnable, Ref<int>.From(ref color));
        }

        /// <summary>
        /// 播放音效
        /// </summary>
        /// <param name="path"></param>
        public static void PlaySound(string path = "sfx/ui/menu_click1.wav") =>
            _ = Audio.Class.ME.playUIEvent((Sound)Res.Class.get_loader().loadCache(path.AsHaxeString(), Sound.Class), null);
    }
}
