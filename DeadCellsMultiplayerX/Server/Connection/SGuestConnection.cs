using dc;
using DeadCellsMultiplayerX.Client;
using DeadCellsMultiplayerX.Client.Guest;
using DeadCellsMultiplayerX.Client.Host;
using DeadCellsMultiplayerX.Client.Networks;
using DeadCellsMultiplayerX.Common;
using DeadCellsMultiplayerX.Common.Data;
using DeadCellsMultiplayerX.Server.Events;
using DeadCellsMultiplayerX.Utils;
using Microsoft.VisualStudio.Threading;
using ModCore.Events;
using ModCore.Events.Interfaces.Game;
using ModCore.Modules;
using ModCore.Utilities;
using Serilog;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace DeadCellsMultiplayerX.Server
{
    internal partial class SGuestConnection :
        DisposableEventReceiver,
        IOnServerEnterNewLevel,
        IOnEntitySetColorMap
    {
        private class EntityInfo2
        {
            public readonly EntityInfo info = new()
            {
                GUID = Guid.NewGuid().ToString()
            };
            public Entity? entity;
        }

        private readonly JsonRpc rpc;
        public GuestInfo guestInfo = new();
        public override ILogger Logger { get; }
        public GuestInfo GuestInfo { get; set; } = new();
        public ServerSession Session { get; }
        public ServerMainThread Main => Session.Main;
        public IGuestRPC guest;

        private readonly Dictionary<int, EntityInfo2> entitiesInfo = [];

        public SGuestConnection(ServerSession session, Stream connection)
        {
            Session = session;
            Logger = Log.ForContext("SourceContext", "Server-Guest-" + guestInfo.Guid);

            rpc = connection.CreateJsonRpc();

            rpc.AddLocalRpcTarget(this);

            guest = rpc.Attach<IGuestRPC>();

            rpc.SynchronizationContext = Game.SynchronizationContext;

            rpc.Disconnected += Rpc_Disconnected;

            rpc.StartListening();

            Logger.Information("Connected.");
        }

        private void Rpc_Disconnected(object? sender, JsonRpcDisconnectedEventArgs e)
        {
            if (e.Reason == DisconnectedReason.LocallyDisposed)
            {
                return;
            }
            Logger.Error(e.Exception, "Abort connection: {reason}: {desc}", e.Reason, e.Description);
            Dispose();
        }

        protected override void MyDispose()
        {
            base.MyDispose();

            rpc?.Dispose();
        }

        private EntityInfo2 GetEntityInfo(Entity e)
        {
            if(!entitiesInfo.TryGetValue(e.__uid, out var result))
            {
                result = new()
                {
                    entity = e,
                };
                entitiesInfo.Add(e.__uid, result);
            }
            result.entity = e;
            return result;
        }
       
        void IOnServerEnterNewLevel.OnServerEnterNewLevel()
        {
            Debug.Assert(Main.savePath != null);

            guest.EnterNewLevel(File.ReadAllBytes(Main.savePath));
        }

        void IOnEntitySetColorMap.OnEntitySetColorMap(IOnEntitySetColorMap.Data data)
        {
            var info = GetEntityInfo(data.Entity).info;
            info.ColorMapSkin = data.Skin;
            info.ColorMapModel = data.Model;

            Logger.Information("Set colormap: {guid} {model} {skin}", info.GUID, info.ColorMapModel, info.ColorMapSkin);
        }
    }
}
