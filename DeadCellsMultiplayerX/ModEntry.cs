using dc.pr;
using DeadCellsMultiplayerX.Client;
using DeadCellsMultiplayerX.Utils;
using ModCore.Mods;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeadCellsMultiplayerX
{
    internal class ModEntry(ModInfo info) : ModBase(info)
    {

        /// <summary>
        /// 初始化
        /// </summary>
        public override void Initialize()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DCCM_MULTIPLAYER_NO_CLIENT")))
            {
                return;
            }

            Environment.SetEnvironmentVariable("DCCM_MULTIPLAYER_NO_CLIENT", "1");

            new ClientMain().Init();

            //Test
            Hook_TitleScreen.initTitleScreen += Hook_TitleScreen_initTitleScreen;
        }

        private void Hook_TitleScreen_initTitleScreen(Hook_TitleScreen.orig_initTitleScreen orig, TitleScreen self,
            dc.libs.heaps.slib.SpriteLib titleLib, HaxeProxy.Runtime.Ref<int> bgType)
        {
            orig(self, titleLib, bgType);

            Test.Start();
        }
    }
}
