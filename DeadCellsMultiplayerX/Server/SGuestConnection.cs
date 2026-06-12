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
using ModCore.Modules;
using Serilog;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DeadCellsMultiplayerX.Server
{
    internal class SGuestConnection :
        DisposableEventReceiver,
        IServerRPC,
        IOnServerEnterNewLevel
    {
        private readonly JsonRpc rpc;
        public GuestInfo guestInfo = new();
        public ILogger Logger { get; }
        public GuestInfo GuestInfo { get; set; } = new();
        public ServerSession Session { get; }
        public ServerMainThread Main => Session.Main;
        public IGuestRPC guest;
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



        // IServerRPC

        public Task<bool> CheckVersion(string version)
        {
            if (Version.Parse(version) != VersionUtils.ModVersion)
            {
                Logger.Error("Dismatch version: {ver}", guestInfo.Guid, version);
                Task.Delay(TimeSpan.FromSeconds(5)).ContinueWith(_ =>
                {
                    if (rpc == null)
                    {
                        return;
                    }
                    if (!rpc.IsDisposed)
                    {
                        Dispose();
                    }
                });
                return Task.FromResult(false);
            }
            return Task.FromResult(true);
        }

        public Task SetGuestInfo(GuestInfo info)
        {
            GuestInfo = info;
            return Task.CompletedTask;
        }

        public async Task UploadSavedata(byte[] data)
        {
            await Task.Delay(1).ConfigureAwait(false);

            var savePath = ServerMain.Instance.savePath;

            await File.WriteAllBytesAsync(savePath, data);

            Logger.Information("Saving savedata to {path}", savePath);

            Logger.Information("Building game...");

            await Main.LaunchGame();

            await Task.Delay(1).ConfigureAwait(false);
        }

        public async Task<byte[]> DownloadSavedata()
        {
            while(string.IsNullOrEmpty(Main.savePath))
            {
                await Task.Yield();
            }
            return File.ReadAllBytes(Main.savePath);
        }

        void IOnServerEnterNewLevel.OnServerEnterNewLevel()
        {
            Debug.Assert(Main.savePath != null);

            guest.EnterNewLevel(File.ReadAllBytes(Main.savePath));
        }

        public async Task<AreaInfo> RequestAreaInfo(IServerRPC.AreaInfoRequest request)
        {
            if(request.X < 0)
            {
                request.X = 0;
            }
            if (request.Y < 0)
            {
                request.Y = 0;
            }
            var areaInfo = new AreaInfo()
            {
                X = request.X,
                Y = request.Y,
                Width = request.Width,
                Height = request.Height
            };
            return areaInfo;
        }

        public Task<EntityInfo> RequestEntityInfo(string guid)
        {
            throw new NotImplementedException();
        }

        //

    }
}
