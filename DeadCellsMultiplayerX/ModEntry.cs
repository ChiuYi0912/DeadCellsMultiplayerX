using dc;
using dc.pr;
using DeadCellsMultiplayerX.Client;
using DeadCellsMultiplayerX.Utils;
using ModCore.Mods;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
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

            //可以捕获到奇怪的报错
            #if DEBUG
                Hook_Boot.mainLoop+= Hook_Boot_mainLoop;
            #endif
        }

        private void Hook_Boot_mainLoop(Hook_Boot.orig_mainLoop orig, Boot self)
        {
            try
            {
                orig(self);
            }
            catch (Exception ex)
            {
                Logger.Error("{ex}", ex);
                //System.Diagnostics.Debugger.Break();
                ExceptionDispatchInfo.Capture(ex).Throw();
                throw;
            }
        }
    }
}
