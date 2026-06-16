using CoreLibrary.Utilities;
using dc;
using dc.h2d;
using dc.hl.types;
using dc.hxd;
using dc.hxd.res;
using dc.libs;
using dc.libs.heaps.slib;
using dc.libs.misc;
using dc.pr;
using dc.ui;
using Hashlink.Virtuals;
using HaxeProxy.Runtime;
using ModCore.Modules;
using ModCore.Utilities;
using Serilog;

namespace DeadCellsMultiplayerX.Client.UI
{
    internal class LobbyMenu : dc.ui.Process
    {
        private readonly ClientMain client;
        private readonly ILogger logger;
        private readonly Dictionary<PageKind, Page> pages = [];
        private Page? currentPage;

        public ModConfig config =null!;

        /// <summary>
        /// 按键检测
        /// </summary>
        public ControllerHelperSuper<ModConfig> controllerSuer =null!;


        /// <summary>
        /// 页面位置
        /// </summary>
        private int LayoutPanelW;
        private int LayoutPanelY;
        private int LayoutSlideX;
        private int ScreenW, ScreenH;


        private void CalcLayout()
        {
            ScreenW = dc.libs.Process.Class.CUSTOM_STAGE_WIDTH > 0
                ? dc.libs.Process.Class.CUSTOM_STAGE_WIDTH
                : dc.hxd.Window.Class.getInstance().get_width();
            ScreenH = dc.libs.Process.Class.CUSTOM_STAGE_HEIGHT > 0
                ? dc.libs.Process.Class.CUSTOM_STAGE_HEIGHT
                : dc.hxd.Window.Class.getInstance().get_height();
            LayoutPanelW = ScreenW / 3;
            LayoutPanelY = ScreenH / 2;
            LayoutSlideX = ScreenW + pixel(20);
        }

        public LobbyMenu(ClientMain client,TitleScreen titleScreen) : base(null)
        {
            this.client = client;
            this.logger = Log.ForContext(GetType());
            config =ModConfig.Config.Value;
        }

        /// <summary>
        /// 构建页面
        /// </summary>
        /// <param name="kind"></param>
        /// <returns></returns>
        private Page GetPage(PageKind kind)
        {
            if (!pages.TryGetValue(kind, out var page))
            {
                page = new Page { startX = LayoutSlideX, targetX = LayoutSlideX };
                pages[kind] = page;
            }
            if (!page.built)
            {
                switch (kind)
                {
                    case PageKind.Host: BuildHostPage(page);  break;
                    case PageKind.Client: BuildClientPage(page); break;

                }
                page.built = true;
            }
            return page;
        }

        /// <summary> 
        /// 切换页面
        /// </summary>
        public void SwitchPage(PageKind kind)
        {
            if (root == null) onResize();

            var newPage = GetPage(kind);
            if (newPage == currentPage || newPage.mainFlow==null)
            {
                logger.Information($"你忘了添加页面{kind.ToString()}");
                return;
            }

            if (currentPage != null)
                AnimatePage(currentPage, LayoutSlideX);

            newPage.mainFlow.x = newPage.startX;
            AnimatePage(newPage, newPage.targetX);

            currentPage = newPage;
        }


        /// <summary>
        /// 房主ui
        /// </summary>
        /// <param name="page"></param>
        private void BuildHostPage(Page page)
        {
            var flow = CreateFlowBox();
            page.mainFlow = flow;
            int pw = LayoutPanelW;

            var title = Assets.Class.makeMedievalText("创建房间".AsHaxeString(), null, flow, null);
            title.set_textAlign(new Align.Center());
            title.maxWidthWanted = pw;
            title.onResize();

            AddSpacer(flow, pw);

            var infoLine = AddLine(flow, pw);
            Assets.Class.makeText("Room: 127.0.0.1:44567".AsHaxeString(), 0xFFD700, null, infoLine);
            infoLine.reflow();

            var pLine = AddLine(flow, pw);
            Assets.Class.makeText("等待玩家...".AsHaxeString(), 0xAAAAAA, null, pLine);
            pLine.reflow();

            AddSpacer(flow, pw);

            var btnFlow = new Flow(flow);
            btnFlow.set_minWidth(pw - pixel(20));
            btnFlow.set_horizontalSpacing(pixel(8));
            btnFlow.set_verticalAlign(new FlowAlign.Middle());
            btnFlow.set_maxWidth(pw);
            btnFlow.multiline = true;

            BuildBtn(btnFlow, "开始游戏", () => SwitchPage(PageKind.Host));
            BuildBtn(btnFlow, "退出房间", () => SetVisible(false));
            
            btnFlow.reflow();

            FinalizePage(page);
        }

        /// <summary>
        /// 客户ui
        /// </summary>
        /// <param name="page"></param>
        private void BuildClientPage(Page page)
        {
            var flow = CreateFlowBox();
            page.mainFlow = flow;
            int pw = LayoutPanelW;

            var title = Assets.Class.makeMedievalText("加入房间".AsHaxeString(), null,flow,null);
            title.set_textAlign(new Align.Center());
            title.maxWidthWanted = pw;
            title.onResize();

            AddSpacer(flow, pw);

            var skinLine = AddLine(flow, pw);
            Assets.Class.makeText("玩家:列表".AsHaxeString(), 0xDDDDDD, null, flow);
            skinLine.reflow();

            AddSpacer(flow, pw);

            var btnFlow = new Flow(flow);
            btnFlow.set_minWidth(pw - pixel(20));
            btnFlow.set_horizontalSpacing(pixel(8));
            btnFlow.set_verticalAlign(new FlowAlign.Middle());

            BuildBtn(btnFlow, "退出房间", () => SetVisible(false));
            btnFlow.reflow();

            FinalizePage(page);
        }

        /// <summary>
        /// 创建新页面的mainflow
        /// </summary>
        /// <returns></returns>
        private FlowBox CreateFlowBox()
        {
            double padH = 8, padV = 8;
            var flow = FlowBox.Class.createBoxValidationWithBiomeParam(null,
                Ref<double>.In(padH), Ref<double>.In(padV));
            flow.set_isVertical(true);
            flow.set_minWidth(LayoutPanelW);
            flow.set_verticalSpacing(pixel(3));
            root.addChildAt(flow, 1);
            return flow;
        }

        /// <summary>
        /// 计算page的宽度,用于动画执行
        /// </summary>
        /// <param name="page"></param>
        private void FinalizePage(Page page)
        {
            page.mainFlow.reflow();
            page.mainFlow.y = LayoutPanelY;
            page.mainFlow.x = page.startX;
            int pw = page.mainFlow.get_outerWidth();
            page.targetX = ScreenW - pw + pixel(8);
        }

        /// <summary>
        /// 分割线
        /// </summary>
        /// <param name="p"></param>
        /// <param name="wid"></param>
        private void AddSpacer(FlowBox p, int wid)
        {
            var g = new Graphics(p);
            g.beginFill(Ref<int>.In(0xFFFFFF), Ref<double>.In(0.15));
            double ps = get_pixelScale.Invoke();
            g.drawRect(0, 0, wid, ps);
            g.endFill();
            p.getProperties(g).verticalAlign = new FlowAlign.Middle();
            p.getProperties(g).horizontalAlign = new FlowAlign.Middle();
        }

        /// <summary>
        /// 添加横向Flow
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="wid"></param>
        /// <returns></returns>
        private Flow AddLine(FlowBox parent, int wid)
        {
            var flow = new Flow(parent);
            flow.set_isVertical(false);
            flow.set_verticalAlign(new FlowAlign.Middle());
            flow.set_paddingTop(pixel(2));
            flow.set_paddingBottom(pixel(2));
            flow.set_paddingLeft(pixel(8));
            flow.set_paddingRight(pixel(8));
            flow.set_maxWidth(wid);
            flow.set_minWidth(wid);
            return flow;
        }


        /// <summary>
        /// 添加按钮
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="label"></param>
        /// <param name="cb"></param>
        /// <param name="color"></param>
        private void BuildBtn(Flow parent, string label, Action cb, int color = 0xFFFFFF)
        {
            var btn = new Flow(parent);
            btn.set_isVertical(false);
            btn.set_padding(pixel(4));
            btn.set_minWidth(pixel(60));
            var text = Assets.Class.makeText(label.AsHaxeString(), color, null, btn);
            text.realMaxWidth = LayoutPanelW/parent.children.length;
            btn.set_enableInteractive(true);
            btn.reflow();
            if (btn.interactive != null)
            {
                btn.interactive.width  = btn.calculatedWidth;
                btn.interactive.height = btn.calculatedHeight;
                btn.interactive.onClick = new HlAction<dc.hxd.Event>(_ => {
                    cb();
                    LoadAudioFormString("sfx/ui/menu_select.wav");
                });
            }

            var ui = Assets.Class.ui;
            var sel = new HSprite(ui, "selectLeftRight".AsHaxeString(), Ref<int>.In(0), null);
            sel.alpha = 0.5;
            sel.visible = false;
            sel.pivot.centerFactorX = 0.0;
            sel.pivot.centerFactorY = 0.0;
            sel.pivot.usingFactor = true;
            sel.pivot.isUndefined = false;
            btn.addChild(sel);

            btn.interactive!.onCheck = new HlAction<dc.hxd.Event>(_ =>
            {
                sel.visible = true;
                sel.scaleX = sel.scaleY = text.scaleX;
                //sel.x = -pixel(5);
                sel.y = text.y*1.5;
            });

            btn.interactive.onOut=new(_=>{ sel.visible = false; });
            
        }

        /// <summary>
        /// 动画
        /// </summary>
        /// <param name="page"></param>
        /// <param name="toX"></param>
        /// <returns></returns>
        private Tween AnimatePage(Page page, double toX)
        {
            return tw.create_(new HlFunc<double>(() => page.mainFlow.x),
                new HlAction<double>(x => { page.mainFlow.x = x; page.mainFlow.posChanged = true; }),
                null, toX, new TType.TEaseOut(), 500, Ref<bool>.Null);
        }

        /// <summary>
        /// 隐藏/显示
        /// </summary>
        /// <param name="v"></param>
        public void SetVisible(bool v)
        {
            if (root == null) onResize();
            if (v)
            {
                currentPage=null!;
                SwitchPage(PageKind.Lobby);
            }
            else if (currentPage != null)
            {
                var outanim =AnimatePage(currentPage, LayoutSlideX);
                outanim.end(() =>
                {
                    currentPage =null!;
                });
            }
               
        }

        public override void onResize()
        {
            base.onResize();
            CalcLayout();
            root.x = 0;
            pages.Clear();
            currentPage = null;
        }

        public override void onDispose() { base.onDispose(); }


        /// <summary>
        /// 主选项
        /// </summary>
        /// <param name="screen"></param>
        public void OnlineMenu(TitleScreen screen)
        {
            screen.isMainMenu = false;
            screen.clearMenu();
#if DEBUG
            BuildMenuChild(T("TEST_Menu"), () => SwitchPage(PageKind.Lobby));
            BuildMenuChild(T("TEST_HOST"), () => Test.Start());
#endif
            BuildMenuChild(T("创建房间"), () => { 
                //ClientMain.Instance.StartHost("127.0.0.1", 44567); 
                SwitchPage(PageKind.Host);

            });
            BuildMenuChild(T("加入房间"), () => { 
                //ClientMain.Instance.StartGuest("127.0.0.1", 44567); 
                SwitchPage(PageKind.Client);
            });
            BuildMenuChild(T("返回"),     () => { screen.mainMenu(); SetVisible(false); });
        }

        public virtual_cb_help_inter_isEnable_t_<bool> BuildMenuChild(
            string text, HlAction cb, string? help = null, bool? isEnable = null, int color = 0xFFFFFF)
        {
            return TitleScreen.Class.ME.addMenu(text.AsHaxeString(), cb,
                help?.AsHaxeString(), isEnable, Ref<int>.From(ref color));
        }

        /// <summary>
        /// Lang
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private static string T(string key) => GetText.Instance.GetString(key);

        /// <summary>
        /// 音效
        /// </summary>
        /// <param name="PATH"></param>
        public static void LoadAudioFormString(string PATH = "sfx/ui/menu_click1.wav") => _ = Audio.Class.ME.playUIEvent((Sound)Res.Class.get_loader().loadCache(PATH.AsHaxeString(), Sound.Class), null);
    }
}
