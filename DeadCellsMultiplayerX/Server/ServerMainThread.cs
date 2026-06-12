using dc;
using DeadCellsMultiplayerX.Common;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeadCellsMultiplayerX.Server
{
    /// <summary>
    /// 这个类的所有 Method 应该在主线程执行
    /// </summary>
    internal class ServerMainThread(ServerSession session) : DisposableEventReceiver
    {
        public ILogger Logger { get; } = Log.Logger.ForContext<ServerMainThread>();
        private static void CheckMainThread()
        {
            if(Thread.CurrentThread != ServerMain.Instance.MainThread)
            {
                throw new InvalidOperationException();
            }
        }
        private static Task RunOnMainThread()
        {
            SynchronizationContext.SetSynchronizationContext(ModCore.Modules.Game.SynchronizationContext);
            return Task.Delay(1);
        }

        /// <summary>
        /// 
        /// </summary>
        public async Task LaunchGame()
        {
            await RunOnMainThread();

            CheckMainThread();

            Logger.Information("Launching game...");

            Main.Class.ME.launchGame(new LaunchMode.NewGame(false, false), null, null);

            
        }
    }
}
