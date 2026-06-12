using Microsoft;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeadCellsMultiplayerX.Common
{
    public abstract class Disposable : IDisposable, IDisposableObservable
    {
        private readonly CancellationTokenSource disposedSource = new();

        public CancellationToken DisposeToken => disposedSource.Token;
        public bool IsDisposed => disposedSource.IsCancellationRequested;

        protected abstract void MyDispose();

        public void Dispose()
        {
            if(IsDisposed)
            {
                return;
            }

            GC.SuppressFinalize(this);
            disposedSource.Cancel();

            MyDispose();
        }

        ~Disposable()
        {
            Dispose();
        }
    }
}
