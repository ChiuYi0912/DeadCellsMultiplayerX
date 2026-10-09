using System;
using DeadCellsMultiplayerX.Common.Serializers;
using Mirror;

namespace DeadCellsMultiplayerX.Common.Data
{
    public class GhostSnapshot : Mirror.Snapshot
    {
        public EntityInfo State = null!;

        public double remoteTime { get; set; }
        public double localTime { get; set; }
    }
}