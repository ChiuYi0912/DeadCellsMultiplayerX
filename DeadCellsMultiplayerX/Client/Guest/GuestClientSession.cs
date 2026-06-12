using dc.tool;
using DeadCellsMultiplayerX.Client.Host;
using DeadCellsMultiplayerX.Server;
using DeadCellsMultiplayerX.Utils;
using ModCore.Modules;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DeadCellsMultiplayerX.Client.Guest
{
    /// <summary>
    /// 访客的客户端 session
    /// </summary>
    internal class GuestClientSession(GuestClient client, Stream serverStream) : ClientSession
    {
        private JsonRpc? rpc;
        private IServerRPC server = null!;
        private bool isOwner = false;

        public override async Task Init()
        {
            rpc = serverStream.CreateJsonRpc();

            server = rpc.Attach<IServerRPC>();

            rpc.Disconnected += Rpc_Disconnected;
            rpc.StartListening();

            if (!await server.CheckVersion(
               VersionUtils.ModVersion.ToString()
               ))
            {
                Logger.Information("Failed to connect lobby. Dismatch version.");
                Dispose();
                return;
            }

            Debug.Assert(client.LobbyInfo != null);

            if (client.LobbyInfo.Owner == client.Guid)
            {
                isOwner = true;
            }

            Logger.Information("Updating guest info...");

            await server.SetGuestInfo(client.LobbyInfo.Guests[client.Guid]);

            if (isOwner)
            {
                //上传存档到服务器
                Logger.Information("Updating savedata...");
                var fp = System.IO.Path.GetFullPath("save/" + Save.Class.fileName(null).ToString());

                //必须使用现有存档进行联机
                Debug.Assert(System.IO.File.Exists(fp));

                using var fs = System.IO.File.OpenRead(fp);
                await server.UploadSavedata(fs, (int)fs.Length);
            }

            Logger.Information("Downloading savedata...");
            
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

            serverStream?.Dispose();
        }
    }
}
