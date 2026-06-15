using dc;
using ModCore.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeadCellsMultiplayerX.Server.Events
{
    [Event]
    internal interface IOnEntityDisposed
    {
        public void OnEntityDisposed(Entity e);
    }
}
