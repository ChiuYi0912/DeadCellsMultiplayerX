using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MessagePack;

namespace DeadCellsMultiplayerX.Common.Data.Snapshot
{
    [MessagePackObject]
    public class WorldSnapshot
    {
        [Key(0)] public double ServerTime;  //服务端时间戳
        [Key(1)] public List<HeroSpawn> Spawned = [];    // 新玩家
        [Key(2)] public List<HeroDynamic> Updated = [];  // 有变化的玩家
        [Key(3)] public List<string> Despawned = [];  // 离开的玩家
    }
}