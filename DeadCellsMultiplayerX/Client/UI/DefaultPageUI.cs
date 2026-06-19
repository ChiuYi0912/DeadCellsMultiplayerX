namespace DeadCellsMultiplayerX.Client.UI
{
    internal class DefaultPageUI : BasePageUI
    {
        public DefaultPageUI(LobbyMenu manager) : base(manager, "默认联机")
        {
            BuildHost   = () => { };
            BuildClient = () => { };
        }

        public override void BuildContent()
        {
            



            // Title("房间");
            // Spacer();

            // var sprflowmain = Row(null);
            // sprflowmain.set_isVertical(false);
            // for (int i = 0; i < 4; i++)
            // {
            //     var sprflow = PlayerVacancies(sprflowmain);
            //     SprVacancies.Add(sprflow);
            // }

            // foreach (var item in SprVacancies)
            // {
            //     var spr =AddHeroSpr(item);
            // }

            // Spacer();
            // var btns = ButtonRow();
            // Button(btns, "退出房间", () => Manager.Hide());
            // Done();
        }

        public override void BuildLeftMenuChild()
        {
            // AddMenuChild("创建房间", new(() => { }));
            // AddMenuChild("加入房间", new(() => { }));
            // AddMenuChild("返回",     new(() => { Manager.Hide(); Manager.titleScreen.clearMenu(); Menu(); }));
        }

        public override void update() { }
    }
}
