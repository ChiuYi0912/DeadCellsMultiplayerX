using dc;
using dc.en;
using dc.en.inter;
using dc.tool;
using DeadCellsMultiplayerX.Client.Host;
using DeadCellsMultiplayerX.Server;
using DeadCellsMultiplayerX.Utils;
using ModCore.Modules;
using ModCore.Utilities;
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
    internal class GuestClientSession(GuestClient client, Stream serverStream) : ClientSession, IGuestRPC
    {
        private JsonRpc? rpc;
        private IServerRPC server = null!;
        private bool isOwner = false;
        private byte[]? saveData = null;

        public override async Task Init()
        {
            rpc = serverStream.CreateJsonRpc();
            rpc.AddLocalRpcTarget(this);

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

                await server.UploadSavedata(await System.IO.File.ReadAllBytesAsync(fp));
            }

            Hook__Save.save += Hook__Save_save;
            dc.tool.Hook__File.getBytes += Hook__File_getBytes;
        }

        private dc.haxe.io.Bytes Hook__File_getBytes(Hook__File.orig_getBytes orig, dc.String file)
        {
            var fn = file.ToString();
            if(fn.StartsWith("user_") && saveData != null)
            {
                var bytes = dc.haxe.io.Bytes.Class.alloc(saveData.Length);
                saveData.CopyTo(bytes.AsSpan());
                return bytes;
            }
            return orig(file);
        }

        private void Hook__Save_save(Hook__Save.orig_save orig, dc.User u, bool onlyGameData)
        {
            
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

            Hook__Save.save -= Hook__Save_save;
        }

        /// <summary>
        /// 载入新 level 并初始化
        /// 
        /// 清除所有的 entity
        /// </summary>
        /// <param name="saveData"></param>
        /// <returns></returns>
        public async Task EnterNewLevel(byte[] saveData)
        {
            this.saveData = saveData;

            Logger.Information("Entering new level...");
            Main.Class.ME.cleanUser();
            Main.Class.ME.launchGame(new LaunchMode.LoadSave(), null, null);

            while (true)
            {
                await Task.Delay(1);
                if (dc.pr.Game.Class.ME?.curLevel != null)
                {
                    break;
                }
            }

            Logger.Information("Clearing entities...");

            var gm = dc.pr.Game.Class.ME;
            var level = gm.curLevel;

            List<Entity> entities = [];
            foreach(Entity v in level.entities)
            {
                if(v is Hero ||
                    v is ZDoor)
                {
                    continue;
                }
                entities.Add(v);
            }
            foreach (var v in entities)
            {
                v.destroy();
            }
        }
    }
}
