using ModCore.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeadCellsMultiplayerX.Common
{
    public abstract class DisposableEventReceiver : Disposable, IEventReceiver
    {
        public DisposableEventReceiver()
        {
            EventSystem.AddReceiver(this);
        }

        protected override void MyDispose()
        {
            EventSystem.RemoveReceiver(this);
        }
    }
}
