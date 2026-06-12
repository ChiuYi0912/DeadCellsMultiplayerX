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
        public ILogger Logger { get; }


        public ClientBase()
        {
            Logger = Log.Logger.ForContext(GetType());
        }

        protected override void MyDispose()
        {
            Logger.Information("Disposing client...");
        }
    }
}
