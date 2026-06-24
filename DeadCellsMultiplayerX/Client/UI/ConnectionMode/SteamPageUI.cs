using dc.ui;

namespace DeadCellsMultiplayerX.Client.UI.Modes
{
    internal class SteamMode : ModeConfig
    {
        public SteamMode(LobbyMenu manager) : base(manager, "SteamP2P") { }

        public override void BuildContent(FlowBox right, int panelW)
        {
            Manager.LoadImageTorightFlow("DeadCellsMultiplayerX/Image/lobbyTile_2.png");
        }

        public override void OnHost(Action onend) { }
        public override void OnClient(Action<bool> canEnter) { }
        public override void Update() { }
        public override void OnHostLeave() { }
        public override void OnClientLeave() { }
    }
}
