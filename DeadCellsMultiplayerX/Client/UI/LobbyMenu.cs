using System.Linq;
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
using ModCore.Modules;
using ModCore.Utilities;
using Serilog;

namespace DeadCellsMultiplayerX.Client.UI
{
    internal class LobbyMenu : dc.ui.Process
    {
        private readonly ClientMain client;
        private readonly ILogger logger;
        private ModeConfig? currentMode;
        private PlayerSlotPanel? playerPanel;
        public ModConfig config = null!;
        public TitleScreen titleScreen { get; private set; } = null!;
        public ControllerHelperSuper<ModConfig> controllerHelper = null!;

        public readonly List<ModeConfig> modes = [];
        private readonly List<BtnData> btns =[];
        private readonly List<BtnData> topbtns =[];
        private readonly List<ReleaseNotes> notes = [];


        private readonly Dictionary<ControllerKey,int> keys =[];    // 按键act


        public Action? OnplayerPanelBtn;

        // 布局
        private FlowBox? mainFlow;
        internal FlowBox? rightFlow;
        private Flow? tabFlow;
        public Flow? leftFlow;
        public Flow? rightFlowMain;


        private HSprite? selection;
        private HSprite? tapselection;
        private ArrayObj? defaultbtns; 

        internal int ScreenW { get; private set; }
        internal int ScreenH { get; private set; }
        internal int PanelW { get; private set; }
        public bool lockInter { get ; set; }
        private int frameCounter { get; set; }
        public bool _isHost { get; private set; }
        public int curTopbts
        {
            get => config.currentMode;
            set
            {
                config.currentMode = value;
                ModConfig.Config.Save();
            }
        }

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
        /// <param name="mode">ModeConfig 子类实例</param>
        public void RegisterMode(ModeConfig mode) => modes.Add(mode);
        

        public void Show()
        {
            setControlLabel();
            BuildUI();
            BuildInformation();

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

        public void RefreshUI(){ Hide(); Show(); }

        public void SelectMode(ModeConfig mode)
        {
            if ( rightFlow == null) return;
            currentMode = mode;
            BuildRightMode(mode);
            mainFlow!.reflow();


            void BuildRightMode(ModeConfig mode)
            {
                if (rightFlow == null) return;
                ClearContent(rightFlow);
                WriteTestLog();
                mode.BuildContent(rightFlow, PanelW);
                rightFlow.reflow();
            }
        }
        
        public void ShowHost(ModeConfig mode)
        {
            mode.OnHost();
            logger.Information($"modetype:{mode.GetType().Name}");
            RefreshFlow(true);
            AddHostButtons();
        }

        public void ShowClient(ModeConfig mode)
        {
            mode.OnClient();
            RefreshFlow(false);
            AddClientButtons();
        }

        public void RefreshFlow(bool ishost)
        {
            if (rightFlow == null || leftFlow == null) return;
            _isHost = ishost;

            ClearContent(rightFlow);
            ClearContent(leftFlow!);

            playerPanel = new PlayerSlotPanel(rightFlow);
            RefreshLobbySlots();

            rightFlow.reflow();
            leftFlow.reflow();
            btns.Clear();
            
            OnplayerPanelBtn = null;
        }

        #region LeftButton
        /// <summary>
        /// 左侧按钮
        /// </summary>
        private void BuildDefaultButtons()
        {
            BuildLeftBtn(T("CreateRoom"), () => { if (currentMode != null) ShowHost(currentMode); });
            BuildLeftBtn(T("JoinRoom"), () => { if (currentMode != null) ShowClient(currentMode); });
            currentMode?.BuildMenu();
            BuildLeftBtn(T("return"), Hide);
        }

        public void AddHostButtons()
        {
            BuildLeftBtn("开始游戏", async () =>
            {
                var hc = client.CurrentHostClient;
                if (hc == null || !hc.CanStartGame) return;
                await hc.StartGame();
            });
            BuildLeftBtn(T("return"), ()=>
            {
                RefreshUI();
            });
        }

        public void AddClientButtons()
        {
            BuildLeftBtn("准备 / 取消", () =>
            {
                var gc = client.CurrentGuestClient;
                if (gc == null) return;
                var me = gc.LobbyInfo?.Guests.Values.FirstOrDefault(g => g.Guid == gc.Guid);
                if (me == null) return;
                gc.SetReady(!me.IsReady);
                RefreshLobbySlots();
            });
            BuildLeftBtn(T("return"), () => 
            {
                RefreshUI();
            });
        }

        public void RefreshLobbySlots()
        {
            if (playerPanel == null) return;

            var lobby = _isHost
                ? client.CurrentHostClient?.LobbyInfo
                : client.CurrentGuestClient?.LobbyInfo;

            if (lobby != null)
                playerPanel.Refresh(lobby.Guests.Values);
            else
                playerPanel.ClearAll();
        }
        #endregion

        

        #region updates
        public override void update()
        {
            base.update();

            // 每15帧刷新一次玩家卡槽
            if (playerPanel != null && frameCounter++ % 15 == 0)
            {
                RefreshLobbySlots();
                playerPanel.RefreshStatuses();
            }

            if (selection != null)
                updateHSprite(selection);
            if (tapselection != null)
                updateHSprite(tapselection);

            //联机模式切换
            if (controllerHelper != null && defaultbtns != null)
            {
                if (controllerHelper.IsPressed(keys[ControllerKey.Tab_Exchange_E]))
                    SwitchMode(1);

                if (controllerHelper.IsPressed(keys[ControllerKey.Tab_Exchange_Q]))
                    SwitchMode(-1);
            }

            


            void SwitchMode(int step)
            {
                curTopbts += step;

                if (curTopbts < 0)
                    curTopbts = modes.Count - 1;
                else if (curTopbts >= modes.Count)
                    curTopbts = 0;

                SelectMode(modes[curTopbts]);

                var btn = topbtns[curTopbts];
                btn?.interactive?.onClick?.Invoke(null!);

                logger.Information($"modes: {currentMode?.GetType().Name}");
            }


            void updateHSprite(HSprite spr)
            {
                double? v = ((HaxeProxyBase)Data.Class.gui.byId.get("co_blinkCursorSpeed".AsHaxeString())).ToVirtual<virtual_biome_color_comment_id_v0_>().v0;
                if (v == null) return;
                double cos = Lib_std.math_cos.Invoke((double)(ftime * 0.1 * v!));
                spr.alpha = 0.8 + 0.2 * cos;
            }
        }

        public override void onResize()
        {
            base.onResize();
            CalcLayout();
        }
        #endregion

        #region Control
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
            Flow flow = fControlLabel;
            flow.reflow();
            createControlLabel(defaultbtns);
            const double SIZE = 0.7;
            for (int i = 0; i < flow.children.length; i++)
            {
                var contor = flow.children.getDyn(i) as ControlLabel;
                if (contor == null)
                    continue;
                contor.scaleX = contor.scaleY = SIZE;
            }
            flow.reflow();

        }
        #endregion

        #region UI Building
        private void BuildUI()
        {
            CalcLayout();
            int tabH = pixel(36);
            int gap = pixel(8);
            int leftW = ScreenW / 4;

            // 背景容器
            mainFlow = CreateBoxLegendaryOutline(null, true);
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

            tapselection = new HSprite(Assets.Class.ui, "selectLeftRight".AsHaxeString(), Ref<int>.In(0), null);
            tapselection.scaleX = tapselection.scaleY = get_pixelScale.Invoke() / 2;
            var pivot = tapselection.pivot;
            pivot.centerFactorX = 0;
            pivot.centerFactorY = 0;
            pivot.usingFactor = true;
            pivot.isUndefined = false;
            tabFlow.addChild(tapselection);
            tabFlow.getProperties(tapselection).set_isAbsolute(true);

            //切换按钮
            var q = ControlIcon.Class.action.Invoke(keys[ControllerKey.Tab_Exchange_Q], 0, 0, tabFlow);
            var e = ControlIcon.Class.action.Invoke(keys[ControllerKey.Tab_Exchange_E], 0, 0, tabFlow);
            q.scaleX = q.scaleY = e.scaleX = e.scaleY = e.scaleX / 2;

            tabFlow.getProperties(e).paddingTop = pixel(5);
            tabFlow.getProperties(q).paddingTop = pixel(5);

            topbtns.Clear();
            foreach (var m in modes)
            {
                var mode = m;
                BuildTab(tabFlow, mode.Name, () => { });
            }
            tabFlow.reflow();

            Point point = tapselection!.parent.globalToLocal(topbtns[curTopbts].text!.localToGlobal(null));
            tapselection.x = point.x - (int)(get_pixelScale.Invoke() * 5.0);
            tapselection.y = point.y;
            tapselection.posChanged = true;

            // 上下分隔
            var spacer = new Graphics(mainFlow);
            spacer.beginFill(Ref<int>.In(0xFFFFFF), Ref<double>.In(0.1));
            spacer.drawRect(0, 0, ScreenW / 2, pixel(1));
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

            selection = new HSprite(Assets.Class.ui, "selectLeftRight".AsHaxeString(), Ref<int>.In(0), null);
            selection.visible = false;
            selection.scaleX = selection.scaleY = get_pixelScale.Invoke();
            pivot = selection.pivot;
            pivot.centerFactorX = 0;
            pivot.centerFactorY = 0;
            pivot.usingFactor = true;
            pivot.isUndefined = false;
            leftFlow.addChild(selection);
            leftFlow.getProperties(selection).set_isAbsolute(true);

            //右侧
            rightFlowMain = new Flow(null);
            rightFlowMain.set_isVertical(true);
            rightFlowMain.set_minWidth(PanelW);
            rightFlowMain.set_maxWidth(PanelW);
            rightFlowMain.set_minHeight(ScreenH / 2);
            rightFlowMain.set_verticalSpacing(pixel(4));
            rightFlowMain.set_horizontalAlign(new FlowAlign.Middle());
            rightFlowMain.set_verticalAlign(new FlowAlign.Middle());
            bodyFlow.addChildAt(rightFlowMain, 1);

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


            if (curTopbts >= modes.Count) curTopbts = 0;
            if (modes.Count > 0) SelectMode(modes[curTopbts]);

            btns.Clear();
            ClearContent(leftFlow!);
            BuildDefaultButtons();


            if (fControlLabel is not null)
            {
                fControlLabel.set_verticalSpacing(pixel(10));
                fControlLabel.reflow();
                rightFlowMain!.addChild(fControlLabel);
            }
        }

        private void BuildInformation()
        {
            var infoFlow = new Flow(rightFlowMain);
            int w = PanelW / 2;
            int h = ScreenH / 4;

            var container = CreateBoxLegendaryOutline(infoFlow);
            container.set_isVertical(true);
            container.set_minWidth(w);
            container.set_maxWidth(w);
            container.set_minHeight(h);
            container.set_horizontalAlign(new FlowAlign.Middle());
            container.reflow();

            var box = container.box;
            double ps = get_pixelScale.Invoke();
            int borderPadW = (int)(box.borderW * ps);
            int borderPadH = (int)(box.borderH * ps);
            int maskW = box.wid - borderPadW * 2;
            int maskH = box.hei - borderPadH * 2;

            double invScale = 1.0 / ps;
            var mask = new Mask(maskW, maskH, null)
            {
                x = box.borderW,
                y = box.borderH,
                scaleX = invScale,
                scaleY = invScale,
                width = maskW,
                height = maskH,
            };
            box.addChildAt(mask, 1);

            int scrollPad = pixel(10);
            var contentContainer = new dc.h2d.Object(null);
            mask.addChild(contentContainer);
            contentContainer.x = pixel(10);
            contentContainer.y = scrollPad;

            var contentFlow = new Flow(contentContainer);
            contentFlow.set_isVertical(true);
            contentFlow.set_minWidth(maskW - pixel(20));
            contentFlow.set_maxWidth(maskW - pixel(20));
            AddText("Release Notes", null, contentFlow, new FlowAlign.Middle(), 2);

            foreach (var note in notes)
            {
                var header = AddText(
                    $"Version:{note.Version}\nLast updated:{note.ReleaseTime}",
                    null, contentFlow, new FlowAlign.Left(), 1.5, pixel(10));

                foreach (var change in note.Changes)
                    AddText($"{change.Type}:{change.Description}",
                        GetColor(change.Type), contentFlow, new FlowAlign.Left(), 1.4, pixel(30));

                contentFlow.getProperties(header).paddingTop = pixel(10);
            }

            contentFlow.reflow();
            var bounds = contentContainer.getBounds(null, null);
            double maxScrollY = System.Math.Max(0, bounds.yMax - bounds.yMin - maskH);

            var inter = new Interactive(maskW, maskH, mask, null) { propagateEvents = true };
            inter.onWheel = e =>
            {
                if (e.wheelDelta == 0) return;
                double newY = contentContainer.y - e.wheelDelta * pixel(30);
                newY = System.Math.Max(-maxScrollY - scrollPad, System.Math.Min(scrollPad, newY));
                contentContainer.posChanged = true;
                contentContainer.y = newY;
            };

            // Mod info box
            var modBox = CreateBoxLegendaryOutline(infoFlow);
            modBox.set_isVertical(true);
            modBox.set_minWidth(w);
            modBox.set_maxWidth(w);
            modBox.set_minHeight(h);
            modBox.set_horizontalAlign(new FlowAlign.Middle());
            modBox.reflow();

            var mod = ModEntry.Instance.Info;
            AddText("Mod Information:", null, modBox, new FlowAlign.Middle(), 2, paddingTop: pixel(10));
            AddText($"Mod name: {mod.Name}", null, modBox, new FlowAlign.Middle(), 1.5, paddingTop: pixel(10));
            AddText("Lead author: HKLab", null, modBox, new FlowAlign.Middle(), 1.5, paddingTop: pixel(10));
            AddText("Contribution: ChiuYi", null, modBox, new FlowAlign.Middle(), 1.5, paddingTop: pixel(10));
            AddText($"Current version: {mod.Version}", null, modBox, new FlowAlign.Middle(), 1.5, paddingTop: pixel(10));

            static int GetColor(ChangeType type) => type switch
            {
                ChangeType.Feature => 0x00FF00,
                ChangeType.Improvement => 0x0000FF,
                ChangeType.BugFix => 0xFF0000,
                ChangeType.Breaking => 0xFF00FF,
                _ => 0xFFFFFF,
            };
        }

        #endregion

        #region Dispose
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


        public override void onDispose()
        {
            mainFlow?.removeChildren();
            mainFlow?.remove();

            mainFlow = null;
            tabFlow = null;
            leftFlow = null;
            rightFlow = null;
            currentMode = null;
            rightFlowMain = null;
            base.onDispose();
        }
        #endregion

        #region Helper
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
            var text = Assets.Class.makeMedievalText(label.AsHaxeString(), null, btn, null);
            text.scaleX = text.scaleY = get_pixelScale.Invoke() * 0.50;
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
                btn.interactive.onClick = new HlAction<dc.hxd.Event>(e => { cb(); AudioHelper.LoadAudioFormString("sfx/ui/menu_select.wav"); });
                btn.interactive.onOver = new(e =>
                {

                    AudioHelper.LoadAudioFormString("sfx/ui/menu_click1.wav");

                });
                btn.interactive.onMove = new(e =>
                {
                    if (selection == null) return;

                    selection.visible = true;
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

            var text = Assets.Class.makeMedievalText(label.AsHaxeString(), null, btn, null);
            text.scaleX = text.scaleY = text.scaleX / 2;
            text.posChanged = true;

            btn.set_enableInteractive(true);
            btn.reflow();

            parent.addChildAt(btn, 2);

            var data = new BtnData
            {
                Flow = btn,
                text = text,
                interactive = btn.interactive
            };


            if (btn.interactive != null)
            {
                btn.interactive.width = btn.calculatedWidth;
                btn.interactive.height = btn.calculatedHeight;
                btn.interactive.cursor = new Cursor.Button();
                btn.interactive.onClick = new HlAction<dc.hxd.Event>(e =>
                {
                    cb();
                    AudioHelper.LoadAudioFormString("sfx/ui/menu_click1.wav");
                    Point point = tapselection!.parent.globalToLocal(topbtns[curTopbts].text!.localToGlobal(null));
                    HSprite sel = tapselection;
                    sel.x = point.x - (int)(get_pixelScale.Invoke() * 5.0);
                    sel.y = point.y;
                    sel.posChanged = true;
                });
                btn.interactive.onOver = new(_ =>
                {
                    AudioHelper.LoadAudioFormString("sfx/ui/menu_click1.wav");
                });
            }

            topbtns.Add(data);
        }


        
        private void WriteTestLog()
        {
            for (int i = 0; i < 15; i++)
            {
                var note = new ReleaseNotes("2.1.0",
                    new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.FromHours(8)), "Summer Update");
                note.Changes.Add(new ChangeEntry(ChangeType.Feature, "添加了深色模式"));
                note.Changes.Add(new ChangeEntry(ChangeType.BugFix, "修复登录页面崩溃问题"));
                notes.Add(note);
            }
        }

        public void LoadImageTorightFlow(string Path)
        {
            var Image = Res.Class.load(Path.AsHaxeString()).toTexture();
            var tile = Tile.Class.fromTexture(Image);
            tile.width = (int)rightFlow!.minWidth!;
            tile.height = (int)rightFlow.minHeight!;
            var bmap = new Bitmap(tile, null);
            rightFlow.addChildAt(bmap, 1);
        }


        public dc.ui.Text AddText(string text, int? color, Flow parent, FlowAlign hAlign,
            double scale = 1.0, int padLeft = 0, int paddingTop = 0)
        {
            var t = Assets.Class.makeText(text.AsHaxeString(), color ?? 0xFFFFFF, null, parent);
            t.scaleX = t.scaleY = scale;
            parent.getProperties(t).horizontalAlign = hAlign;
            if (padLeft > 0) parent.getProperties(t).paddingLeft = padLeft;
            if (paddingTop > 0) parent.getProperties(t).paddingTop = paddingTop;
            return t;
        }

        public void Title(Flow parent, string text)
        {
            var t = Assets.Class.makeMedievalText(text.AsHaxeString(), null, parent, null);
            t.maxWidthWanted = PanelW;
            t.onResize();
            parent.getProperties(t).horizontalAlign = new FlowAlign.Middle();
            parent.getProperties(t).verticalAlign = new FlowAlign.Top();
        }

        public void Spacer(Flow flow)
        {
            var g = new Graphics(flow);
            g.beginFill(Ref<int>.In(0xFFFFFF), Ref<double>.In(0.15));
            g.drawRect(0, 0, PanelW / 2, 5);
            g.endFill();
            flow.getProperties(g).verticalAlign = new FlowAlign.Top();
            flow.getProperties(g).horizontalAlign = new FlowAlign.Middle();
        }

        public Flow Row(string? text, int color = 0xDDDDDD)
        {
            var line = new Flow(rightFlow);
            line.set_isVertical(true);
            line.set_verticalAlign(new FlowAlign.Middle());
            line.set_maxWidth(PanelW);
            line.set_minWidth(PanelW);
            line.set_padding(pixel(10));
            if (text != null)
            {
                var labe = Assets.Class.makeText(text.AsHaxeString(), color, null, line);
                labe.scaleX = labe.scaleY = 1.2;
                line.getProperties(labe).horizontalAlign = new FlowAlign.Middle();
                line.getProperties(labe).verticalAlign = new FlowAlign.Top();
            }
            line.reflow();
            return line;
        }

        public Flow ButtonRow()
        {
            var flow = new Flow(rightFlow);
            flow.set_minWidth(PanelW - pixel(20));
            flow.set_verticalAlign(new FlowAlign.Middle());
            flow.set_horizontalAlign(new FlowAlign.Middle());
            flow.set_maxWidth(PanelW);
            flow.multiline = true;
            return flow;
        }

        public void PageButton(Flow parent, string label, Action cb, int color = 0xFFFFFF)
        {
            var btn = new Flow(parent);
            btn.set_isVertical(false);
            btn.set_padding(pixel(4));
            btn.set_minWidth(pixel(60));
            btn.set_verticalAlign(new FlowAlign.Top());
            btn.set_horizontalAlign(new FlowAlign.Middle());
            var text = Assets.Class.makeText(label.AsHaxeString(), color, null, btn);
            btn.set_enableInteractive(true);
            btn.reflow();
            if (btn.interactive != null)
            {
                btn.interactive.width = btn.calculatedWidth;
                btn.interactive.height = btn.calculatedHeight;
                btn.interactive.onClick = new HlAction<dc.hxd.Event>(_ =>
                {
                    lockInter = true;
                    cb();
                    AudioHelper.LoadAudioFormString("sfx/ui/menu_select.wav");
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

            btn.interactive!.onMove = new(_ =>
            {
                lockInter = true;
                sel.visible = true;
                sel.scaleX = sel.scaleY = text.scaleX;
                sel.y = text.y * 1.5;
            });
            btn.interactive.onOut = new(_ =>
            {
                sel.visible = false;
                lockInter = false;
            });
            btn.interactive.onCheck = new(_ => UpdateSprite(sel));

            void UpdateSprite(HSprite spr)
            {
                double? v = ((HaxeProxyBase)Data.Class.gui.byId.get("co_blinkCursorSpeed".AsHaxeString()))
                    .ToVirtual<virtual_biome_color_comment_id_v0_>().v0;
                if (v == null) return;
                double cos = Lib_std.math_cos.Invoke((double)(ftime * 0.1 * v!));
                spr.alpha = 0.8 + 0.2 * cos;
            }
        }

        public string T(string str) => GetText.Instance.GetString(str);
        public dc.String HT(string str) => GetText.Instance.GetString(str).AsHaxeString();

        public static FlowBox CreateBoxLegendaryOutline(dc.h2d.Object? p, bool boxspr = true)
        {
            FlowBox flowBox = FlowBox.Class.createBoxValidation(p, default, default, Ref<bool>.In(true), null);

            var gui = Data.Class.gui.byId;
            var blue = gui.get("co_defaultOpacityBlue".AsHaxeString());
            var black = gui.get("co_defaultOpacityBlack".AsHaxeString());
            flowBox.box.alpha = blue != null ? ((HaxeProxyBase)blue).ToVirtual<virtual_biome_color_comment_id_v0_>().v0 ?? 0.8 : 0.8;
            flowBox.blackBG.alpha = black != null ? ((HaxeProxyBase)black).ToVirtual<virtual_biome_color_comment_id_v0_>().v0 ?? 0.5 : 0.5;
            flowBox.box.bgDuo.alpha = 0.5;

            flowBox.box.sg.onParentChanged();

            return flowBox;
        }

        public virtual_cb_help_inter_isEnable_t_<bool> BuildMenuChild(
        string text, HlAction cb, string? help = null, bool? isEnable = null, int color = 0xFFFFFF)
        {
            return TitleScreen.Class.ME.addMenu(text.AsHaxeString(), cb,
                help?.AsHaxeString(), isEnable, Ref<int>.From(ref color));
        }
    }
    #endregion
}
