using DeadCellsMultiplayerX.Client;
using PolyType;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeadCellsMultiplayerX.Server
{
    [JsonRpcContract, GenerateShape(IncludeMethods = MethodShapeFlags.PublicInstance)]
    internal partial interface IServerRPC
    {
        /// <summary>
        /// 设置玩家信息
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public Task SetGuestInfo(GuestInfo info);

        /// <summary>
        /// Host 上传存档文件
        /// </summary>
        /// <param name="savedata"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public Task UploadSavedata(Stream savedata, int length);

        /// <summary>
        /// Guest 下载存档文件
        /// </summary>
        /// <returns></returns>
        public Task<(Stream, int)> DownloadSavedata();

        public Task<bool> CheckVersion(string version);
    }
}
