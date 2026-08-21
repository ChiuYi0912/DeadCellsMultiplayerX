using System.Diagnostics;
using dc.level;
using DeadCellsMultiplayerX.Common;
using DeadCellsMultiplayerX.Common.Serializers;
using DeadCellsMultiplayerX.Server.WorldX;
using StreamJsonRpc;

namespace DeadCellsMultiplayerX.Client.Guest.WorldX
{
    internal partial class WorldXDataReceive(JsonRpc rpc) : DisposableEventReceiver
    {
        private IWorldDataRPC? worldData = rpc.Attach<IWorldDataRPC>();
        public async Task Init()
        {
            Debug.Assert(worldData != null);
            Logger.Information("WorldXDataReceive Register {hello}", await worldData.Test());
        }

        public async Task<Dictionary<string, dc.level.Mob>> GetServerMobs()
        {
            Debug.Assert(worldData != null);

            var rawDict = await worldData.GetLevelMobs();
            return rawDict.ToDictionary(
                kv => kv.Key,
                kv => DCMXSerializers.MessagePack.Deserialize<dc.level.Mob>(kv.Value)!
            );
        }


        protected override void MyDispose()
        {
            base.MyDispose();
        }
    }
}