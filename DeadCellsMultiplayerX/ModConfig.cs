using ModCore.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeadCellsMultiplayerX
{
    internal class ModConfig
    {
        public static Config<ModConfig> Config { get; } = new("DeadCellsMultiplayerX");

        /// <summary>
        /// 开始游戏等待的最大时间
        /// </summary>
        public double StartTimeout { get; set; } = 5; 
    }
}
