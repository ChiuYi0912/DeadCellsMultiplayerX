using dc;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeadCellsMultiplayerX.Common.Data
{
    public class EntityInfo
    {
        public string GUID { get; set; } = "";

        public string? AtlasName { get; set; }
        public string? GroupName { get; set; }
        public int Frame { get; set; }
        public Dictionary<string, int> IntValues { get; set; } = [];
        public Dictionary<string, bool> BoolValues { get; set; } = [];
        public Dictionary<string, double> DoubleValues { get; set; } = [];
        
        
    }
}
