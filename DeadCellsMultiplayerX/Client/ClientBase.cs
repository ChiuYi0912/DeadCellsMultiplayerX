using DeadCellsMultiplayerX.Common;
using ModCore.Events;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeadCellsMultiplayerX.Client
{
    public abstract class ClientBase : DisposableEventReceiver
    {

        protected override void MyDispose()
        {
            Logger.Information("Disposing client...");
        }
    }
}
