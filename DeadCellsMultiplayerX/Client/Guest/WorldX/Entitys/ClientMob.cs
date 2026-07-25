using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using dc.en;
using dc.libs.heaps.slib._AnimManager;
using DeadCellsMultiplayerX.Common;
using DeadCellsMultiplayerX.Common.Data;
using DeadCellsMultiplayerX.Common.Serializers;
using HaxeProxy.Runtime;
using Mirror;
using ModCore.Utilities;

namespace DeadCellsMultiplayerX.Client.Guest.WorldX.Entitys
{
    public class ClientMob : DisposableEventReceiver
    {
        public readonly Mob mob;
        public readonly string GUID;

        private dc.libs.Process? interpolationProcess; //挂载位置渲染程序

        private string lastGroup = "";



        private double visualX, visualY;   // 实际渲染位置
        private bool visualInit;//是否首次渲染


        /// <summary>
        /// Mirror参数
        /// </summary>
        private SortedList<double, GhostSnapshot> snapshotBuffer = new();// 快照缓冲区，按远程时间排序，用于插值
        private const float sendInterval = 1f / 60f; // 服务器快照发送间隔（60Hz）
        private double bufferTime = 0.1; // 本地时间线落后服务器的目标缓冲时间（秒）
        private int bufferLimit = 64; // 缓冲区最大快照数量，防止内存无限增长
        private const double catchupSpeed = 0; // 追赶加速比例
        private const double slowdownSpeed = 0; // 减速比例
        private const float catchupNegativeThreshold = -0.5f;  // 触发减速的漂移负阈值（sendInterval的倍数，当前无效）
        private const float catchupPositiveThreshold = 2f;  // 触发加速的漂移正阈值（sendInterval的倍数，当前无效）
        private double localTimeline; // 本地插值时间线，始终落后服务器 latestRemoteTime - bufferTime
        private double localTimescale; // 时间缩放因子，用于追赶/减速，当前恒为1.0
        private ExponentialMovingAverage driftEma = new(10); // 漂移量（latestRemoteTime - localTimeline）的指数移动平均
        private ExponentialMovingAverage deliveryTimeEma = new(10); // 快照交付间隔的指数移动平均，备用动态缓冲调整
        private double deliveryTimeVariance = 0.0;      // 动态 bufferTime 所需
        private const double alphaStd = 0.1;            // 标准差指数平滑系数
        private const double toleranceMultiplier = 1.2;  // 安全冗余倍数
        private const double bufferSmoothFactor = 0.05;  // bufferTime 平滑系数，防止突变


        private const double TilePx = 24.0;

        public ClientMob(Mob mob, string guid)
        {
            this.mob = mob;
            this.GUID = guid;

            interpolationProcess = new dc.libs.Process(mob._level);
            interpolationProcess.onUpdateCb = new HlAction(OnInterpolationUpdate);

            

            Debug.Assert(mob != null);
            Debug.Assert(guid != null);
        }


        public void ApplyUpdate(EntityInfo incoming)
        {
            if (incoming == null) return;
            if (mob.spr == null) return;

            //mob.say("hello".AsHaxeString(), null, null, null);

            double remoteSeconds = incoming.remoteTime / 1000.0;
            double localSeconds = GuestClientSession.SyncedTimeMs / 1000.0;

            var snap = new GhostSnapshot
            {
                State = incoming,
                remoteTime = remoteSeconds,
                localTime = localSeconds
            };

            int beforeCount = snapshotBuffer.Count;

            SnapshotInterpolation.InsertAndAdjust(
                snapshotBuffer,
                bufferLimit,
                snap,
                ref localTimeline,
                ref localTimescale,
                sendInterval,
                bufferTime,
                catchupSpeed,
                slowdownSpeed,
                ref driftEma,
                catchupNegativeThreshold,
                catchupPositiveThreshold,
                ref deliveryTimeEma
            );

            // 只有在真正插入新快照且缓冲至少有两个快照时才更新统计
            if (snapshotBuffer.Count > beforeCount && snapshotBuffer.Count >= 2)
            {
                // 最近两次快照的本地到达时间差
                double prevLocal = snapshotBuffer.Values[snapshotBuffer.Count - 2].localTime;
                double latestLocal = snapshotBuffer.Values[snapshotBuffer.Count - 1].localTime;
                double interval = latestLocal - prevLocal;

                // 获取平均交付时间
                double avg = deliveryTimeEma.Value;
                if (double.IsNaN(avg) || double.IsInfinity(avg))
                    avg = sendInterval;

                // 更新指数移动方差
                double diff = interval - avg;
                deliveryTimeVariance = (1 - alphaStd) * deliveryTimeVariance + alphaStd * diff * diff;
                double stdDev = System.Math.Sqrt(System.Math.Max(0, deliveryTimeVariance));

                // 计算安全缓冲倍数（DynamicAdjustment 返回 double，调用静态方法无需空检查）
                double safeMultiplier = SnapshotInterpolation.DynamicAdjustment(
                    sendInterval, stdDev, toleranceMultiplier);

                // 至少保留 2 个快照的时间
                double targetBufferTime = System.Math.Max(sendInterval * 2, sendInterval * safeMultiplier);

                // 避免 bufferTime 突变引起插值跳跃
                bufferTime += (targetBufferTime - bufferTime) * bufferSmoothFactor;
            }
        }

        public void UpdateAnim(EntityInfo info)
        {
            var animinfo = info.animInfo;
            var anim = mob.spr.get_anim();

            if (mob.spr == null || info == null || info.MainSprite == null || animinfo == null || anim == null) return;

            var stack = anim.stack.getDyn(0) as AnimInstance;
            if (lastGroup != info.MainSprite.GroupName)
            {
                lastGroup = info.MainSprite.GroupName;
                var cur = anim.stack?.getDyn(0) as AnimInstance;
                if (cur != null) cur.plays = 0;
                anim.play(info.MainSprite.GroupName.AsHaxeString(), info.animInfo.Plays, null).loop(null);
            }

            if (stack != null)
            {
                stack.speed = info.animInfo.Speed;
                stack.paused = info.animInfo.Paused;
                stack.playDuration = info.animInfo.playDuration;
            }
        }

        void OnInterpolationUpdate()
        {
            if (snapshotBuffer.Count == 0) return;
            float deltaTime = (float)dc.hxd.Timer.Class.dt;

            // 使用 Mirror 快照插值系统，获取当前时间线对应的 from/to 快照和插值因子 t
            SnapshotInterpolation.Step(
                snapshotBuffer, deltaTime,
                ref localTimeline, localTimescale,
                out GhostSnapshot from, out GhostSnapshot to, out double t);

            var Posfrom = DCMXSerializers.MessagePack.Deserialize<PosVector>(from.State.PosVector);
            var Posto = DCMXSerializers.MessagePack.Deserialize<PosVector>(to.State.PosVector);

            if (Posfrom == null || Posto == null) return;

            // 将格子坐标和归一化偏移转换为全局像素坐标，避免跨格子插值失真
            double fromPx = Posfrom.CX * TilePx + Posfrom.XR * TilePx;
            double fromPy = Posfrom.CY * TilePx + Posfrom.XY * TilePx;
            double toPx = Posto.CX * TilePx + Posto.XR * TilePx;
            double toPy = Posto.CY * TilePx + Posto.XY * TilePx;

            // 在像素空间线性插值，得到当前帧的目标位置
            double targetX = fromPx + (toPx - fromPx) * t;
            double targetY = fromPy + (toPy - fromPy) * t;

            mob.dir = Posto.DIR;

            //传送检测
            if (!visualInit ||
                System.Math.Sqrt((targetX - visualX) * (targetX - visualX) + (targetY - visualY) * (targetY - visualY)) > 600)
            {
                visualX = targetX;
                visualY = targetY;
                visualInit = true;
            }
            else
            {
                double tdx = targetX - visualX;
                double tdy = targetY - visualY;
                double distance = System.Math.Sqrt(tdx * tdx + tdy * tdy);

                // 微小抖动直接吸附，消除静止状态下的浮点/网络波动
                if (distance < 0.3)
                {
                    visualX = targetX;
                    visualY = targetY;
                }
                else
                {
                    // 指数移动平均（EMA）平滑，无追赶，保证视觉匀速且无过冲
                    const double smoothFactor = 55.0;
                    double talpha = 1.0 - System.Math.Exp(-smoothFactor * deltaTime);
                    visualX += tdx * talpha;
                    visualY += tdy * talpha;
                }
            }


            mob.setPosPixel(visualX, visualY);

            //同时更新位置避免位置与动画不同步
            UpdateAnim(from.State);
        }
    }
}