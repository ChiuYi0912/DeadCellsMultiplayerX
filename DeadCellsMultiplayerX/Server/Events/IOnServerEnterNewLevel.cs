using ModCore.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeadCellsMultiplayerX.Server.Events
{
    [Event]
    internal interface IOnServerEnterNewLevel
    {
        public void OnServerEnterNewLevel();
    }
}
