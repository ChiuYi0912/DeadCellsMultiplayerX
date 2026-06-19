using CoreLibrary.Core.Utilities;
using dc;
using dc.en.mob;
using dc.h2d;
using dc.h2d.col;
using dc.hxd;
using dc.libs.heaps.slib;
using dc.pr;
using dc.shader;
using dc.ui;
using Hashlink.Virtuals;
using HashlinkNET.Native.Impl;
using HaxeProxy.Runtime;
using ModCore.Utilities;

namespace DeadCellsMultiplayerX.Client.UI
{
    internal abstract class BasePageUI
    {
        protected LobbyMenu Manager { get; }
        protected FlowBox Flow { get; private set; } = null!;
        protected int PanelW { get; private set; }
        public string Name { get; }
        public bool lockInter = false;


        public  List<ReleaseNotes> Notes =[];
        public List<Flow> SprVacancies =[];

        /// <summary>
        /// 左侧按钮动作
        /// </summary>
        public Action? BuildHost   { get; set; }
        public Action? BuildClient { get; set; }

        public Flow Information { get;private set; } =null!;

        protected BasePageUI(LobbyMenu manager, string menuName)
        {
            Manager = manager;
            Name = menuName;
            Writelog();
        }

        public void BuildRight(FlowBox right, int panelW)
        {
            PanelW = panelW;
            Flow = right;

            Spacer(Flow);
            BuildInformation();
            BuildContent();
        }

        public abstract void BuildContent();
        public abstract void BuildLeftMenuChild();
        public abstract void update();


        /// <summary>
        /// 测试翻页
        /// </summary>
        private void Writelog()
        {
            var note = new ReleaseNotes(
                "2.1.0",
                new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.FromHours(8)),
                "Summer Update"
            );
            note.Changes.Add(new ChangeEntry(ChangeType.Feature, "添加了.."));
            note.Changes.Add(new ChangeEntry(ChangeType.BugFix, "修复登录页面崩溃问题"));
            note.Changes.Add(new ChangeEntry(ChangeType.Other, "其他"));
            note.Changes.Add(new ChangeEntry(ChangeType.Breaking, "删除..."));
            Notes.Add(note);

            note = new ReleaseNotes(
                "2.1.0",
                new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.FromHours(8)),
                "Summer Update"
            );
            note.Changes.Add(new ChangeEntry(ChangeType.Feature, "添加了.."));
            note.Changes.Add(new ChangeEntry(ChangeType.BugFix, "修复登录页面崩溃问题"));
            note.Changes.Add(new ChangeEntry(ChangeType.Other, "其他"));
            note.Changes.Add(new ChangeEntry(ChangeType.Breaking, "删除..."));
            Notes.Add(note);

            note = new ReleaseNotes(
                "2.1.0",
                new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.FromHours(8)),
                "Summer Update"
            );
            note.Changes.Add(new ChangeEntry(ChangeType.Feature, "添加了.."));
            note.Changes.Add(new ChangeEntry(ChangeType.BugFix, "修复登录页面崩溃问题"));
            note.Changes.Add(new ChangeEntry(ChangeType.Other, "其他"));
            note.Changes.Add(new ChangeEntry(ChangeType.Breaking, "删除..."));
            Notes.Add(note);

            note = new ReleaseNotes(
                "2.1.0",
                new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.FromHours(8)),
                "Summer Update"
            );
            note.Changes.Add(new ChangeEntry(ChangeType.Feature, "添加了.."));
            note.Changes.Add(new ChangeEntry(ChangeType.BugFix, "修复登录页面崩溃问题"));
            note.Changes.Add(new ChangeEntry(ChangeType.Other, "其他"));
            note.Changes.Add(new ChangeEntry(ChangeType.Breaking, "删除..."));
            Notes.Add(note);

            note = new ReleaseNotes(
                "2.1.0",
                new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.FromHours(8)),
                "Summer Update"
            );
            note.Changes.Add(new ChangeEntry(ChangeType.Feature, "添加了.."));
            note.Changes.Add(new ChangeEntry(ChangeType.BugFix, "修复登录页面崩溃问题"));
            note.Changes.Add(new ChangeEntry(ChangeType.Other, "其他"));
            note.Changes.Add(new ChangeEntry(ChangeType.Breaking, "删除..."));
            Notes.Add(note);

            note = new ReleaseNotes(
                "2.1.0",
                new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.FromHours(8)),
                "Summer Update"
            );
            note.Changes.Add(new ChangeEntry(ChangeType.Feature, "添加了.."));
            note.Changes.Add(new ChangeEntry(ChangeType.BugFix, "修复登录页面崩溃问题"));
            note.Changes.Add(new ChangeEntry(ChangeType.Other, "其他"));
            note.Changes.Add(new ChangeEntry(ChangeType.Breaking, "删除..."));
            Notes.Add(note);

            note = new ReleaseNotes(
                "2.1.0",
                new DateTimeOffset(2026, 6, 15, 10, 0, 0, TimeSpan.FromHours(8)),
                "Summer Update"
            );
            note.Changes.Add(new ChangeEntry(ChangeType.Feature, "添加了.."));
            note.Changes.Add(new ChangeEntry(ChangeType.BugFix, "修复登录页面崩溃问题"));
            note.Changes.Add(new ChangeEntry(ChangeType.Other, "其他"));
            note.Changes.Add(new ChangeEntry(ChangeType.Breaking, "删除..."));
            Notes.Add(note);
        }

        /// <summary>
        /// 基本信息
        /// </summary>
        private void BuildInformation()
        {
            Information = new Flow(Manager.rightFlowMain);
            var right = Information;

            if (right == null) return;

            int w = PanelW/2;
            int h = Manager.ScreenH / 4;

            double padH = 0, padV = 0;
            var container = LobbyMenu.CreateBoxLegendaryOutline(right, ref padH, ref padV);
            container.set_isVertical(true);
            container.set_minWidth(w);
            container.set_maxWidth(w);
            container.set_minHeight(h);
            container.set_horizontalAlign(new FlowAlign.Middle());
            container.reflow();

            var box = container.box;
            double ps = Manager.get_pixelScale.Invoke();
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

            int scrollPad = Manager.pixel(10);

            var contentContainer = new dc.h2d.Object(null);
            mask.addChild(contentContainer);
            contentContainer.x = Manager.pixel(10);
            contentContainer.y = scrollPad;

            var contentFlow = new Flow(contentContainer);
            contentFlow.set_isVertical(true);
            contentFlow.set_minWidth(maskW - Manager.pixel(20));
            contentFlow.set_maxWidth(maskW - Manager.pixel(20));
            AddText("Release Notes", null, contentFlow, new FlowAlign.Middle(),2);

            foreach (var note in Notes)
            {
                var header = AddText(
                    $"Version:{note.Version}\nLast updated:{note.ReleaseTime}",
                    null, contentFlow, new FlowAlign.Left(), 1.5, Manager.pixel(10));

                foreach (var change in note.Changes)
                    AddText($"{change.Type}:{change.Description}",
                        GetColor(change.Type), contentFlow, new FlowAlign.Left(), 1.4, Manager.pixel(30));

                contentFlow.getProperties(header).paddingTop=Manager.pixel(10);
            }

            contentFlow.reflow();

            var bounds = contentContainer.getBounds(null, null);
            double maxScrollY = System.Math.Max(0, bounds.yMax - bounds.yMin - maskH);

            var inter = new Interactive(maskW, maskH, mask, null) { propagateEvents = true };
            inter.onWheel = e =>
            {
                if (e.wheelDelta == 0) return;
                double newY = contentContainer.y - e.wheelDelta * Manager.pixel(30);
                newY = System.Math.Max(-maxScrollY - scrollPad, System.Math.Min(scrollPad, newY));
                contentContainer.posChanged = true;
                contentContainer.y = newY;
            };

            static int GetColor(ChangeType type) => type switch
            {
                ChangeType.Feature     => 0x00FF00,
                ChangeType.Improvement => 0x0000FF,
                ChangeType.BugFix      => 0xFF0000,
                ChangeType.Breaking    => 0xFF00FF,
                _                      => 0xFFFFFF,
            };

            dc.ui.Text AddText(string text, int? color, Flow parent, FlowAlign hAlign,
            double scale = 1.0, int padLeft = 0,int paddingTop =0)
            {
                var t = Assets.Class.makeText(text.AsHaxeString(), color ?? 0xFFFFFF, null, parent);
                t.scaleX = t.scaleY = scale;
                parent.getProperties(t).horizontalAlign = hAlign;
                if (padLeft > 0) parent.getProperties(t).paddingLeft = padLeft;
                if (paddingTop>0) parent.getProperties(t).paddingTop =paddingTop;

                return t;
            }


            var info = LobbyMenu.CreateBoxLegendaryOutline(right, ref padH, ref padV);
            info.set_isVertical(true);
            info.set_minWidth(w);
            info.set_maxWidth(w);
            info.set_minHeight(h);
            info.set_horizontalAlign(new FlowAlign.Middle());
            info.reflow();

            var mod = ModEntry.Instance.Info;
            AddText($"Mod Information:",null, info, new FlowAlign.Middle(), 2, paddingTop: Manager.pixel(10));
            AddText($"Mod name: {mod.Name}", null, info, new FlowAlign.Middle(), 1.5,paddingTop:Manager.pixel(10));
            AddText("Lead author: HKLab", null, info, new FlowAlign.Middle(), 1.5, paddingTop: Manager.pixel(10));
            AddText("Contribution: ChiuYi", null, info, new FlowAlign.Middle(), 1.5, paddingTop: Manager.pixel(10));
            AddText($"Current version: {mod.Version}", null, info, new FlowAlign.Middle(), 1.5, paddingTop: Manager.pixel(10));
        }

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
            t.maxWidthWanted = PanelW;
            t.onResize();
            Flow.getProperties(t).horizontalAlign = new FlowAlign.Middle();
            Flow.getProperties(t).verticalAlign = new FlowAlign.Top();
        }

        /// <summary>
        /// 分割线
        /// </summary>
        protected void Spacer(Flow flow)
        {
            var g = new Graphics(flow);
            g.beginFill(Ref<int>.In(0xFFFFFF), Ref<double>.In(0.15));
            double ps = Manager.get_pixelScale.Invoke();
            g.drawRect(0, 0, PanelW/2, 5);
            g.endFill();
            flow.getProperties(g).verticalAlign = new FlowAlign.Top();
            flow.getProperties(g).horizontalAlign = new FlowAlign.Middle();
        }

        /// <summary>
        /// 文本行
        /// </summary>
        /// <param name="text"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        protected Flow Row(string? text, int color = 0xDDDDDD)
        {
            var line = new Flow(Flow);
            line.set_isVertical(true);
            line.set_verticalAlign(new FlowAlign.Middle());
            line.set_maxWidth(PanelW);
            line.set_minWidth(PanelW);
            line.set_padding(Manager.pixel(10));
            if (text!=null)
            {
                var labe = Assets.Class.makeText(text.AsHaxeString(), color, null, line);
                labe.scaleX = labe.scaleY = 1.2;
                line.getProperties(labe).horizontalAlign = new FlowAlign.Middle();
                line.getProperties(labe).verticalAlign = new FlowAlign.Top();
            }
               
            
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
            flow.set_verticalAlign(new FlowAlign.Middle());
            flow.set_horizontalAlign(new FlowAlign.Middle());
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
            btn.set_verticalAlign(new FlowAlign.Top());
            btn.set_horizontalAlign(new FlowAlign.Middle());
            var text = Assets.Class.makeText(label.AsHaxeString(), color, null, btn);
            btn.set_enableInteractive(true);
            btn.reflow();
            if (btn.interactive != null)
            {
                btn.interactive.width  = btn.calculatedWidth;
                btn.interactive.height = btn.calculatedHeight;
                btn.interactive.onClick = new HlAction<dc.hxd.Event>(_ =>
                {
                    Manager.lockInter=true;
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

            btn.interactive!.onMove = new (_ =>
            {
                Manager.lockInter = true;
                sel.visible = true;
                sel.scaleX = sel.scaleY = text.scaleX;
                sel.y = text.y * 1.5;
            });
            btn.interactive.onOut = new (_ => {
                sel.visible = false;
                Manager.lockInter=false;
            });
            btn.interactive.onCheck=new((_)=>updateHSprite(sel));
        }

        private void updateHSprite(HSprite spr)
        {
            double? v = ((HaxeProxyBase)Data.Class.gui.byId.get("co_blinkCursorSpeed".AsHaxeString())).ToVirtual<virtual_biome_color_comment_id_v0_>().v0;
            if(v==null)return;
            double cos = Lib_std.math_cos.Invoke((double)(Manager.ftime *0.1* v!));
            spr.alpha = 0.8 + 0.2 * cos;
        }


        public Flow PlayerVacancies(Flow flow)
        {
            var spritesflow = new Flow(flow);
            spritesflow.set_verticalAlign(new FlowAlign.Middle());
            spritesflow.set_horizontalAlign(new FlowAlign.Middle());
            spritesflow.isVertical = true;
            spritesflow.set_minWidth(Flow.minWidth/4);
            return spritesflow;
        }

        public HSprite AddHeroSpr(Flow flow,string sprmuld = "Tick4")
        {
            dc.String idle = "idle".AsHaxeString();
            SpriteLib g = Assets.Class.getHeroLib(Cdb.Class.getSkinInfo(sprmuld.AsHaxeString()));
            var spriteui = new HSprite(g, idle, Ref<int>.Null, null);
            spriteui.scaleX =spriteui.scaleY=2.0;
            initColorMap(sprmuld,spriteui);

            AnimManager animManager = spriteui.get_anim().play(idle, null, null).loop(null);
            animManager.genSpeed = 0.4;

            spriteui.set_visible(true);
            flow.addChild(spriteui);
            flow.reflow();

            return spriteui;
        }

        private void initColorMap(string colorMap, HSprite spriteui)
        {
            ColorMap shader = (ColorMap)spriteui!.getShader(ColorMap.Class);
            if (shader != null)
                spriteui.removeShader(shader);
            

            var texture = Res.Class.load("atlas/beheaded_aladdin_s.png".AsHaxeString()).toTexture();
            texture.set_filter(new dc.h3d.mat.Filter.Nearest());

            var skinInfo = ((HaxeProxyBase)Cdb.Class.getSkinInfo(colorMap.AsHaxeString())).ToVirtual<virtual_colorMap_consoleCmdId_glowData_group_head_incompatibleHeads_item_model_onlyDefaultHead_scarfBlendMode_scarfs_>();
            spriteui.addShader(new ColorMap(Assets.Class.getHeroColorMap(skinInfo)));


            // DirLighted s2 = new DirLighted();
            // s2 = (DirLighted)spriteui.addShader(s2);


            // var normalMapFromGroup = spriteui.lib.getNormalMapFromSprite(spriteui);
            // NormalMap normal = new NormalMap(normalMapFromGroup);
            // spriteui.addShader(normal);
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
            var label = TitleScreen.Class.ME.addMenu(text.AsHaxeString(), cb,
                help?.AsHaxeString(), isEnable, Ref<int>.From(ref color));
            var onMove = label.inter.onMove;
            label.inter.onMove=new HlAction<dc.hxd.Event>((e) =>
            {
                if(lockInter)return;
                onMove.Invoke(e);
            });
            return label;
        }
    }
}
