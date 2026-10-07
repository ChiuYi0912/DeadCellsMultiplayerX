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
        private string lastGroup = "";
        private bool inRun;


        private const double InterpolationDelaySeconds = 0.1; // 渲染延迟100ms，保证总有两个快照可插值，平滑网络状态
        private const double TeleportDistSq = 25.0 * 25.0; // 与快照距离平方超过25格时直接瞬移，避免远距离拉扯


        private const double PositionCorrectionStrength = 2.0; // 位置误差拉动目标速度的强度，越大追得越快，越小越平滑
        private const double VelocityResponse = 0.25; // 每物理帧向目标速度靠拢的比例，越大响应越快，越小越平滑

        private readonly SnapshotBuffer<EntityInfo> snapshotBuffer = new(InterpolationDelaySeconds);
        private bool hasData;
        private double lastSnapX, lastSnapY;

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
               new(RunConditionNoCinematic),
               default,
               null);
            anim.removeAllStateAnims();

            createConfLight("Hero".AsHaxeString());
        }

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
            if (!hasData) { base.fixedUpdate(); return; }

            if (snapshotBuffer.TryGetInterpolation(GuestClientSession.SyncedTimeMs / 1000.0, out var prev, out var next, out var alpha))
            {
                var fromPos = prev.PosVector;
                var toPos = next.PosVector;

                if (fromPos != null && toPos != null)
                {
                    //网络目标位置（仅用于测量漂移）
                    double snapX = Lerp(fromPos.CX + fromPos.XR, toPos.CX + toPos.XR, alpha);
                    double snapY = Lerp(fromPos.CY + fromPos.XY, toPos.CY + toPos.XY, alpha);

                    //网络目标速度
                    double netDx = Lerp(fromPos.DX, toPos.DX, alpha);
                    double netDy = Lerp(fromPos.DY, toPos.DY, alpha);

                    //与实际模拟位置相比的位置误差
                    double actualX = cx + xr;
                    double actualY = cy + yr;
                    double errorX = snapX - actualX;
                    double errorY = snapY - actualY;

                    // 严重不同步,直接瞬移并重置
                    if (errorX * errorX + errorY * errorY > TeleportDistSq)
                    {
                        int scx = (int)System.Math.Floor(snapX);
                        int scy = (int)System.Math.Floor(snapY);
                        setPosCase(scx, scy, snapX - scx, snapY - scy);
                        dx = netDx; dy = netDy;
                        bdx = 0; bdy = 0;
                        if (dir != toPos.DIR) dir = toPos.DIR;
                        snapshotBuffer.Clear();
                        UpdateAnim(next);
                        AffectCodec.Apply(this, prev.HeroEffectList);
                        base.fixedUpdate();
                        return;
                    }

                    //根据位置误差对目标速度进行校准
                    double targetDx = netDx + errorX * PositionCorrectionStrength;
                    double targetDy = netDy + errorY * PositionCorrectionStrength;

                    //将 dx/dy 加速至目标速度
                    dx += (targetDx - dx) * VelocityResponse;
                    dy += (targetDy - dy) * VelocityResponse;

                    //击退速度由服务器控制
                    bdx = Lerp(fromPos.BDX, toPos.BDX, alpha);
                    bdy = Lerp(fromPos.BDY, toPos.BDY, alpha);

                    //根据alpha值选择更近的快照,设置方向
                    int targetDir = alpha < 0.5 ? fromPos.DIR : toPos.DIR;
                    if (dir != targetDir) dir = targetDir;

                    //collisionMode = CollisionModeLookup.Get(prev.CollisionMode);
                    AffectCodec.Apply(this, prev.HeroEffectList);
                    UpdateAnim(prev);
                }
            }

            base.fixedUpdate();
        }

        public void UpdateAnim(EntityInfo info)
        {
            var animinfo = info.animInfo;
            var anim = spr.get_anim();
            if (spr == null || info == null || info.MainSprite == null || animinfo == null || anim == null) return;

            string groupName = info.MainSprite.GroupName;

            if (IsRollRelatedGroup(groupName) && IsRollAct())
            {
                lastGroup = "";
                return;
            }

            if (lastGroup != groupName)
            {
                lastGroup = groupName;
                var cur = anim.stack?.getDyn(0) as AnimInstance;
                if (cur != null) cur.plays = 0;

                anim.play(groupName.AsHaxeString(), info.animInfo.Plays, null);
            }

            var stack = anim.stack?.getDyn(0) as AnimInstance;
            if (stack != null)
            {
                stack.speed = info.animInfo.Speed;
                stack.paused = info.animInfo.Paused;
                stack.playDuration = info.animInfo.playDuration;
            }
        }




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

        private bool RunConditionNoCinematic()
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
        private bool IsRollAct() => IsAffectActive(3) ? true : false;   /// 检查本地英雄是否处于与翻滚状态


        private static double Lerp(double a, double b, double t) => a + (b - a) * t;
    }
}
