using DeadCellsMultiplayerX.Client.Guest;
using DeadCellsMultiplayerX.Client.Host;
using DeadCellsMultiplayerX.Client.Networks.Quic;
using DeadCellsMultiplayerX.Server;
using ModCore;
using ModCore.Mods;
using ModCore.Utilities;

namespace DeadCellsMultiplayerX.Client
{
    internal class ClientMain : Module<ClientMain>
    {
        /// <summary>
        /// 当前的 Guest Client 实例
        /// </summary>
        public GuestClient? CurrentGuestClient { get; internal set; }

        /// <summary>
        /// 当前的 Host Client 实例
        /// </summary>

        public HostClient? CurrentHostClient { get; internal set;  }

        //初始化客户端
        public void Init()
        {
            
        }

        /// <summary>
        /// 清理 Client 实例
        /// </summary>
        public void CleanupClient()
        {
            Logger.Information("Cleaning clients...");

            CurrentGuestClient?.Dispose();
            CurrentHostClient?.Dispose();
            CurrentHostClient = null;
            CurrentGuestClient = null;

            GC.Collect(2, GCCollectionMode.Forced);

            Task.Delay(200).ContinueWith(_ => GC.Collect(2, GCCollectionMode.Forced));
        }

        /// <summary>
        /// 启动 Host 客户端，并加入房间
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        public async Task StartHost(string ip, int port)
        {
            CleanupClient();

            var listener = new QuicNetworkListener(ip, port);

            await listener.Init();

            CurrentHostClient = new HostClient(listener, default);
            await CurrentHostClient.Init();

            await StartGuest(ip, port);
        }

        /// <summary>
        /// 启动 Guest 客户端
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public async Task StartGuest(string ip, int port, string? playerName = null)
        {
            if(string.IsNullOrEmpty(playerName))
            {
                playerName = Environment.UserName;
            }

            if(CurrentGuestClient != null && !CurrentGuestClient.IsDisposed)
            {
                throw new InvalidOperationException();
            }

            var connect = await QuicNetworkConnect.Connect(ip, port, default);

            CurrentGuestClient = new GuestClient(connect);
            await CurrentGuestClient.Init(playerName);
        }
    }
}
