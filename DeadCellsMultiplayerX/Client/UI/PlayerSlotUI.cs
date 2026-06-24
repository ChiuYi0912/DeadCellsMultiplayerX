using dc;
using dc.h2d;
using dc.libs.heaps.slib;
using dc.tool;
using HaxeProxy.Runtime;
using ModCore.Utilities;
using Serilog;

namespace DeadCellsMultiplayerX.Client.UI
{
    public class PlayerSlotUI
    {
        public Flow Container { get; }
        public Flow? emptybox { get; set; }
        public HSprite? HeroSprite { get; private set; }
        public dc.ui.Text? NameLabel { get; private set; }
        public dc.ui.Text? StatusLabel { get; private set; }
        public GuestInfo? Guest { get; private set; }
        public bool IsOccupied => Guest != null;

        private readonly ILogger logger;

        /// <summary>
        /// 玩家状态变更时触发
        /// </summary>
        public event Action<PlayerSlotUI>? OnChanged;

        internal PlayerSlotUI(Flow parent)
        {
            logger = Log.ForContext(GetType());

            Container = new Flow(parent) { isVertical = true };
            Container.set_minWidth(parent.minWidth / 4);
            Container.set_verticalAlign(new FlowAlign.Middle());
            Container.set_horizontalAlign(new FlowAlign.Middle());

            ShowEmpty();
        }


        public void Bind(GuestInfo guest)
        {
            if (Guest?.Guid == guest.Guid)
            {
                Guest = guest;
                if (NameLabel != null)
                    NameLabel.set_text(guest.Name.AsHaxeString());
                RefreshStatus();
                return;
            }

            // 新玩家
            Guest = guest;
            ClearContent();

            if (emptybox != null)
            {
                emptybox.removeChildren();
                emptybox.remove();
                emptybox = null;
            }

            string skin = guest.IsHost ? Save.Class.tryLoad().heroSkin.ToString() : guest.SkinMould;
            HeroSprite = UIRenderer.CreateHeroSpr(skin, Container);

            NameLabel = Assets.Class.makeText(guest.Name.AsHaxeString(), 0xDDDDDD, null, Container);
            NameLabel.scaleX = NameLabel.scaleY = 1.2;

            StatusLabel = Assets.Class.makeText("".AsHaxeString(), null, null, Container);
            StatusLabel.scaleX = StatusLabel.scaleY = 1.0;

            Container.reflow();
            RefreshStatus();
            OnChanged?.Invoke(this);
        }

        public void RefreshStatus()
        {
            if (Guest == null || StatusLabel == null) return;

            string status = Guest.IsHost ? "[Host]" :
                            Guest.IsReady ? "[Ready]" : "[...]";
            int sc = Guest.IsHost ? 0xFFDD44 :
                     Guest.IsReady ? 0x44FF44 : 0x888888;

            StatusLabel.set_text(status.AsHaxeString());
            StatusLabel.set_textColor(sc);
        }

        public void Clear()
        {
            if (Guest == null) return;
            Guest = null;
            ClearContent();
            ShowEmpty();
            Container.reflow();
            OnChanged?.Invoke(this);
        }

        private void ClearContent()
        {
            HeroSprite?.remove(); HeroSprite = null;
            NameLabel?.remove(); NameLabel = null;
            StatusLabel?.remove(); StatusLabel = null;
        }

        private void ShowEmpty()
        {
            emptybox = new Flow(Container);
            var empty = Assets.Class.makeText("[Empty]".AsHaxeString(), 0x666666, null, emptybox);
            empty.scaleX = empty.scaleY = 1.0;
        }
    }


    internal class PlayerSlotPanel
    {
        public Flow Container { get; }
        public PlayerSlotUI[] Slots { get; }

        private readonly ILogger logger;

        public PlayerSlotPanel(Flow parent)
        {
            logger = Log.ForContext(GetType());

            Container = new Flow(null) { isVertical = false };
            parent.addChild(Container);
            Container.set_minWidth(parent.minWidth);
            Container.set_verticalAlign(new FlowAlign.Middle());
            Container.set_horizontalAlign(new FlowAlign.Middle());

            Slots = new PlayerSlotUI[4];
            for (int i = 0; i < 4; i++)
            {
                Slots[i] = new PlayerSlotUI(Container);
                Slots[i].OnChanged += OnSlotChanged;
            }

            Container.reflow();
        }


        public void Refresh(IEnumerable<GuestInfo> guests)
        {
            var guestList = guests.Take(4).ToList();

            for (int i = 0; i < 4; i++)
            {
                if (Slots[i].Guest != null && !guestList.Any(g => g.Guid == Slots[i].Guest!.Guid))
                    Slots[i].Clear();
            }

            for (int i = 0; i < guestList.Count; i++)
                Slots[i].Bind(guestList[i]);
        }

        /// <summary>
        /// 刷新所有已占用卡槽
        /// </summary>
        public void RefreshStatuses()
        {
            foreach (var s in Slots)
                s.RefreshStatus();
        }

        public void ClearAll()
        {
            foreach (var s in Slots) s.Clear();
        }

        private void OnSlotChanged(PlayerSlotUI slot)
        {
            logger.Information("Slot changed: {Name}", slot.Guest?.Name ?? "(empty)");
        }
    }
}
