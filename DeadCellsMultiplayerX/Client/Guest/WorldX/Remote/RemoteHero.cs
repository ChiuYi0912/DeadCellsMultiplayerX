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
using DeadCellsMultiplayerX.Common.Tools;
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
        private readonly RemoteMover<RemoteHero, RemoteHeroSnapshot> mover;
        private readonly RemoteHeroSnapshot current = new();

        private string lastGroup = "";
        private long lastAppliedStartTime = -1;
        private bool inRun;
        public string LevelId { get; set; } = string.Empty;
        public int SubLevelIndex { get; set; } = -1;

        public class IHxData { }

        public RemoteHero(GuestClientSession session, Level lvl, int x = 0, int y = 0) : base(lvl, x, y)
        {
            this.session = session;
            set_easeSpritePos(false);
            hasWineGlass = false;

            mover = new RemoteMover<RemoteHero, RemoteHeroSnapshot>(
                snap => snap.Pos,
                interpolationDelaySeconds: 0.1,
                teleportDistance: 25.0 * 25.0,
                positionCorrectionStrength: 2.0,
                velocityResponse: 0.25);
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
            anim.removeAllStateAnims();

            createConfLight("Hero".AsHaxeString());
        }

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

            mover.Push(current.Clone(), serverTimeMs / 1000.0);
        }

        public override void fixedUpdate()
        {
            if (mover.Tick(session.SyncedTimeMs / 1000.0, this,
                           out var prev, out var next, out var renderTimeSec))
            {
                Present(prev, next, renderTimeSec);
            }

            base.fixedUpdate();
        }

        private void Present(RemoteHeroSnapshot prev, RemoteHeroSnapshot next, double renderTimeSec)
        {
            AffectCodec.Apply(this, prev.Affects, renderTimeSec);
            Animate(prev.Anim, next.Anim, renderTimeSec);
        }

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

        IHxData IHxbitSerializable<IHxData>.GetData()
        {
            return new();
        }

        void IHxbitSerializable<IHxData>.SetData(IHxData data)
        {
        }
    }
}