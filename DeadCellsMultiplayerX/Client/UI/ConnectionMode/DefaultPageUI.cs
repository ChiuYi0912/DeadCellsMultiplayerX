using dc;
using dc.ui;
using HaxeProxy.Runtime;
using ModCore.Utilities;

namespace DeadCellsMultiplayerX.Client.UI.Modes
{
    internal class DefaultMode : ModeConfig
    {
        public DefaultMode(LobbyMenu manager) : base(manager, "默认联机") { }

        public override void BuildContent(FlowBox right, int panelW)
        {
            Manager.LoadImageTorightFlow("DeadCellsMultiplayerX/Image/lobbyTile.png");
        }

        public async override void OnHost(Action onend)
        {
            Manager.LoaddingIn("正在创建房间...", async () =>
            {
                await ClientMain.Instance.StartHost("127.0.0.1", 12345);
                ClientMain.Instance.CurrentGuestClient!.SetReady(true);
            });
            Manager.delayer.addMs(null, () => { Manager.LoaddingOut(); onend(); }, 5 * 1000);

        }
        public async override void OnClient(Action<bool> canEnter)
        {
            var input = new TextInput(
                Manager,
                "输入ip及端口".AsHaxeString(),
                "test".AsHaxeString(),
                "".AsHaxeString(),
                (str) =>
                {
                    canEnter.Invoke(true);
                    Test.StartClient();
                }
                , null, null, null
            );
        }


        public override void OnHostLeave()
        {
            Manager.client.CurrentHostClient?.Dispose();
        }

        public override void OnClientLeave()
        {
            Manager.client.CurrentGuestClient?.Quit();
        }

        public override void Update()
        {

        }

    }
}
