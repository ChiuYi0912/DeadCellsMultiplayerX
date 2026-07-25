using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeadCellsMultiplayerX.Common;
using DeadCellsMultiplayerX.Utils;
using ModCore.Modules;
using StreamJsonRpc;

namespace DeadCellsMultiplayerX.Server.WorldX
{
    public class WorldXDataDirector : DisposableEventReceiver,
    IWorldDataRPC
    {
        public WorldXDataDirector()
        {
            
        }

        protected override void MyDispose()
        {
            base.MyDispose();
        }

        Task<string> IWorldDataRPC.Test()
        {
            return Task.FromResult("====hello world===");
        }
    }
}