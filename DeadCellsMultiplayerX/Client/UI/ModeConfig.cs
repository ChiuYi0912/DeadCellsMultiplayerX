using dc;
using dc.ui;
using ModCore.Utilities;
using Serilog;

namespace DeadCellsMultiplayerX.Client.UI
{
    internal abstract class ModeConfig
    {
        public string Name { get; }

        protected LobbyMenu Manager { get; }
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
        public abstract void OnHost(Action onend);

        /// <summary>
        /// 点击"加入房间"调用
        /// </summary>
        public abstract void OnClient(Action onend);

        /// <summary>
        /// 房主离开房间
        /// </summary>
        public abstract void OnHostLeave();

        /// <summary>
        /// 玩家离开房间
        /// </summary>
        public abstract void OnClientLeave();


        public abstract void Update();

        /// <summary>
        /// 向左侧菜单添加按钮
        /// </summary>
        public virtual void BuildMenu() { }

        
    }
}
