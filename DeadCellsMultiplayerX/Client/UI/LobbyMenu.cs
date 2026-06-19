using dc;
using dc.h2d;
using dc.hxd;
using dc.hxd.res;
using dc.libs.heaps.slib;
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
        private readonly List<BasePageUI> modes = [];

        public bool lockInter;
        public ModConfig config = null!;
        public TitleScreen titleScreen { get; private set; } = null!;
        public CoreLibrary.Utilities.ControllerHelperSuper<ModConfig> controllerHelper =null!;

        // 布局
        private FlowBox? mainFlow;
        private Flow? tabFlow;
        private Flow? leftFlow;
        internal FlowBox? rightFlow;
        public Flow? rightFlowMain;

        internal int ScreenW { get; private set; }
        internal int ScreenH { get; private set; }
        internal int PanelW { get; private set; }

        private BasePageUI? currentMode;

        private void CalcLayout()
        {
            ScreenW = dc.libs.Process.Class.CUSTOM_STAGE_WIDTH > 0
                ? dc.libs.Process.Class.CUSTOM_STAGE_WIDTH
                : dc.hxd.Window.Class.getInstance().get_width();
            ScreenH = dc.libs.Process.Class.CUSTOM_STAGE_HEIGHT > 0
                ? dc.libs.Process.Class.CUSTOM_STAGE_HEIGHT
                : dc.hxd.Window.Class.getInstance().get_height();
            PanelW = (int)(ScreenW * 0.65);
        }

        public LobbyMenu(ClientMain client, TitleScreen titleScreen) : base(null)
        {
            this.client = client;
            this.logger = Log.ForContext(GetType());
            this.config = ModConfig.Config.Value;
            this.titleScreen = titleScreen;
        }
        /// <summary>
        /// 注册游戏模式
        /// </summary>
        /// <param name="mode">BasePageUI 子类实例</param>
        public void RegisterMode(BasePageUI mode) => modes.Add(mode);
        
        /// <summary>
        /// 构建标题画面左侧菜单：调用每个模式的 BuildLeftMenu
        /// </summary>
        public void BuildLeftMenu() { foreach (var m in modes) m.Menu(); }

        public void Show()
        {
            if (root == null) onResize();
            Hide();
            BuildUI();
            titleScreen.controller.manualLock = true;
            titleScreen.blur(default, default);
        }

        public void Hide()
        {
            mainFlow?.remove(); mainFlow = null;
            tabFlow = null; leftFlow = null; rightFlow = null;
            currentMode = null;
            titleScreen.controller.manualLock = false;
            titleScreen.unblur();
        }

        public void SelectMode(BasePageUI mode)
        {
            if (currentMode == mode || rightFlow == null) return;
            currentMode = mode;
            ClearContent(rightFlow);
            mode.BuildRight(rightFlow, PanelW);
            rightFlow.reflow();
        }

        private void BuildUI()
        {
            CalcLayout();
            int tabH = pixel(36);
            int gap  = pixel(8);
            int leftW = ScreenW / 4;

            // 背景容器
            double padH = 0, padV = 0;
            mainFlow = CreateBoxLegendaryOutline(null, ref padH, ref padV);
            mainFlow.set_isVertical(true);
            mainFlow.set_minWidth(ScreenW);
            mainFlow.set_minHeight(ScreenH);
            mainFlow.set_maxWidth(ScreenW);
            mainFlow.set_maxHeight(ScreenH);
            mainFlow.set_horizontalAlign(new FlowAlign.Middle());
            root.addChildAt(mainFlow, 1);

            // 顶部标签
            tabFlow = new Flow(mainFlow);
            tabFlow.set_isVertical(false);
            tabFlow.set_horizontalAlign(new FlowAlign.Middle());
            tabFlow.set_verticalAlign(new FlowAlign.Top());
            tabFlow.set_horizontalSpacing(pixel(6));
            foreach (var m in modes)
            {
                var mode = m;
                BuildTab(tabFlow, mode.Name, () => SelectMode(mode));
            }
            tabFlow.reflow();

            // 上下分隔
            var spacer = new Graphics(mainFlow);
            spacer.beginFill(Ref<int>.In(0xFFFFFF), Ref<double>.In(0.1));
            spacer.drawRect(0, 0, ScreenW, pixel(1));
            spacer.endFill();

            // 主体
            var bodyFlow = new Flow(mainFlow);
            bodyFlow.set_isVertical(false);
            bodyFlow.set_paddingTop(gap);
            bodyFlow.set_minHeight((int?)(ScreenH * 0.95));
            bodyFlow.set_horizontalAlign(new FlowAlign.Middle());
            bodyFlow.set_verticalAlign(new FlowAlign.Middle());

            // 左侧
            leftFlow = new Flow(bodyFlow);
            leftFlow.set_isVertical(true);
            leftFlow.set_minWidth(leftW);
            leftFlow.set_verticalSpacing(pixel(6));
            leftFlow.set_horizontalAlign(new FlowAlign.Middle());
            leftFlow.set_verticalAlign(new FlowAlign.Middle());

            //右侧
            rightFlowMain =new Flow(bodyFlow);
            rightFlowMain.set_isVertical(true);
            rightFlowMain.set_minWidth(PanelW);
            rightFlowMain.set_maxWidth(PanelW);
            rightFlowMain.set_minHeight(ScreenH / 2);
            rightFlowMain.set_verticalSpacing(pixel(4));
            rightFlowMain.set_horizontalAlign(new FlowAlign.Middle());
            rightFlowMain.set_verticalAlign(new FlowAlign.Middle());


            // 右侧内容面板
            double rPadH = 0, rPadV = 0;
            rightFlow = CreateBoxLegendaryOutline(rightFlowMain, ref rPadH, ref rPadV);
            rightFlow.set_isVertical(true);
            rightFlow.set_minWidth(PanelW);
            rightFlow.set_maxWidth(PanelW);
            rightFlow.set_minHeight(ScreenH/2);
            rightFlow.set_verticalSpacing(pixel(4));
            rightFlow.set_horizontalAlign(new FlowAlign.Middle());
            rightFlow.set_verticalAlign(new FlowAlign.Middle());
            mainFlow.reflow();
            mainFlow.x = 0;

            // 默认按钮 + 首模式
            BuildDefaultButtons();
            if (modes.Count > 0) SelectMode(modes[0]);
        }

        /// <summary>
        /// 左侧按钮
        /// </summary>
        private void BuildDefaultButtons()
        {
            if (leftFlow == null) return;
            BuildLeftBtn("创建房间", () => currentMode?.BuildHost?.Invoke());
            BuildLeftBtn("加入房间", () => currentMode?.BuildClient?.Invoke());
            BuildLeftBtn("返回",     Hide);
        }

        /// <summary>
        /// 联机模式
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="label"></param>
        /// <param name="cb"></param>
        private void BuildTab(Flow parent, string label, Action cb)
        {
            var btn = new Flow(parent);
            btn.set_padding(pixel(4));
            btn.set_minWidth(pixel(70));
            btn.set_horizontalAlign(new FlowAlign.Middle());
            Assets.Class.makeText(label.AsHaxeString(), 0xFFFFFF, null, btn);
            btn.set_enableInteractive(true);
            btn.reflow();
            if (btn.interactive != null)
            {
                btn.interactive.width = btn.calculatedWidth;
                btn.interactive.height = btn.calculatedHeight;
                btn.interactive.onClick = new HlAction<dc.hxd.Event>(_ => cb());
            }
        }

        /// <summary>
        /// 按钮构建
        /// </summary>
        /// <param name="label"></param>
        /// <param name="cb"></param>
        private void BuildLeftBtn(string label, Action cb)
        {
            var btn = new Flow(leftFlow);
            btn.set_padding(pixel(6));
            btn.set_minWidth(pixel(140));
            Assets.Class.makeText(label.AsHaxeString(), 0xDDDDDD, null, btn);
            btn.set_enableInteractive(true);
            btn.reflow();
            if (btn.interactive != null)
            {
                btn.interactive.width = btn.calculatedWidth;
                btn.interactive.height = btn.calculatedHeight;
                btn.interactive.onClick = new HlAction<dc.hxd.Event>(_ => cb());
            }
        }


        public void ClearContent(FlowBox flow)
        {
            var len = flow.children.length;
            for (int i = len - 1; i >= 0; i--)
            {
                var child = (dc.h2d.Object)flow.children.getDyn(i);
                var props = flow.getProperties(child);
                if (props == null || !props.isAbsolute)
                    flow.removeChild(child);
            }
        }

        public override void onResize()
        {
            base.onResize();
            CalcLayout();
            Hide();
        }
        public override void onDispose() { Hide(); base.onDispose(); }

        public virtual_cb_help_inter_isEnable_t_<bool> BuildMenuChild(
            string text, HlAction cb, string? help = null, bool? isEnable = null, int color = 0xFFFFFF)
        {
            return TitleScreen.Class.ME.addMenu(text.AsHaxeString(), cb,
                help?.AsHaxeString(), isEnable, Ref<int>.From(ref color));
        }

        public static FlowBox CreateBoxLegendaryOutline(dc.h2d.Object? p, ref double padH, ref double padV)
        {
            FlowBox flowBox = FlowBox.Class.createBoxValidation(p, default, default , Ref<bool>.In(true), null);

            var gui = Data.Class.gui.byId;
            var blue = gui.get("co_defaultOpacityBlue".AsHaxeString());
            var black = gui.get("co_defaultOpacityBlack".AsHaxeString());
            flowBox.box.alpha = blue != null ? ((HaxeProxyBase)blue).ToVirtual<virtual_biome_color_comment_id_v0_>().v0 ?? 0.8 : 0.8;
            flowBox.blackBG.alpha = black != null ? ((HaxeProxyBase)black).ToVirtual<virtual_biome_color_comment_id_v0_>().v0 ?? 0.5 : 0.5;
            flowBox.box.bgDuo.alpha = 0.2;
            flowBox.box.sg.tile = Assets.Class.ui.getTile("boxLegendary".AsHaxeString(), default, default, default, null);
            flowBox.box.sg.onParentChanged();

            return flowBox;
        }

        public static void PlaySound(string path = "sfx/ui/menu_click1.wav") =>
            _ = Audio.Class.ME.playUIEvent((Sound)Res.Class.get_loader().loadCache(path.AsHaxeString(), Sound.Class), null);
    }
}
