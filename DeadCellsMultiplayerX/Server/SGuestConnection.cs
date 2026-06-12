using DeadCellsMultiplayerX.Client;
using DeadCellsMultiplayerX.Client.Host;
using DeadCellsMultiplayerX.Client.Networks;
using DeadCellsMultiplayerX.Common;
using DeadCellsMultiplayerX.Utils;
using Microsoft.VisualStudio.Threading;
using ModCore.Events;
using ModCore.Modules;
using Serilog;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeadCellsMultiplayerX.Server
{
    internal class SGuestConnection :
        DisposableEventReceiver,
        IServerRPC
    {
        private readonly JsonRpc rpc;
        public GuestInfo guestInfo = new();
        public ILogger Logger { get; }
        public GuestInfo GuestInfo { get; set; } = new();
        public ServerSession Session { get; }
        public ServerMainThread Main => Session.Main;
        public SGuestConnection(ServerSession session, Stream connection)
        {
            Session = session;
            Logger = Log.ForContext("SourceContext", "Server-Guest-" + guestInfo.Guid);

            rpc = connection.CreateJsonRpc();

            rpc.AddLocalRpcTarget(this);

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

        public async Task UploadSavedata(Stream savedata, int length)
        {
            await Task.Delay(1).ConfigureAwait(false);

            Logger.Information("Downloading savedata({size})...", length);

            using var reader = new BinaryReader(savedata, Encoding.UTF8, false);
            var data = reader.ReadBytes(length);
            var savePath = ServerMain.Instance.savePath;

            await File.WriteAllBytesAsync(savePath, data);

            Logger.Information("Saving savedata to {path}", savePath);

            Logger.Information("Building game...");

            await Main.LaunchGame();

            await Task.Delay(1).ConfigureAwait(false);
        }

        public Task<(Stream, int)> DownloadSavedata()
        {
            throw new NotImplementedException();
        }

        //

    }
}
