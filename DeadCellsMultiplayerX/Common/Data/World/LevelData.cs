using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeadCellsMultiplayerX.Common.Data.World
{
    /// <summary>
    /// guest首次进入关卡时从server获取,关卡基本信息
    /// </summary>
    public class LevelData
    {
        public record Level();
        public record Map(string id, int seed, int mobDmgTier, int mobLifeTier);
    }
}