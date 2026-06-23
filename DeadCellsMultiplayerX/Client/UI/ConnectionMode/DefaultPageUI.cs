using dc.ui;

namespace DeadCellsMultiplayerX.Client.UI.Modes
{
    internal class DefaultMode : ModeConfig
    {
        public DefaultMode(LobbyMenu manager) : base(manager, "默认联机") { }

        public override void BuildContent(FlowBox right, int panelW)
        {
            Manager.LoadImageTorightFlow("DeadCellsMultiplayerX/Image/lobbyTile.png");
        }

        public override void OnHost()
        {
            Test.Start();
            logger.Information("正在创建房间");
            
        }
        public override void OnClient() { }
        public override void Update()   { }
    }
}
