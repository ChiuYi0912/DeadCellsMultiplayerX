using System;
using System.Collections.Generic;
using System.Text;

namespace DeadCellsMultiplayerX.Common.Data
{
    public class AreaInfo
    {
        public RectInt Rect { get; set; } = new();
        public int[]? Collision { get; set; }
        public List<EntityInfo> Entities { get; set; } = [];
    }
}
