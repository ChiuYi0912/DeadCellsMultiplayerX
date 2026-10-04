using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dc.en;
using ModCore.Events;

namespace DeadCellsMultiplayerX.Client.Event
{
    [Event]
    public interface IOnLocalHeroPostUpdate
    {
        void OnPostUpdate(Hero hero);
    }
}