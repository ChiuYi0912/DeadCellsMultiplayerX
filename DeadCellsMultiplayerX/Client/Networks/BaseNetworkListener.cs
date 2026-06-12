using DeadCellsMultiplayerX.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeadCellsMultiplayerX.Client.Networks
{
    internal abstract class BaseNetworkListener : Disposable
    {
        public abstract Task Init();
        public abstract Task<BaseNetworkConnection> WaitConnect(CancellationToken cancellationToken);
    }
}
