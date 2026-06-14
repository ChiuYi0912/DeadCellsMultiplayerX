using dc;
using dc.hl.types;
using dc.libs;
using dc.pr;
using dc.ui;
using HaxeProxy.Runtime;
using ModCore.Modules;
using ModCore.Utilities;
using Hashlink.Virtuals;

namespace DeadCellsMultiplayerX.Client.Lobby
{
    internal static class TitleScreenHooks
    {
        public static void Init()
        {
            Hook_TitleScreen.mainMenu += Hook_TitleScreen_mainMenu;
        }


        private static void Hook_TitleScreen_mainMenu(
            Hook_TitleScreen.orig_mainMenu orig, TitleScreen self)
        {
            orig(self);

            int color = (255 << 16) | (215 << 8) | 0;
            BuildMenuChild("Online", () => OnlineMenu(self), color: color);

            var wrapper = self.menuItemsWrapper;
            var menu = wrapper.children.getDyn(wrapper.children.length - 1);
            wrapper.removeChild(menu);
            wrapper.addChildAt(menu, 1);

            var item = self.menuItems.pop();
            self.menuItems.insert(1, item);

            self.fControlLabel.reflow();
            self.select(0, default);
        }


        private static void OnlineMenu(TitleScreen screen)
        {
            screen.isMainMenu = false;
            screen.clearMenu();

#if DEBUG
            BuildMenuChild(T("TEST_Server"), () =>
            {
                Test.Start();

            });

            BuildMenuChild(T("TEST_Menu"), () =>
            {
                new LobbyMenu(null!);
            });
#endif

            BuildMenuChild(T("创建房间"), () =>
            {
                //ClientMain.Instance.StartHost("127.0.0.1", 44567);

            });

            BuildMenuChild(T("加入房间"), () =>
            {
                //ClientMain.Instance.StartGuest("127.0.0.1", 44567);

            });

            BuildMenuChild(T("返回"), screen.mainMenu);
        }


        private static virtual_cb_help_inter_isEnable_t_<bool> BuildMenuChild(
            string text,
            HlAction callback,
            string? help = null,
            bool? isEnable = null,
            int color = 0xFFFFFF)
        {
            return TitleScreen.Class.ME.addMenu(
                text.AsHaxeString(),
                callback,
                help?.AsHaxeString(),
                isEnable,
                Ref<int>.From(ref color)
            );
        }

        private static string T(string key) =>
            ModCore.Modules.GetText.Instance.GetString(key);
    }
}
