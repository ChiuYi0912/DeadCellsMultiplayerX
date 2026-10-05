using dc;
using dc.cine;
using dc.en;
using dc.pr;
using dc.tool;
using DeadCellsMultiplayerX.Client.Event;
using DeadCellsMultiplayerX.Client.Guest.WorldX;
using DeadCellsMultiplayerX.Client.Host;
using DeadCellsMultiplayerX.Common.Data;
using DeadCellsMultiplayerX.Server;
using DeadCellsMultiplayerX.Utils;
using Hashlink.Proxy.Clousre;
using ModCore;
using ModCore.Events;
using ModCore.Events.Interfaces.Game;
using ModCore.Events.Interfaces.Game.Hero;
using ModCore.Modules;
using ModCore.Utilities;
using StreamJsonRpc;
using System.Diagnostics;

namespace DeadCellsMultiplayerX.Client.Guest
{
    /// <summary>
    /// 访客的客户端 session
    /// </summary>
    internal class GuestClientSession(GuestClient client, Stream serverStream) : ClientSession,
        IGuestRPC,
        IOnFrameUpdate,
        IOnLocalHeroPostUpdate
    {
        private JsonRpc? rpc;
        private IServerRPC server = null!;
        private bool isOwner = false;
        private byte[]? saveData;
        private ClientReplicator? replicator;
        private readonly List<HashlinkHooks.HookHandle> hooks = [];

        #region Time
        private long lastSyncStopwatchTime = 0;
        private long prevStopwatchTime = 0;
        private readonly Stopwatch stopwatch = new();
        private Task? syncTimeStampTask;

        public long CurrentTimeStamp { get; private set; }
        public static long SyncedTimeMs { get; private set; }

        #endregion

        public IServerRPC Server => server ?? throw new InvalidOperationException();

        public dc.pr.Game Game => dc.pr.Game.Class.ME;
        public GuestClient Client { get; private set; } = null!;
        public RemoteHeroManager remoteHeroManager { get; private set; } = null!;
        public volatile bool serverDrivenTransition = false;

        public override async Task Init()
        {
            InitHooks();

            stopwatch.Start();

            rpc = serverStream.CreateJsonRpc();
            rpc.AddLocalRpcTarget(this);

            server = rpc.Attach<IServerRPC>();

            rpc.SynchronizationContext = ModCore.Modules.Game.SynchronizationContext;

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

            Client = client;

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


        }

        private void InitHooks()
        {
            Hook__Save.save += Hook__Save_save;
            Hook__File.getBytes += Hook__File_getBytes;

            if (GameInfo.Platform == GameInfo.PlatformKind.Steam)
            {
                HashlinkHooks.Instance.CreateHook("tool.$File", "getSteamCloudStatus", Hook__File_getSteamCloudStatus);
                HashlinkHooks.Instance.CreateHook("tool.$File", "saveSteamCloudStatus", Hook__File_saveSteamCloudStatus);
            }

            Hook_Game.onDispose += Hook_Game_onDispose;
            Hook_Mob.aiLocked += Hook_Mob_aiLocked;
        }
        private bool Hook_Mob_aiLocked(Hook_Mob.orig_aiLocked orig, Mob self)
        {
            return true;
        }

        private void Hook_Game_onDispose(Hook_Game.orig_onDispose orig, dc.pr.Game self)
        {
            orig(self);

            Logger.Information("Quiting...");

            client.Quit();
            Dispose();
        }

        private void Hook__File_saveSteamCloudStatus(HashlinkClosure orig)
        {

        }

        private bool? Hook__File_getSteamCloudStatus(HashlinkClosure orig)
        {
            return null;
        }

        private dc.haxe.io.Bytes Hook__File_getBytes(Hook__File.orig_getBytes orig, dc.String file)
        {
            var fn = file.ToString();
            if (fn.StartsWith("user"))
            {
                Debug.Assert(saveData != null);

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

            ModCore.Modules.Game.SynchronizationContext.Post(static _ =>
            {
                ClientMain.Instance.CleanupClient();

                Boot.Class.ME.returnToMainMenu();
            }, null);

            Dispose();
        }

        protected override void MyDispose()
        {
            base.MyDispose();

            serverStream?.Dispose();
            replicator?.Dispose();
            rpc?.Dispose();

            Hook__Save.save -= Hook__Save_save;
            Hook__File.getBytes -= Hook__File_getBytes;

            foreach (var v in hooks)
            {
                v.Disable();
            }
            hooks.Clear();
        }

        /// <summary>
        /// 载入新 level 并初始化
        /// </summary>
        /// <param name="saveData"></param>
        /// <returns></returns>
        public async Task EnterNewLevel(byte[] saveData)
        {
            this.saveData = saveData;

            Logger.Information("Entering new level...");

            Main.Class.ME.options.disableLoreRooms = true;

            Main.Class.ME.cleanUser();
            Main.Class.ME.launchGame(new LaunchMode.LoadSave(), null, null);

            while (true)
            {
                await Task.Delay(1);
                DisposeToken.ThrowIfCancellationRequested();

                var g = dc.pr.Game.Class.ME;
                if (g?.curLevel == null || g.subLevels == null)
                {
                    continue;
                }
                if (!Main.Class.ME.isLoading)
                {
                    break;
                }
            }

            Logger.Information("Clearing entities...");

            var gm = dc.pr.Game.Class.ME;

            foreach (Level level in gm.subLevels)
            {
                List<Entity> entities = [];
                foreach (Entity v in level.entities)
                {
                    if (v is Hero || v is Interactive)
                    {
                        continue;
                    }
                    if (v is Mob)
                    {
                        entities.Add(v);
                    }

                }
                foreach (var v in entities)
                {
                    v.destroy();
                }
            }
            // #if true
            //             Debug.Assert(client.gameSessionInfo != null);
            //             var options = new JsonSerializerOptions { WriteIndented = true };
            //             Logger.Information("sever mobs  {info}\n", JsonSerializer.Serialize(client.gameSessionInfo, options));
            // #endif


            replicator?.Dispose();
            replicator = new(this);
            replicator.Start();

            remoteHeroManager = new(this);

            Debug.Assert(rpc != null);
        }

        private void UpdateTimeStamp()
        {
            long now = stopwatch.ElapsedMilliseconds;

            if (prevStopwatchTime == 0)
            {
                prevStopwatchTime = now;
                return;
            }

            if (rpc?.IsDisposed ?? true) return;

            CurrentTimeStamp += now - prevStopwatchTime;
            prevStopwatchTime = now;

            bool needSync = now - lastSyncStopwatchTime > 5_000 || lastSyncStopwatchTime == 0;
            bool syncIdle = syncTimeStampTask == null || syncTimeStampTask.IsCompleted;

            if (needSync && syncIdle)
            {
                lastSyncStopwatchTime = now;
                syncTimeStampTask = SyncWithServer();
            }
        }

        private async Task SyncWithServer()
        {
            try
            {
                long t0 = stopwatch.ElapsedMilliseconds;
                long serverTime = await Server.GetTimeStamp();
                long rtt = stopwatch.ElapsedMilliseconds - t0;

                CurrentTimeStamp = serverTime + rtt / 2;
            }
            catch (Exception) when (IsDisposed || (rpc?.IsDisposed ?? true)) { }
            catch (Exception ex)
            {
                Logger.Error(ex, "Sync time failed");
            }
        }

        void IOnFrameUpdate.OnFrameUpdate(double dt)
        {
            // 同步 TimeStamp
            UpdateTimeStamp();
            SyncedTimeMs = CurrentTimeStamp;
        }

        public void UpdateEntity(EntityInfo info)
        {
            replicator?.ApplyEntityInfo(info, null);
        }

        public Task EnterNextLevel(string levelid)
        {
            serverDrivenTransition = true;
            try
            {
                LevelTransition.Class.@goto.Invoke(levelid.AsHaxeString());
            }
            finally
            {
                serverDrivenTransition = false;
            }
            return Task.CompletedTask;
        }

        void IOnLocalHeroPostUpdate.OnPostUpdate(Hero hero)
        {
            var game = dc.pr.Game.Class.ME;
            if (hero != game?.hero) return;
            if (hero.destroyed) return;

            EntityInfo info = HeroUtils.Collect(
                hero,
                guid: Client.Guid,
                remoteTime: CurrentTimeStamp,
                atlasResolver: lib => ClientMain.Instance.spriteLib2altas.GetValueOrDefault(lib)
            );

            server.BroadcastSyncHero(info);
        }

        public Task SyncRemoteHero(EntityInfo info)
        {
            remoteHeroManager.ApplyOrCreate(info);
            return Task.CompletedTask;
        }
    }
}
