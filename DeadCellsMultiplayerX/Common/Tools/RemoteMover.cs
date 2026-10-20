using System;
using dc;
using DeadCellsMultiplayerX.Common.Data;
using DeadCellsMultiplayerX.Common.Math;
using DeadCellsSync.Core.Snapshot;

namespace DeadCellsMultiplayerX.Common.Tools
{
    public sealed class RemoteMover<TEntity, TSnapshot>
        where TEntity : Entity
        where TSnapshot : class
    {
        /// <summary>
        /// 快照插值延迟时间（秒），用于缓冲远程实体的位置快照，
        /// 以便在相邻快照之间进行平滑插值。
        /// </summary>
        public double InterpolationDelaySeconds { get; }

        /// <summary>
        /// 传送距离阈值。实体当前位置与目标位置的距离超过该阈值时，
        /// 直接设置位置，避免插值造成长距离拖动。
        /// </summary>
        public double TeleportDistance { get; }

        /// <summary>
        /// 位置校正强度，控制实体向目标位置进行误差校正的速度。
        /// 值越大，位置误差收敛越快。
        /// </summary>
        public double PositionCorrectionStrength { get; }

        /// <summary>
        /// 速度响应系数，控制实体当前速度向目标速度调整的响应程度。
        /// 值越大，速度变化的响应越快。
        /// </summary>
        public double VelocityResponse { get; }

        private readonly SnapshotBuffer<TSnapshot> buffer;
        private readonly Func<TSnapshot, PosVector> posOf;

        private bool hasData;
        private double lastSnapX, lastSnapY;

        public bool HasData => hasData;
        public bool JustTeleported { get; private set; }

        public RemoteMover(
            Func<TSnapshot, PosVector> posOf,
            double interpolationDelaySeconds,
            double teleportDistance,
            double positionCorrectionStrength,
            double velocityResponse)
        {
            this.posOf = posOf ?? throw new ArgumentNullException(nameof(posOf));
            InterpolationDelaySeconds = interpolationDelaySeconds;
            TeleportDistance = teleportDistance;
            PositionCorrectionStrength = positionCorrectionStrength;
            VelocityResponse = velocityResponse;
            this.buffer = new SnapshotBuffer<TSnapshot>(interpolationDelaySeconds);
        }

        public void Push(TSnapshot snapshot, double serverTimeSec)
        {
            if (snapshot == null) return;

            var pos = posOf(snapshot);
            if (pos == null) return;

            double nx = pos.CX + pos.XR;
            double ny = pos.CY + pos.XY;
            double teleportSq = TeleportDistance * TeleportDistance;

            if (hasData)
            {
                double dx = nx - lastSnapX;
                double dy = ny - lastSnapY;
                if (dx * dx + dy * dy > teleportSq)
                    buffer.Clear();
            }

            lastSnapX = nx;
            lastSnapY = ny;
            buffer.AddSnapshot(serverTimeSec, snapshot);
            hasData = true;
        }

        public bool Tick(double nowSec, TEntity target,
                         out TSnapshot prev, out TSnapshot next, out double renderTimeSec)
        {
            prev = null!;
            next = null!;
            renderTimeSec = 0;
            JustTeleported = false;

            if (!hasData) return false;
            if (!buffer.TryGetInterpolation(nowSec, out prev, out next, out double alpha)) return false;

            var fromPos = posOf(prev);
            var toPos = posOf(next);
            if (fromPos == null || toPos == null) return false;

            // 插值
            double snapX = MathX.Lerp(fromPos.CX + fromPos.XR, toPos.CX + toPos.XR, alpha);
            double snapY = MathX.Lerp(fromPos.CY + fromPos.XY, toPos.CY + toPos.XY, alpha);
            double netDx = MathX.Lerp(fromPos.DX, toPos.DX, alpha);
            double netDy = MathX.Lerp(fromPos.DY, toPos.DY, alpha);
            double netBdx = MathX.Lerp(fromPos.BDX, toPos.BDX, alpha);
            double netBdy = MathX.Lerp(fromPos.BDY, toPos.BDY, alpha);
            int targetDir = alpha < 0.5 ? fromPos.DIR : toPos.DIR;

            renderTimeSec = nowSec - InterpolationDelaySeconds;

            double actualX = target.cx + target.xr;
            double actualY = target.cy + target.yr;
            double errX = snapX - actualX;
            double errY = snapY - actualY;
            double teleportSq = TeleportDistance * TeleportDistance;

            // 瞬移
            if (errX * errX + errY * errY > teleportSq)
            {
                int scx = (int)System.Math.Floor(snapX);
                int scy = (int)System.Math.Floor(snapY);
                target.setPosCase(scx, scy, snapX - scx, snapY - scy);

                target.dx = netDx;
                target.dy = netDy;
                target.bdx = 0;
                target.bdy = 0;
                if (target.dir != toPos.DIR) target.dir = toPos.DIR;

                buffer.Clear();
                JustTeleported = true;
                return true;
            }

            // 朝向
            if (target.dir != targetDir) target.dir = targetDir;

            //修正
            double targetDx = netDx + errX * PositionCorrectionStrength;
            double targetDy = netDy + errY * PositionCorrectionStrength;
            target.dx += (targetDx - target.dx) * VelocityResponse;
            target.dy += (targetDy - target.dy) * VelocityResponse;
            target.bdx = netBdx;
            target.bdy = netBdy;

            return true;
        }

        public void Clear()
        {
            buffer.Clear();
            hasData = false;
            lastSnapX = lastSnapY = 0;
            JustTeleported = false;
        }
    }
}