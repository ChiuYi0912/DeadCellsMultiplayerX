using dc;
using dc.h2d;
using dc.h3d;
using dc.hxd;
using dc.hxsl;
using dc.libs.heaps.slib;
using dc.shader;
using Hashlink.Virtuals;
using HaxeProxy.Runtime;
using ModCore.Utilities;
using Serilog.Core;

namespace DeadCellsMultiplayerX.Client.UI
{
    public class PlayerSlotUI
    {
        public Flow      Container  { get; }
        public HSprite?  HeroSprite { get; private set; }
        public dc.ui.Text? NameLabel  { get; private set; }
        public dc.ui.Text? StatusLabel { get; private set; }
        public GuestInfo? Guest      { get; private set; }

        public bool IsOccupied => Guest != null;

        private readonly LobbyMenu _mgr;

        internal PlayerSlotUI(LobbyMenu mgr, Flow parent)
        {
            _mgr = mgr;

            Container = new Flow(parent) { isVertical = true };
            Container.set_minWidth(parent.minWidth / 4);
            Container.set_verticalAlign(new FlowAlign.Middle());
            Container.set_horizontalAlign(new FlowAlign.Middle());

            ShowEmpty();
        }

        public void Bind(GuestInfo guest, string skinMould = "Tick4")
        {
            Guest = guest;
            ClearContent();
            var skinInfo = Cdb.Class.getSkinInfo(skinMould.AsHaxeString()).ToVirtual<virtual_colorMap_consoleCmdId_glowData_group_head_incompatibleHeads_item_model_onlyDefaultHead_scarfBlendMode_scarfs_>();

            var idle = "idle".AsHaxeString();
            var lib  = Assets.Class.getHeroLib(Cdb.Class.getSkinInfo(skinMould.AsHaxeString()));
            HeroSprite = new HSprite(lib, idle, Ref<int>.Null, null)
            {
                scaleX = 2.0,
                scaleY = 2.0,
            };
            var anim = HeroSprite.get_anim().play(idle, null, null)?.loop(null);
            if (anim != null) anim.genSpeed = 0.4;
            HeroSprite.set_visible(true);
            Container.addChild(HeroSprite);

            NameLabel = Assets.Class.makeText(
                guest.Name.AsHaxeString(), 0xDDDDDD, null, Container);
            NameLabel.scaleX = NameLabel.scaleY = 1.2;

            string status = guest.IsHost  ? "[Host]"  :
                            guest.IsReady ? "[Ready]" : "[...]";
            int sc = guest.IsHost  ? 0xFFDD44 :
                     guest.IsReady ? 0x44FF44 : 0x888888;
            StatusLabel = Assets.Class.makeText(
                status.AsHaxeString(), sc, null, Container);
            StatusLabel.scaleX = StatusLabel.scaleY = 1.0;


            Res.Class.load("atlas/beheaded_aladdin_s.png".AsHaxeString()).toTexture().set_filter(new dc.h3d.mat.Filter.Nearest());

            
            HeroSprite.addShader(new ColorMap(Assets.Class.getHeroColorMap(skinInfo)));
            var scene =_mgr.root.getScene();
            var light = new DirLighted();
            var colorMap = new NormalMap(HeroSprite.lib.getNormalMapFromSprite(HeroSprite));

            int shadowId = -1;
            int dirId = -1;

            var g = light.shader.globals;
            var globals = scene.ctx.manager.globals;
            for (int i = 0; i < g.length; i++)
            {
                ShaderGlobal c = g.getDyn(i);

                var name = c.v.name.ToString();
                var parent =c.v.parent.name.ToString();

                if (name == "shadowColor" && parent == "light")
                    shadowId = c.globalId;

                if (name == "dirVec" && parent == "light")
                    dirId = c.globalId;

                if(shadowId!=-1&&dirId!=-1)break;
            }
            globals.map.set(shadowId, new Vector(Ref<double>.In(1), Ref<double>.In(1), Ref<double>.In(1), Ref<double>.In(1))); // shadowColor
            globals.map.set(dirId, new Vector(Ref<double>.In(0), Ref<double>.In(0), Ref<double>.In(1), Ref<double>.In(1))); // dirVec

            HeroSprite.addShader(colorMap);
            HeroSprite.addShader(light);
            

            Container.reflow();
        }

        public void Clear()
        {
            Guest = null;
            ClearContent();
            ShowEmpty();
            Container.reflow();
        }

        private void ClearContent()
        {
            HeroSprite?.remove();  HeroSprite  = null;
            NameLabel?.remove();   NameLabel   = null;
            StatusLabel?.remove(); StatusLabel = null;
        }

        private void ShowEmpty()
        {
            var empty = Assets.Class.makeText(
                "[Empty]".AsHaxeString(), 0x666666, null, Container);
            empty.scaleX = empty.scaleY = 1.0;
        }
    }

    internal class PlayerSlotPanel
    {
        public Flow            Container { get; }
        public PlayerSlotUI[]  Slots     { get; }

        public PlayerSlotPanel(LobbyMenu mgr, Flow parent)
        {
            Container = new Flow(parent) { isVertical = false };
            Container.set_verticalAlign(new FlowAlign.Middle());
            Container.set_horizontalAlign(new FlowAlign.Middle());

            Slots = new PlayerSlotUI[4];
            for (int i = 0; i < 4; i++)
                Slots[i] = new PlayerSlotUI(mgr, Container);

            Container.reflow();
        }


        public void Refresh(System.Collections.Generic.IEnumerable<GuestInfo> guests)
        {
            int i = 0;
            foreach (var g in guests)
            {
                if (i >= 4) break;
                Slots[i].Bind(g);
                i++;
            }
            for (; i < 4; i++)
                Slots[i].Clear();
        }

        public void ClearAll()
        {
            foreach (var s in Slots) s.Clear();
        }
    }
}
