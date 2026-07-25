using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DeadCellsMultiplayerX.Common;
using DeadCellsMultiplayerX.Server.WorldX;
using DeadCellsMultiplayerX.Utils;
using ModCore.Modules;
using StreamJsonRpc;

namespace DeadCellsMultiplayerX.Client.Guest.WorldX
{
    internal class WorldXDataReceive(JsonRpc rpc) : DisposableEventReceiver
    {
        private IWorldDataRPC? worldData = rpc.Attach<IWorldDataRPC>();
        public async Task Init()
        {
            Debug.Assert(worldData != null);
            Logger.Information("WorldXDataReceive Register {hello}", await worldData.Test());
        }

        protected override void MyDispose()
        {
            base.MyDispose();
        }
    }
}