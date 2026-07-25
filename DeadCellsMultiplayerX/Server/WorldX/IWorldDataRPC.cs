using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PolyType;
using StreamJsonRpc;

namespace DeadCellsMultiplayerX.Server.WorldX
{
    [JsonRpcContract, GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
    internal partial interface IWorldDataRPC
    {
        public Task<string> Test();
    }
}

