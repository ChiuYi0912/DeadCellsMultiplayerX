using DeadCellsMultiplayerX.Common.Data;
using DeadCellsMultiplayerX.Common.Data.Snapshot;
using PolyType;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeadCellsMultiplayerX.Client.Guest
{
    [JsonRpcContract, GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
    internal partial interface IGuestRPC
    {
        /// <summary>
        /// 告诉客户端进入新level
        /// </summary>
        /// <param name="saveData"></param>
        /// <returns></returns>
        public Task EnterNewLevel(byte[] saveData);

        /// <summary>
        /// 告诉客户端切换关卡
        /// </summary>
        /// <param name="levelid"></param>
        /// <returns></returns>
        public Task EnterNextLevel(string levelid);


        /// <summary>服务端每帧推送世界快照</summary>
        public Task SyncSnapshot(WorldSnapshot snapshot);
    }
}
