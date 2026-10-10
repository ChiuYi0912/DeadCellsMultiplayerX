using dc.en;
using DeadCellsMultiplayerX.Common;
using DeadCellsMultiplayerX.Common.Data;
using DeadCellsMultiplayerX.Common.Data.Snapshot;
using DeadCellsMultiplayerX.Server.Connection;
using DeadCellsMultiplayerX.Server.WorldX;
using DeadCellsMultiplayerX.Server.WorldX.Hubs;
using DeadCellsMultiplayerX.Utils;
using Microsoft.VisualStudio.Threading;
using ModCore.Events.Interfaces.Game;
using Nerdbank.Streams;
using System.Diagnostics;
using System.IO.Pipes;

namespace DeadCellsMultiplayerX.Server
{
    internal class ServerSession : DisposableEventReceiver,
        IOnFrameUpdate
    {
        public AnonymousPipeClientStream outPipe = new(PipeDirection.Out, Environment.GetEnvironmentVariable("DCMP_HOST_IN_PIPE")!);
        public AnonymousPipeClientStream inPipe = new(PipeDirection.In, Environment.GetEnvironmentVariable("DCMP_HOST_OUT_PIPE")!);

        private MultiplexingStream? multiplexingStream;
        private MultiplexingStream.Channel? mainChannel;
        private Stream? mainChannelReader;
        private ServerMainThread? mainThread;
        private HeroSnapshotHub heroHub = new();
        private readonly ServerTimeSystem timeSystem = new();

        private readonly List<SGuestConnection> guests = [];
        public readonly Dictionary<string, GuestGameInfo> GuestsGameInfo = [];

        public long CurrentTimeStamp => timeSystem.Now;

        // 发送节流（20Hz）
        private double sendAccumulator;
        private const double SendInterval = 0.05;

        public ServerMainThread Main => mainThread ?? throw new InvalidOperationException();

        public async Task Init()
        {
            await Task.Yield().ConfigureAwait(false);

            mainThread = new(this);
            timeSystem.Start();
            outPipe.WriteByte(0x32);

            Logger.Information("Waiting host...");
            multiplexingStream = await MultiplexingStream.CreateAsync(
                FullDuplexStream.Splice(inPipe, outPipe),
                new MultiplexingStream.Options()
                {
                    ProtocolMajorVersion = 3
                }
                );

            await Task.Delay(10);

            Logger.Information("Binding main channel...");

            mainChannel = await multiplexingStream.AcceptChannelAsync("main");
            mainChannelReader = mainChannel.AsStream();

            Logger.Information("Waiting guests...");
            await WaitGuestConnect();
        }

        /// <summary>
        /// 等待 Guest 连接
        /// </summary>
        /// <returns></returns>
        private async Task WaitGuestConnect()
        {
            await Task.Yield().ConfigureAwait(false);
            Debug.Assert(mainChannelReader != null);
            Debug.Assert(multiplexingStream != null);

            byte[] numBuffer = new byte[4];
            while (true)
            {
                await mainChannelReader.ReadAtLeastAsync(numBuffer, 4, throwOnEndOfStream: true);

                var channelId = BitConverter.ToInt32(numBuffer);
                if (channelId == -1)
                {
                    break; // 加载完成
                }

                Logger.Information("Connecting from channel {id}", channelId);

                var channel = multiplexingStream.AcceptChannel(channelId);
                var stream = channel.AsStream();

                string guestGuid = await StreamUtils.ReadGuestGuidAsync(stream);

                var sguest = new SGuestConnection(this, stream);
                sguest.GuestInfo.Guid = guestGuid;

                guests.Add(sguest);

                var gameInfo = new GuestGameInfo
                {
                    PlyerID = guestGuid,
                };
                GuestsGameInfo.Add(guestGuid, gameInfo);

                Logger.Information("Client Enter : {guid}", guestGuid);
            }
        }


        /// <summary>
        /// 通知所有客户端切换关卡
        /// </summary>
        /// <param name="levelid"></param>
        /// <returns></returns>
        public Task NoticeGuestsEnterNextLevel(string levelid)
        {
            foreach (var sGuest in guests)
            {
                sGuest.guest.EnterNextLevel(levelid);
            }

            foreach (var guest in GuestsGameInfo)
            {
                guest.Value.LevelSelected = false;
            }

            return Task.CompletedTask;
        }

        public Task BroadcastGuestsSyncRemoteHero(HeroUpload upload)
        {
            heroHub.Upload(upload);
            return Task.CompletedTask;
        }

        void IOnFrameUpdate.OnFrameUpdate(double dt)
        {
            timeSystem.Tick();

            TickHeroSync(dt);
        }

        /// <summary>
        /// 玩家同步，20Hz 固定发送
        /// </summary>
        /// <param name="dt"></param>
        private void TickHeroSync(double dt)
        {
            sendAccumulator += dt;
            if (sendAccumulator < SendInterval) return;
            sendAccumulator -= SendInterval;

            var snap = heroHub.Flush(CurrentTimeStamp);
            if (snap == null) return;

            foreach (var guest in guests)
                guest.SyncSnapshot(snap);
        }
    }
}
