
namespace DeadCellsMultiplayerX.Client.UI
{
    internal class DefaultPageUI : BasePageUI
    {
        public DefaultPageUI(LobbyMenu manager) : base(manager, "默认联机") { }

        /// <summary>
        /// 房主页面
        /// </summary>
        public override void BuildRightHost()
        {
            //test
            Title("创建房间");
            Spacer();
            Row("Room: 127.0.0.1:44567", 0xFFD700);
            Row("等待玩家...", 0xAAAAAA);
            Spacer();
            var btns = ButtonRow();
            Button(btns, "开始游戏", () => {  });
            Button(btns, "退出房间", () => Manager.Hide());
            Done();
        }

        /// <summary>
        /// 访客页面
        /// </summary>
        public override void BuildRightClient()
        {
            //test
            Title("加入房间");
            Spacer();
            Row("玩家:列表", 0xDDDDDD);
            Spacer();
            var btns = ButtonRow();
            Button(btns, "退出房间", () => Manager.Hide());
            Done();
        }

        /// <summary>
        /// 用于构建左选项
        /// </summary>
        public override void BuildLeftMenuChild()
        {
            AddMenuChild("创建房间",new(()=>Manager.ShowHost(this)));
            AddMenuChild("加入房间", new(() => Manager.ShowClient(this)));
            AddMenuChild("返回", new(() => {Manager.titleScreen.clearMenu();Menu();}));
        }

        /// <summary>
        /// 每帧更新
        /// </summary>
        public override void update()
        {
            
        }
    }
}
