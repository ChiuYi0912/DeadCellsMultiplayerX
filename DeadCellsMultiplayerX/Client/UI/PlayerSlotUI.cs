using dc;
using dc.h2d;
using dc.libs.heaps.slib;
using dc.tool;
using dc.ui;
using HaxeProxy.Runtime;
using ModCore.Utilities;
using Serilog;

namespace DeadCellsMultiplayerX.Client.UI
{
    internal class PlayerSlotUI
    {
        public Flow Container { get; }
        public Flow? emptybox { get; set; }
        public HSprite? HeroSprite { get; private set; }
        public dc.ui.Text? NameLabel { get; private set; }
        public dc.ui.Text? StatusLabel { get; private set; }
        public GuestInfo? Guest { get; private set; }
        public bool IsOccupied => Guest != null;

        private readonly ILogger logger;
        private readonly LobbyMenu? menu;
        private readonly Loading? loading;
        private readonly Action? OnResizeFlow;

        /// <summary>
        /// 玩家状态变更时触发
        /// </summary>
        public event Action<PlayerSlotUI>? OnChanged;

        internal PlayerSlotUI(Flow parent, Loading loading)
        {
            this.loading = loading;
            logger = Log.ForContext(GetType());
            menu = ClientMain.Instance.lobby;
            Container = new Flow(parent) { isVertical = true };
            Container.set_minWidth(parent.minWidth / 4);
            Container.set_verticalAlign(new FlowAlign.Middle());
            Container.set_horizontalAlign(new FlowAlign.Middle());

            OnResizeFlow = () =>
            {
                Container.set_minWidth(parent.minWidth / 4);
                Container.reflow();
            };

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
            StatusLabel = Assets.Class.makeText("".AsHaxeString(), null, null, Container);

            RefreshStatus();
            OnReszie();
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
            var text = Assets.Class.makeText("[Empty]".AsHaxeString(), 0x666666, null, emptybox);
            text.scaleX = text.scaleY = (double)(menu?.get_pixelScale.Invoke() * 0.25)!;
        }

        public void OnReszie()
        {
            var pixe = menu?.get_pixelScale.Invoke();

            if (emptybox != null)
            {
                foreach (var text in emptybox.children)
                {
                    text.scaleX = text.scaleY = (double)(pixe * 0.25)!;
                    text.posChanged = true;
                }
            }
            if (HeroSprite != null)
            {
                HeroSprite.scaleX = HeroSprite.scaleY = (double)(pixe * 0.5)!;
                HeroSprite.posChanged = true;
            }

            if (StatusLabel != null)
            {
                StatusLabel.scaleX = StatusLabel.scaleY = (double)(pixe * 0.25)!;
                StatusLabel.posChanged = true;
            }

            if (NameLabel != null)
            {
                NameLabel.scaleX = NameLabel.scaleY = (double)(pixe * 0.30)!;
                NameLabel.posChanged = true;
            }

            OnResizeFlow?.Invoke();
        }
    }


    internal class PlayerSlotPanel
    {
        public Flow Container { get; }
        public Flow title { get; }

        public PlayerSlotUI[] Slots { get; }
        private readonly Queue<string> nameQueue = [];

        private readonly ILogger logger;
        private readonly Loading loading;
        private readonly LobbyMenu? lobby;


        private bool isShowingName = false;

        public PlayerSlotPanel(Flow parent, Loading loading)
        {
            logger = Log.ForContext(GetType());
            lobby = ClientMain.Instance.lobby;
            this.loading = loading;

            title = new Flow(null) { isVertical = false };
            title.set_minWidth(parent.minWidth);
            title.set_minHeight(parent.minHeight / 10);
            title.set_maxHeight(parent.minHeight / 10);
            title.set_verticalAlign(new FlowAlign.Top());
            title.set_horizontalAlign(new FlowAlign.Middle());
            parent.addChild(title);

            Container = new Flow(null) { isVertical = false };
            parent.addChild(Container);
            Container.set_minWidth(parent.minWidth);
            Container.set_verticalAlign(new FlowAlign.Middle());
            Container.set_horizontalAlign(new FlowAlign.Middle());

            Slots = new PlayerSlotUI[4];
            for (int i = 0; i < 4; i++)
            {
                Slots[i] = new PlayerSlotUI(Container, loading);
                Slots[i].OnChanged += OnSlotChanged;
            }

            title.reflow();
            Container.reflow();
        }


        public void Refresh(IEnumerable<GuestInfo> guests)
        {
            var guestList = guests.Take(4).ToList();

            for (int i = 0; i < Slots.Length; i++)
            {
                if (Slots[i].Guest != null && !guestList.Any(g => g.Guid == Slots[i].Guest!.Guid))
                    Slots[i].Clear();
            }

            for (int i = 0; i < guestList.Count; i++)
            {
                int index = i;
                var guest = guestList[index];

                if (Slots[index].Guest?.Guid == guest.Guid)
                {
                    Slots[index].RefreshStatus();
                    continue;
                }

                if (Slots[index].Guest != null)
                    Slots[index].Clear();

                lobby?.delayer.addMs(null, () =>
                {
                    try
                    {
                        if (Slots[index].Guest?.Guid != guest.Guid)
                        {
                            Slots[index].Bind(guest);
                            Slots[index].OnReszie();
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Failed to bind player slot {Index}", index);
                    }
                }, 10);
            }
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
            var name = slot.Guest?.Name;
            if (string.IsNullOrEmpty(name)) return;

            nameQueue.Enqueue(name);
            if (!isShowingName)
                ShowNextName();
            LoadingNewPlayer(slot.Guest?.Name!);
        }

        private void ShowNextName()
        {
            if (nameQueue.Count == 0)
            {
                isShowingName = false;
                return;
            }

            isShowingName = true;
            string nextName = nameQueue.Dequeue();
            LoadingNewPlayer(nextName);
        }

        public void LoadingNewPlayer(string name)
        {
            if (lobby == null) return;
            loading.text.alpha = 0;

            var animIn = lobby.CreateTween(() => loading.text.alpha, (v) => loading.text.alpha = v, 1.0, 4);
            animIn?.end(() =>
            {
                loading.text.set_text("".AsHaxeString());
                loading.text.remove();
                RefreshStatuses();
                lobby.rightFlow?.reflow();
                loading.loadingFlow.reflow();
                lobby.onResizeAllloadingFlow?.Invoke();

                ShowNextName();
            });
            loading.text.set_text($"玩家:{name}加入".AsHaxeString());
            lobby.onResizeAllloadingFlow?.Invoke();
        }
    }
}
