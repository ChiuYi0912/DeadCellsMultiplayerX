namespace DeadCellsMultiplayerX.Client.UI.ConnectionMode
{
    internal class DefaultPageUI : BasePageUI
    {
        public DefaultPageUI(LobbyMenu manager) : base(manager, "默认联机")
        {
            //添加其他按键.使用manager.BuildLeftBtn
            addmenu = new(() =>
            {
                //manager.
            });
        }

        public override void AfterBuildClient()
        {
            
        }

        public override void AfterBuildHost()
        {
           
        }

        public override void BeforeBuildClient()
        {
            
        }

        public override void BeforeBuildHost()
        {
            
        }


        public override void update() { }
    }
}
