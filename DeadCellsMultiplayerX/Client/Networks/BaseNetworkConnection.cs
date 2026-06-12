using DeadCellsMultiplayerX.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeadCellsMultiplayerX.Client.Networks
{
    internal abstract class BaseNetworkConnection : Disposable
    {
        public abstract Stream Stream { get; }
        public abstract Task Init(CancellationToken cancellationToken);
    }
}
