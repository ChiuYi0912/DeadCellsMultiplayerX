using dc;
using dc.haxe;
using Mirror;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Text;

namespace DeadCellsMultiplayerX.Common.Data
{
    public class EntityInfo
    {
        public string? TypeName { get; set; } = "";
        public string GUID { get; set; } = Guid.NewGuid().ToString();
        public double remoteTime { get; set; }
        public double localTime { get; set; }


        public string? ColorMapModel { get; set; }
        public string? ColorMapSkin { get; set; }


        public int SubLevelId { get; set; }

        public byte[] PosVector = [];
        public Dictionary<int, byte[]> GlowData { get; set; } = [];
        public SpriteInfo? MainSprite { get; set; }
        public AnimInfo animInfo { get; set; } = new();

    }
}
