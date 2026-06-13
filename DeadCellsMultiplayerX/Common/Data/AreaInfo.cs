using System;
using System.Collections.Generic;
using System.Text;

namespace DeadCellsMultiplayerX.Common.Data
{
    public class AreaInfo
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int[]? Collision { get; set; }
        public List<EntityInfo> Entities { get; set; } = [];

    }
}
