using CoreLibrary.Core.Extensions;
using dc;
using dc.en;
using dc.hl.types;
using dc.libs.heaps.slib._AnimManager;
using dc.pr;
using DeadCellsMultiplayerX.Common.Data;
using DeadCellsMultiplayerX.Common.Serializers;
using DeadCellsMultiplayerX.Utils;
using DeadCellsSync.Core.Snapshot;
using ModCore.Utilities;

namespace DeadCellsMultiplayerX.Client.Guest.WorldX.Entitys
{
    public class RemoteHero : KingSkin
    {
        // 常量
        private const double InterpolationDelaySeconds = 0.1;// 渲染延迟100ms，保证总有两个快照可插值，平滑网络状态
        private const double TeleportDistSq = 25.0 * 25.0;// 与快照距离平方超过25格时直接瞬移，避免远距离拉扯
        private const double PositionCorrectionStrength = 2.0;// 位置误差拉动目标速度的强度，越大追得越快，越小越平滑
        private const double VelocityResponse = 0.25;// 每物理帧向目标速度靠拢的比例，越大响应越快，越小越平滑

        // 状态
        private readonly SnapshotBuffer<EntityInfo> snapshotBuffer = new(InterpolationDelaySeconds);
        private bool hasData;
        private double lastSnapX, lastSnapY;
        private string lastGroup = "";
        private long lastAppliedStartTime = -1;
        private bool inRun;

        // 插值结果
        private struct InterpolatedSnapshot
        {
            public double SnapX;
            public double SnapY;
            public double NetDx;
            public double NetDy;
            public double NetBdx;
            public double NetBdy;
            public int TargetDir;
        }


        public RemoteHero(Level lvl, int x, int y) : base(lvl, x, y)
        {
            set_easeSpritePos(false);
            hasWineGlass = false;
        }

        public override void init()
        {
            base.init();
            hasRepelling = false;
            hasGravity = false;
            gravity = 0;
            collisionMode = new CollisionMode.None();
        }

        public override void initGfx()
        {
            base.initGfx();

            var anim = spr.get_anim();
            anim.registerStateAnim(
               "run".AsHaxeString(),
               2,
               null,
               new(RunCondition),
               default,
               null);
            anim.removeAllStateAnims();

            createConfLight("Hero".AsHaxeString());
        }

        // 接收网络数据
        public void ApplyInfoToHero(EntityInfo info)
        {
            var pos = info.PosVector;
            if (pos == null) return;

            double nx = pos.CX + pos.XR;
            double ny = pos.CY + pos.XY;

            if (hasData && (nx - lastSnapX) * (nx - lastSnapX) + (ny - lastSnapY) * (ny - lastSnapY) > TeleportDistSq)
            {
                snapshotBuffer.Clear();
            }

            lastSnapX = nx;
            lastSnapY = ny;
            snapshotBuffer.AddSnapshot(info.remoteTime / 1000.0, info);
            hasData = true;
        }


        public override void fixedUpdate()
        {
            if (hasData)
                Tick();

            base.fixedUpdate();
        }

        private void Tick()
        {
            double nowSec = GuestClientSession.SyncedTimeMs / 1000.0;
            if (!snapshotBuffer.TryGetInterpolation(nowSec, out var prev, out var next, out var alpha))
                return;

            var fromPos = prev.PosVector;
            var toPos = next.PosVector;
            if (fromPos == null || toPos == null)
                return;

            double renderTimeSec = nowSec - InterpolationDelaySeconds;
            var snap = Interp(fromPos, toPos, alpha);

            if (TryTeleport(toPos.DIR, in snap))
            {
                Present(prev, next, renderTimeSec);
                return;
            }

            Sync(in snap);
            Dir(in snap);
            Present(prev, next, renderTimeSec);
        }

        private void Present(EntityInfo prev, EntityInfo next, double renderTimeSec)
        {
            AffectCodec.Apply(this, prev.HeroEffectList, renderTimeSec);
            Animate(prev, next, renderTimeSec);
        }

        // 插值
        private InterpolatedSnapshot Interp(PosVector fromPos, PosVector toPos, double alpha)
        {
            return new InterpolatedSnapshot
            {
                SnapX = Lerp(fromPos.CX + fromPos.XR, toPos.CX + toPos.XR, alpha),
                SnapY = Lerp(fromPos.CY + fromPos.XY, toPos.CY + toPos.XY, alpha),
                NetDx = Lerp(fromPos.DX, toPos.DX, alpha),
                NetDy = Lerp(fromPos.DY, toPos.DY, alpha),
                NetBdx = Lerp(fromPos.BDX, toPos.BDX, alpha),
                NetBdy = Lerp(fromPos.BDY, toPos.BDY, alpha),
                TargetDir = alpha < 0.5 ? fromPos.DIR : toPos.DIR,
            };
        }

        // 瞬移
        private bool TryTeleport(int targetDir, in InterpolatedSnapshot snap)
        {
            double actualX = cx + xr;
            double actualY = cy + yr;
            double errorX = snap.SnapX - actualX;
            double errorY = snap.SnapY - actualY;

            if (errorX * errorX + errorY * errorY <= TeleportDistSq)
                return false;

            int scx = (int)System.Math.Floor(snap.SnapX);
            int scy = (int)System.Math.Floor(snap.SnapY);
            setPosCase(scx, scy, snap.SnapX - scx, snap.SnapY - scy);

            dx = snap.NetDx;
            dy = snap.NetDy;
            bdx = 0;
            bdy = 0;

            if (dir != targetDir) dir = targetDir;

            snapshotBuffer.Clear();
            return true;
        }

        // 位置修正,误差驱动目标速度 + 击退速度
        private void Sync(in InterpolatedSnapshot snap)
        {
            double actualX = cx + xr;
            double actualY = cy + yr;
            double errorX = snap.SnapX - actualX;
            double errorY = snap.SnapY - actualY;

            double targetDx = snap.NetDx + errorX * PositionCorrectionStrength;
            double targetDy = snap.NetDy + errorY * PositionCorrectionStrength;

            dx += (targetDx - dx) * VelocityResponse;
            dy += (targetDy - dy) * VelocityResponse;

            bdx = snap.NetBdx;
            bdy = snap.NetBdy;
        }

        // 方向
        private void Dir(in InterpolatedSnapshot snap)
        {
            if (dir != snap.TargetDir) dir = snap.TargetDir;
        }

        // 动画
        public void Animate(EntityInfo prev, EntityInfo next, double renderTimeSec)
        {
            var anim = spr?.get_anim();
            if (anim == null) return;

            var prevAnim = prev.animInfo;
            var nextAnim = next.animInfo;
            if (prevAnim == null && nextAnim == null) return;

            AnimInfo chosen = nextAnim;
            if (nextAnim == null)
            {
                chosen = prevAnim!;
            }
            else if (prevAnim != null)
            {
                double nextStartSec = nextAnim.StartTime / 1000.0;
                if (renderTimeSec < nextStartSec)
                    chosen = prevAnim;
            }

            if (chosen == null) return;

            string groupName = chosen.GroupName;
            if (string.IsNullOrEmpty(groupName)) return;

            // 打断翻滚
            if (IsRollRelatedGroup(groupName) && IsRollAct())
            {
                lastGroup = "";
                lastAppliedStartTime = -1;
                return;
            }

            bool needPlay = lastAppliedStartTime != chosen.StartTime
                         || lastGroup != groupName;

            if (needPlay)
            {
                lastGroup = groupName;
                lastAppliedStartTime = chosen.StartTime;

                var cur = anim.stack?.getDyn(0) as AnimInstance;
                if (cur != null) cur.plays = 0;

                anim.play(groupName.AsHaxeString(), chosen.Plays, null);
            }

            var stack = anim.stack?.getDyn(0) as AnimInstance;
            if (stack != null)
            {
                stack.speed = chosen.Speed;
                stack.paused = chosen.Paused;
                stack.playDuration = chosen.playDuration;
            }
        }

        // 辅助
        private bool IsAffectActive(int affectId)
        {
            var affects = this.affects;
            if (affects == null) return false;
            if (affectId < 0 || affectId >= affects.length) return false;
            if (!(affects.array[affectId] is ArrayObj list)) return false;
            return list.length > 0;
        }

        private static bool IsRollRelatedGroup(string name)
        {
            switch (name)
            {
                case "rolling":
                case "rollEnd":
                case "rollIdle":
                case "rollRun":
                    return true;
                default:
                    return false;
            }
        }

        private bool RunCondition()
        {
            double absDx = dx < 0.0 ? -dx : dx;

            if (inRun)
            {
                if (absDx < runSpd * 0.05)
                    inRun = false;
            }
            else
            {
                if (absDx > runSpd * 0.15)
                    inRun = true;
            }

            return inRun;
        }

        public override bool _isOnScreen() => true;

        private bool IsRollAct() => IsAffectActive(3) ? true : false;

        private static double Lerp(double a, double b, double t) => a + (b - a) * t;
    }
}