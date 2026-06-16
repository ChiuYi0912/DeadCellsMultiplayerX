using dc;
using dc.h2d;
using dc.libs.heaps.slib;
using dc.pr;
using dc.ui;
using Hashlink.Virtuals;
using HaxeProxy.Runtime;
using ModCore.Utilities;

namespace DeadCellsMultiplayerX.Client.UI
{
    internal abstract class BasePageUI
    {
        protected LobbyMenu Manager { get; }
        protected FlowBox Flow { get; private set; } = null!;
        protected int PanelW { get; private set; }
        private string Name =string.Empty;

        protected BasePageUI(LobbyMenu manager,string MenuName)
        {
            Manager = manager;
            Name =MenuName;
        }

        public void Init(FlowBox flow, int panelW)
        {
            Flow = flow;
            PanelW = panelW;
        }
        public abstract void BuildRightHost();
        public abstract void BuildRightClient();
        public abstract void BuildLeftMenuChild();
        public abstract void update();

        public void Menu()
        {
            AddMenuChild(Name, () => {
                Manager.titleScreen.clearMenu();
                BuildLeftMenuChild();
            });

            AddMenuChild("返回", () =>
            {
                Manager.titleScreen.clearMenu();
                Manager.titleScreen.mainMenu();
            });
        }


        /// <summary>
        /// 标题
        /// </summary>
        /// <param name="text"></param>
        protected void Title(string text)
        {
            var t = Assets.Class.makeMedievalText(text.AsHaxeString(), null, Flow, null);
            t.set_textAlign(new Align.Center());
            t.maxWidthWanted = PanelW;
            t.onResize();
        }

        /// <summary>
        /// 分割线
        /// </summary>
        protected void Spacer()
        {
            var g = new Graphics(Flow);
            g.beginFill(Ref<int>.In(0xFFFFFF), Ref<double>.In(0.15));
            double ps = Manager.get_pixelScale.Invoke();
            g.drawRect(0, 0, PanelW, ps);
            g.endFill();
            Flow.getProperties(g).verticalAlign = new FlowAlign.Middle();
            Flow.getProperties(g).horizontalAlign = new FlowAlign.Middle();
        }

        /// <summary>
        /// 文本行
        /// </summary>
        /// <param name="text"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        protected Flow Row(string text, int color = 0xDDDDDD)
        {
            var line = new Flow(Flow);
            line.set_isVertical(false);
            line.set_verticalAlign(new FlowAlign.Middle());
            line.set_paddingTop(Manager.pixel(2));
            line.set_paddingBottom(Manager.pixel(2));
            line.set_paddingLeft(Manager.pixel(8));
            line.set_paddingRight(Manager.pixel(8));
            line.set_maxWidth(PanelW);
            line.set_minWidth(PanelW);
            Assets.Class.makeText(text.AsHaxeString(), color, null, line);
            line.reflow();
            return line;
        }

        /// <summary>
        /// 按钮flow
        /// </summary>
        /// <returns></returns>
        protected Flow ButtonRow()
        {
            var flow = new Flow(Flow);
            flow.set_minWidth(PanelW - Manager.pixel(20));
            flow.set_horizontalSpacing(Manager.pixel(8));
            flow.set_verticalAlign(new FlowAlign.Middle());
            flow.set_maxWidth(PanelW);
            flow.multiline = true;
            return flow;
        }

        /// <summary>
        /// 按钮
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="label"></param>
        /// <param name="cb"></param>
        /// <param name="color"></param>
        protected void Button(Flow parent, string label, Action cb, int color = 0xFFFFFF)
        {
            var btn = new Flow(parent);
            btn.set_isVertical(false);
            btn.set_padding(Manager.pixel(4));
            btn.set_minWidth(Manager.pixel(60));
            var text = Assets.Class.makeText(label.AsHaxeString(), color, null, btn);
            btn.set_enableInteractive(true);
            btn.reflow();
            if (btn.interactive != null)
            {
                btn.interactive.width  = btn.calculatedWidth;
                btn.interactive.height = btn.calculatedHeight;
                btn.interactive.onClick = new HlAction<dc.hxd.Event>(_ =>
                {
                    cb();
                    LobbyMenu.PlaySound("sfx/ui/menu_select.wav");
                });
            }

            // 选中高亮
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
                sel.y = text.y * 1.5;
            });
            btn.interactive.onOut = new HlAction<dc.hxd.Event>(_ => sel.visible = false);
        }

        /// <summary>
        /// 构建完成后调用一次 reflow
        /// </summary>
        protected void Done()
        {
            Flow.reflow();
        }

        public virtual_cb_help_inter_isEnable_t_<bool> AddMenuChild(
            string text, HlAction cb, string? help = null, bool? isEnable = null, int color = 0xFFFFFF)
        {
            return TitleScreen.Class.ME.addMenu(text.AsHaxeString(), cb,
                help?.AsHaxeString(), isEnable, Ref<int>.From(ref color));
        }
    }
}
