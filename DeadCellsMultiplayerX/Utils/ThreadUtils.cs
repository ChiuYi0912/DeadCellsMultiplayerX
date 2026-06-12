using Hashlink;
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace DeadCellsMultiplayerX.Utils
{
    internal static class ThreadUtils
    {
        public static CancellationToken Combine(this CancellationToken cancellationToken, CancellationToken another)
        {
            if(!cancellationToken.CanBeCanceled && another.CanBeCanceled)
            {
                return another;
            }
            if(cancellationToken.CanBeCanceled && !another.CanBeCanceled)
            {
                return cancellationToken;
            }
            if(!cancellationToken.CanBeCanceled && !another.CanBeCanceled)
            {
                return default;
            }
            var cts = cancellationToken.CombineWith(another);
            return cts.Token;
        }
    }
}
