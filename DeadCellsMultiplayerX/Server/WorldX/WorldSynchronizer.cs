using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dc;
using DeadCellsMultiplayerX.Common;
using DeadCellsMultiplayerX.Common.Data;
using DeadCellsMultiplayerX.Common.Serializers;
using DeadCellsMultiplayerX.Server.Connection;
using DeadCellsMultiplayerX.Server.Events;
using DeadCellsMultiplayerX.Utils;
using ModCore.Modules;
using StreamJsonRpc;

namespace DeadCellsMultiplayerX.Server.WorldX
{
    internal partial class WorldSynchronizer() : DisposableEventReceiver,
    IWorldDataRPC
    {
        Task<string> IWorldDataRPC.Test()
        {
            return Task.FromResult("WorldSynchronizer Init...");
        }

        

        protected override void MyDispose()
        {
            base.MyDispose();
        }

        Task<Dictionary<string, byte[]>> IWorldDataRPC.GetLevelMobs()
        {
            return Task.FromResult(dynamicLevelMobs);
        }
    }
}