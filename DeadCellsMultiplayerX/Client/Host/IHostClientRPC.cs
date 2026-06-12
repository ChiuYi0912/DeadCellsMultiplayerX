using PolyType;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeadCellsMultiplayerX.Client.Host
{
    [JsonRpcContract, GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
    internal partial interface IHostClientRPC
    {
        public Task<bool> CheckVersion(string version);
        /// <summary>
        /// 获取房间信息
        /// </summary>
        /// <returns></returns>
        public Task<LobbyInfo> GetLobbyInfo();

        /// <summary>
        /// 获取当前玩家的 GUID
        /// </summary>
        /// <returns></returns>
        public Task<string> GetGUID();

        /// <summary>
        /// 设置访客名称
        /// </summary>
        /// <param name="name"></param>
        public void SetName(string name);

        /// <summary>
        /// 退出房间
        /// </summary>
        public void Quit();

        /// <summary>
        /// 设置是否准备好开始游戏
        /// </summary>
        /// <param name="ready"></param>
        public void SetReady(bool ready);

        public Task<Stream> GetServerStream();
    }
}
