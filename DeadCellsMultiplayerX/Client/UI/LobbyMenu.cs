using System.Net.Http.Headers;
using System.Windows.Controls;
using CoreLibrary.Utilities;
using dc;
using dc.h2d;
using dc.h2d.col;
using dc.hl.types;
using dc.hxd;
using dc.hxd.res;
using dc.libs.heaps.slib;
using dc.libs.misc;
using dc.pr;
using dc.ui;
using Hashlink.Virtuals;
using HashlinkNET.Native.Impl;
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
        private readonly List<BtnData> btns =[];
        private readonly List<BtnData> topbtns =[];

        
        private readonly Dictionary<ControllerKey,int> keys =[];    // 按键act


        public ModConfig config = null!;
        public TitleScreen titleScreen { get; private set; } = null!;
        public ControllerHelperSuper<ModConfig> controllerHelper =null!;

        // 布局
        private FlowBox? mainFlow;
        internal FlowBox? rightFlow;
        private Flow? tabFlow;
        public Flow? leftFlow;
        public Flow? rightFlowMain;


        private HSprite? selection;
        private ArrayObj? defaultbtns; 

        internal int ScreenW { get; private set; }
        internal int ScreenH { get; private set; }
        internal int PanelW { get; private set; }
        public bool lockInter { get ; set; }
        public int curTopbts { get ; set; } =0;

        private BasePageUI? currentMode;

        private void CalcLayout()
        {
            ScreenW = dc.libs.Process.Class.CUSTOM_STAGE_WIDTH > 0
                ? dc.libs.Process.Class.CUSTOM_STAGE_WIDTH
                : dc.hxd.Window.Class.getInstance().get_width();
            ScreenH = dc.libs.Process.Class.CUSTOM_STAGE_HEIGHT > 0
                ? dc.libs.Process.Class.CUSTOM_STAGE_HEIGHT
                : dc.hxd.Window.Class.getInstance().get_height();
            PanelW = (int)(ScreenW * 0.60);
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
        

        public void Show()
        {
            setControlLabel();
            if (root == null) onResize();
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
            mainFlow = CreateBoxLegendaryOutline(null,true);
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

            //切换按钮
            var q = ControlIcon.Class.action.Invoke(keys[ControllerKey.Tab_Exchange_Q], 0, 0, tabFlow);
            var e = ControlIcon.Class.action.Invoke(keys[ControllerKey.Tab_Exchange_E], 0, 0, tabFlow);
            q.scaleX =q.scaleY =e.scaleX =e.scaleY =e.scaleX/2;

            tabFlow.getProperties(e).paddingTop = pixel(5);
            tabFlow.getProperties(q).paddingTop = pixel(5);



            foreach (var m in modes)
            {
                var mode = m;
                BuildTab(tabFlow, mode.Name, () => SelectMode(mode));
            }
            tabFlow.reflow();

            // 上下分隔
            var spacer = new Graphics(mainFlow);
            spacer.beginFill(Ref<int>.In(0xFFFFFF), Ref<double>.In(0.1));
            spacer.drawRect(0, 0, ScreenW/2, pixel(1));
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

            selection =new HSprite(Assets.Class.ui, "selectLeftRight".AsHaxeString(), Ref<int>.In(0), null);
            selection.visible =false;
            selection.scaleX =selection.scaleY =get_pixelScale.Invoke();
            var pivot =selection.pivot;
            pivot.centerFactorX = 0;
            pivot.centerFactorY = 0;
            pivot.usingFactor = true;
            pivot.isUndefined = false;
            leftFlow.addChild(selection);
            leftFlow.getProperties(selection).set_isAbsolute(true);

            //右侧
            rightFlowMain =new Flow(null);
            rightFlowMain.set_isVertical(true);
            rightFlowMain.set_minWidth(PanelW);
            rightFlowMain.set_maxWidth(PanelW);
            rightFlowMain.set_minHeight(ScreenH / 2);
            rightFlowMain.set_verticalSpacing(pixel(4));
            rightFlowMain.set_horizontalAlign(new FlowAlign.Middle());
            rightFlowMain.set_verticalAlign(new FlowAlign.Middle());
            bodyFlow.addChildAt(rightFlowMain,1);

            // 右侧内容面板
            rightFlow = CreateBoxLegendaryOutline(rightFlowMain);
            rightFlow.set_isVertical(true);
            rightFlow.set_minWidth(PanelW);
            rightFlow.set_maxWidth(PanelW);
            rightFlow.set_minHeight((int?)(ScreenH / 1.8));
            rightFlow.set_verticalSpacing(pixel(4));
            rightFlow.set_horizontalAlign(new FlowAlign.Middle());
            rightFlow.set_verticalAlign(new FlowAlign.Middle());
            mainFlow.reflow();
            mainFlow.x = 0;


            if (modes.Count > 0) SelectMode(modes[0]);

            BuildDefaultButtons();
        }

        /// <summary>
        /// 左侧按钮
        /// </summary>
        private void BuildDefaultButtons()
        {
            btns.Clear();
            if (leftFlow == null) return;
            ClearContent(leftFlow);
            BuildLeftBtn("创建房间", () => currentMode?.ShowHost());
            BuildLeftBtn("加入房间", () => currentMode?.ShowClient());
            currentMode?.addmenu.Invoke();
            BuildLeftBtn("返回",     Hide);

            if (fControlLabel is not null)
            {
                fControlLabel.set_verticalSpacing(pixel(10));
                fControlLabel.reflow();
                rightFlowMain!.addChild(fControlLabel);
            }
        }

        /// <summary>
        /// 联机模式
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="label"></param>
        /// <param name="cb"></param>
        private void BuildTab(Flow parent, string label, Action cb)
        {
            var btn = new Flow(null);
            btn.set_padding(pixel(4));
            btn.set_minWidth(pixel(70));
            btn.set_horizontalAlign(new FlowAlign.Middle());

            var text =Assets.Class.makeMedievalText(label.AsHaxeString(),null , btn, null);
            text.scaleX =text.scaleY =text.scaleX/2;
            text.posChanged=true;

            btn.set_enableInteractive(true);
            btn.reflow();

            parent.addChildAt(btn,1);

            var data =new BtnData
            {
                Flow =btn,
                text =text,
                interactive =btn.interactive
            };
            

            var inter =btn.interactive;
            if (inter != null)
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
        public void BuildLeftBtn(string label, Action cb)
        {
            var btn = new Flow(leftFlow);
            btn.set_padding(pixel(6));
            btn.set_minWidth(pixel(140));
            var text =Assets.Class.makeMedievalText(label.AsHaxeString(),null, btn,  null);
            text.scaleX =text.scaleY =get_pixelScale.Invoke()*0.50;
            btn.set_enableInteractive(true);
            btn.reflow();

            var data = new BtnData
            {
                Flow = btn,
                text = text,
                interactive = btn.interactive,
            };

            if (btn.interactive != null)
            {
                btn.interactive.width = btn.calculatedWidth;
                btn.interactive.height = btn.calculatedHeight;
                btn.interactive.cursor = new Cursor.Button();
                btn.interactive.onClick = new HlAction<dc.hxd.Event>(e => {cb();AudioHelper.LoadAudioFormString("sfx/ui/menu_select.wav");});
                btn.interactive.onOver = new(e =>
                {
                   
                        AudioHelper.LoadAudioFormString("sfx/ui/menu_click1.wav");
                    
                });
                btn.interactive.onMove = new(e =>
                {
                    if (selection == null) return;

                    selection.visible=true;
                    Point point = selection.parent.globalToLocal(text.localToGlobal(null));
                    HSprite sel = selection;
                    sel.x = point.x - (int)(get_pixelScale.Invoke() * 5.0);
                    sel.y = point.y;
                    sel.posChanged = true;
                });
            }

            btns.Add(data);
        }

        /// <summary>
        /// 清除flow子项
        /// </summary>
        /// <param name="flow"></param>
        public void ClearContent(Flow flow)
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
        }
        public override void onDispose() { Hide(); base.onDispose(); }

        public override void update()
        {
            base.update();
            if(selection!=null)
                updateHSprite(selection);

            if(controllerHelper==null)return;
            if(defaultbtns==null)return;

            if (controllerHelper.IsPressed(keys[ControllerKey.Tab_Exchange_Q]))
            {
                curTopbts++;
                if (curTopbts>modes.Count-1)
                    curTopbts =0;
                SelectMode(modes[curTopbts]);
            }

            if (controllerHelper.IsPressed(keys[ControllerKey.Tab_Exchange_E]))
            {
                curTopbts--;
                if (curTopbts > modes.Count - 1||curTopbts<0)
                    curTopbts = modes.Count-1;
                SelectMode(modes[curTopbts]);
            }


            void updateHSprite(HSprite spr)
            {
                double? v = ((HaxeProxyBase)Data.Class.gui.byId.get("co_blinkCursorSpeed".AsHaxeString())).ToVirtual<virtual_biome_color_comment_id_v0_>().v0;
                if (v == null) return;
                double cos = Lib_std.math_cos.Invoke((double)(ftime * 0.1 * v!));
                spr.alpha = 0.8 + 0.2 * cos;
            }
        }

        public void setControlLabelKeys()
        {
            var info = ModEntry.Instance.Info;
            const string Lobby_Tab_Exchange_Q = "Lobby_Tab_Exchange_Q";
            const string Lobby_Tab_Exchange_E = "Lobby_Tab_Exchange_E";

            keys.Add(ControllerKey.Tab_Exchange_Q, controllerHelper.GetAction(Lobby_Tab_Exchange_Q)
            ?? controllerHelper.AddKey(info, Lobby_Tab_Exchange_Q, KeyHelper.Q));

            keys.Add(ControllerKey.Tab_Exchange_E, controllerHelper.GetAction(Lobby_Tab_Exchange_E)
            ?? controllerHelper.AddKey(info, Lobby_Tab_Exchange_E, KeyHelper.E));

            keys.Add(ControllerKey.Default_UP, 10);
            keys.Add(ControllerKey.Default_DOWN, 12);
            keys.Add(ControllerKey.Default_Left, 11);
            keys.Add(ControllerKey.Default_Rigth, 13);

            // //test
            // btns.push(creataBCreateButton(10, "上"));
            // btns.push(creataBCreateButton(12, "下"));
            // btns.push(creataBCreateButton(11, "左"));
            // btns.push(creataBCreateButton(13, "右"));


            defaultbtns = (ArrayObj)ArrayUtils.CreateDyn().array;
            defaultbtns.push(creataBCreateButton(14, "Valider"));
            defaultbtns.push(creataBCreateButton(16, "Retour"));

            virtual_acts_cond_label_onAdd_ creataBCreateButton(int actionId, string labelText)
            {
                ArrayBytes_Int acts = ArrayUtils.CreateInt();
                acts.push(actionId);
                var buttonConfig = new virtual_acts_cond_label_
                {
                    acts = acts,
                    label = Lang.Class.t.get(labelText.AsHaxeString(), null),
                    cond = null
                };
                return buttonConfig.ToVirtual<virtual_acts_cond_label_onAdd_>();
            }
        }

        public void setControlLabel()
        {
            createControlLabel(defaultbtns);

            Flow flow = fControlLabel;
            const double SIZE = 0.7;
            for (int i = 0; i < flow.children.length; i++)
            {
                var contor = flow.children.getDyn(i) as ControlLabel;
                if (contor == null)
                    continue;
                contor.scaleX = contor.scaleY = SIZE;
            }
        }

        public virtual_cb_help_inter_isEnable_t_<bool> BuildMenuChild(
            string text, HlAction cb, string? help = null, bool? isEnable = null, int color = 0xFFFFFF)
        {
            return TitleScreen.Class.ME.addMenu(text.AsHaxeString(), cb,
                help?.AsHaxeString(), isEnable, Ref<int>.From(ref color));
        }

        public static FlowBox CreateBoxLegendaryOutline(dc.h2d.Object? p,bool boxspr = true)
        {
            FlowBox flowBox = FlowBox.Class.createBoxValidation(p, default, default , Ref<bool>.In(true), null);

            var gui = Data.Class.gui.byId;
            var blue = gui.get("co_defaultOpacityBlue".AsHaxeString());
            var black = gui.get("co_defaultOpacityBlack".AsHaxeString());
            flowBox.box.alpha = blue != null ? ((HaxeProxyBase)blue).ToVirtual<virtual_biome_color_comment_id_v0_>().v0 ?? 0.8 : 0.8;
            flowBox.blackBG.alpha = black != null ? ((HaxeProxyBase)black).ToVirtual<virtual_biome_color_comment_id_v0_>().v0 ?? 0.5 : 0.5;
            flowBox.box.bgDuo.alpha = 0.5;

            flowBox.box.sg.onParentChanged();

            return flowBox;
        }

        public static void PlaySound(string path = "sfx/ui/menu_click1.wav") =>
            _ = Audio.Class.ME.playUIEvent((Sound)Res.Class.get_loader().loadCache(path.AsHaxeString(), Sound.Class), null);
    }
}
