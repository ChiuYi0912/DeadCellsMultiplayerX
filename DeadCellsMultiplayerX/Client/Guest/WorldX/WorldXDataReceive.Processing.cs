using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dc.en;
using dc.pr;
using DeadCellsMultiplayerX.Common.Data;

namespace DeadCellsMultiplayerX.Client.Guest.WorldX
{
    internal partial class WorldXDataReceive
    {

        public async Task CretaGuestMobs(Level level, Action<EntityInfo, Level, Mob> addghostToClient)
        {
            var mobs = await GetServerMobs();

            foreach (var data in mobs)
            {
                var info = new EntityInfo();
                info.GUID = data.Key;
                var mob = level.attachMob(data.Value);

                addghostToClient(info, level, mob);
            }
        }

    }
}