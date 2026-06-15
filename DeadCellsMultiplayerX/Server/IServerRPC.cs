using DeadCellsMultiplayerX.Client;
using DeadCellsMultiplayerX.Common.Data;
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
        public class AreaInfoRequest
        {
            public RectInt Rect { get; set; } = new();
            public int SubLevelId { get; set; }
        }
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
        public Task UploadSavedata(byte[] data);

        /// <summary>
        /// Guest 下载存档文件
        /// </summary>
        /// <returns></returns>
        public Task<byte[]> DownloadSavedata();

        /// <summary>
        /// 请求一个区域信息
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        public Task<AreaInfo> RequestAreaInfo(AreaInfoRequest request);

        /// <summary>
        /// 请求一个 Entity 信息
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public Task<EntityInfo?> RequestEntityInfo(string guid);

        /// <summary>
        /// 获取服务器时间
        /// </summary>
        /// <returns></returns>
        public Task<long> GetTimeStamp();

        public Task<bool> CheckVersion(string version);
    }
}
