using CoreLibrary.Core.Extensions;
using dc;
using dc.en;
using dc.hl.types;
using dc.libs.heaps.slib;
using dc.libs.heaps.slib._AnimManager;
using dc.pr;
using DeadCellsMultiplayerX.Common.Data;
using DeadCellsMultiplayerX.Common.Data.Snapshot;
using DeadCellsMultiplayerX.Common.Serializers;
using DeadCellsMultiplayerX.Utils;
using DeadCellsSync.Core.Snapshot;
using Hashlink.Virtuals;
using ModCore.Serialization;
using ModCore.Storage;
using ModCore.Utilities;

namespace DeadCellsMultiplayerX.Client.Guest.WorldX.Remote
{
    internal class RemoteHero : KingSkin, IHxbitSerializable<RemoteHero.IHxData>
    {
        private readonly GuestClientSession session;

        // 常量
        private const double InterpolationDelaySeconds = 0.1;// 渲染延迟100ms，保证总有两个快照可插值，平滑网络状态
        private const double TeleportDistSq = 25.0 * 25.0;// 与快照距离平方超过25格时直接瞬移，避免远距离拉扯
        private const double PositionCorrectionStrength = 2.0;// 位置误差拉动目标速度的强度，越大追得越快，越小越平滑
        private const double VelocityResponse = 0.25;// 每物理帧向目标速度靠拢的比例，越大响应越快，越小越平滑

        // 状态
        private readonly SnapshotBuffer<RemoteHeroSnapshot> snapshotBuffer = new(InterpolationDelaySeconds);
        private readonly RemoteHeroSnapshot current = new();
        private bool hasData;
        private double lastSnapX, lastSnapY;
        private string lastGroup = "";
        private long lastAppliedStartTime = -1;
        private bool inRun;
        public string LevelId { get; set; } = string.Empty;
        public int SubLevelIndex { get; set; } = -1;

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

        public class IHxData { }


        public RemoteHero(GuestClientSession session, Level lvl, int x = 0, int y = 0) : base(lvl, x, y)
        {
            this.session = session;
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
            frict = 0;
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

        //初始化皮肤
        public void ApplySpawn(HeroSpawn spawn)
        {
            LevelId = spawn.LevelId;
            SubLevelIndex = spawn.SubLevelId;

            if (string.IsNullOrEmpty(spawn.ColorMapModel)
                || string.IsNullOrEmpty(spawn.ColorMapSkin)
                || spawn.MainSprite == null) return;

            var sprlib = Assets.Class.lib.get(spawn.MainSprite.AtlasName.AsHaxeString());
            var group = spawn.MainSprite.GroupName.AsHaxeString();
            dc.h3d.mat.Texture normalMapFromGroup = sprlib.getNormalMapFromGroup(group);
            initSprite(sprlib, group, null, null, null, true, null, normalMapFromGroup);

            spr.pivot.copyFrom(
                DCMXSerializers.MessagePack.Deserialize<SpritePivot>(spawn.MainSprite.PivotData));

            setColorMap(
                spawn.ColorMapModel.AsHaxeString(),
                spawn.ColorMapSkin.AsHaxeString(),
                null);

            foreach (var (idx, gdd) in spawn.GlowData)
            {
                if (gdd == null) continue;
                setGlowData(idx,
                    DCMXSerializers.MessagePack.Deserialize<virtual_animationIntensity_animationScale_animationSpeed_animationTextureMask_inner_key_outer_power_>(gdd),
                    spr);
            }
        }

        /// <summary>
        /// 将状态写入缓冲区
        /// </summary>
        /// <param name="dyn"></param>
        /// <param name="serverTimeMs"></param>
        public void PushState(HeroDynamic dyn, double serverTimeMs)
        {
            if (dyn == null) return;

            if (dyn.HasLevel)
            {
                LevelId = dyn.LevelId;
                SubLevelIndex = dyn.SubLevelIndex;
            }

            if (dyn.HasPos) current.Pos = dyn.Pos;
            if (dyn.HasAnim) current.Anim = dyn.Anim;
            if (dyn.HasAffects) current.Affects = dyn.Affects;

            var pos = current.Pos;
            double nx = pos.CX + pos.XR;
            double ny = pos.CY + pos.XY;

            if (hasData
                && (nx - lastSnapX) * (nx - lastSnapX) + (ny - lastSnapY) * (ny - lastSnapY) > TeleportDistSq)
            {
                snapshotBuffer.Clear();
            }

            lastSnapX = nx;
            lastSnapY = ny;
            snapshotBuffer.AddSnapshot(serverTimeMs / 1000.0, current.Clone());
            hasData = true;


        }

        //固定更新
        public override void fixedUpdate()
        {
            if (hasData)
                Tick();

            base.fixedUpdate();
        }

        private void Tick()
        {
            double nowSec = session.SyncedTimeMs / 1000.0;
            if (!snapshotBuffer.TryGetInterpolation(nowSec, out var prev, out var next, out var alpha))
                return;

            var fromPos = prev.Pos;
            var toPos = next.Pos;
            if (fromPos == null || toPos == null) return;

            double renderTimeSec = nowSec - InterpolationDelaySeconds;
            var snap = Interp(fromPos, toPos, alpha);

            if (TryTeleport(toPos.DIR, in snap))
            {
                Present(prev, next, renderTimeSec);
                return;
            }

            //更新朝向
            if (dir != snap.TargetDir) dir = snap.TargetDir;

            Sync(in snap);
            Present(prev, next, renderTimeSec);
        }

        private void Present(RemoteHeroSnapshot prev, RemoteHeroSnapshot next, double renderTimeSec)
        {
            //更新effct数组
            AffectCodec.Apply(this, prev.Affects, renderTimeSec);

            //播放动画
            Animate(prev.Anim, next.Anim, renderTimeSec);
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

        // 位置修正,误差驱动目标速度
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

        // 动画
        public void Animate(AnimInfo prevAnim, AnimInfo nextAnim, double renderTimeSec)
        {
            var anim = spr?.get_anim();
            if (anim == null) return;
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

        //辅助 
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

        private bool IsRollAct() => IsAffectActive(3);

        private static double Lerp(double a, double b, double t) => a + (b - a) * t;

        IHxData IHxbitSerializable<IHxData>.GetData()
        {
            return new();
        }

        void IHxbitSerializable<IHxData>.SetData(IHxData data)
        {

        }
    }
}