using dc.ui;
using Serilog;

namespace DeadCellsMultiplayerX.Client.UI
{
    internal abstract class ModeConfig
    {
        protected LobbyMenu Manager { get; }
        public string Name { get; }
        public readonly ILogger logger;

        protected ModeConfig(LobbyMenu manager, string menuName)
        {
            Manager = manager;
            Name = menuName;
            logger =Log.ForContext(GetType());
        }
        
        public abstract void BuildContent(FlowBox right, int panelW);

        /// <summary>
        /// 点击"创建房间"调用
        /// </summary>
        public abstract void OnHost();

        /// <summary>
        /// 点击"加入房间"调用
        /// </summary>
        public abstract void OnClient();

        public abstract void Update();

        /// <summary>
        /// 向左侧菜单添加按钮
        /// </summary>
        public virtual void BuildMenu() { }
    }
}
