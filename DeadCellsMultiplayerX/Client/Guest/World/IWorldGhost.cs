using System;
using System.Collections.Generic;
using System.Text;

namespace DeadCellsMultiplayerX.Client.Guest.World
{
    internal interface IWorldGhost : IDisposable
    {
        public string GUID { get; }
        public void SetVisible(bool visible);
    }
}
