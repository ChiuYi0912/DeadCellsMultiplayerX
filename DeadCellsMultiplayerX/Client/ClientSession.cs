using DeadCellsMultiplayerX.Common;
using ModCore.Events;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeadCellsMultiplayerX.Client
{
    /// <summary>
    /// 表示一个 session (一局游戏)
    /// 
    /// 应该在游戏开始时初始化，不允许新玩家加入已开始的游戏
    /// </summary>
    public abstract class ClientSession : DisposableEventReceiver
    {

        public static ILogger Logger => ClientMain.Logger;

        public virtual async Task Init()
        {

        }
    }
}
