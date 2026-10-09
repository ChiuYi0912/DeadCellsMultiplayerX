using System.Collections.Generic;
using MessagePack;

namespace DeadCellsMultiplayerX.Common.Data.Snapshot
{
    /// <summary>
    /// 客户端每帧上传给服务端的统一包
    /// </summary>
    [MessagePackObject]
    public class HeroUpload
    {
        [Key(0)] public long ClientTime;    //客户端本地时间

        [Key(1)] public uint FrameId;   //客户端帧号

        [Key(2)] public HeroSpawn? Spawn;   //静态数据

        [Key(3)] public HeroDynamic Dynamic = null!;    //动态数据
    }
}