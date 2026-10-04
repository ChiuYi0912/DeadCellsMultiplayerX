using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dc;
using dc.en;
using dc.libs.heaps.slib._AnimManager;
using dc.pr;
using DeadCellsMultiplayerX.Common.Data;
using ModCore.Utilities;
using Serilog.Core;

namespace DeadCellsMultiplayerX.Client.Guest.WorldX.Entitys
{
    public class RemoteHero : KingSkin
    {
        private string lastGroup = "";
        private bool inRun;
        private const double PosCorrectDistSq = 8.0;   // 2² = 4

        private int netCx, netCy;
        private double netXr, netYr;
        private double netDx, netDy, netBdx, netBdy;
        private int netDir;
        private bool hasData;

        public RemoteHero(Level lvl, int x, int y) : base(lvl, x, y)
        {
            set_easeSpritePos(false);
            hasWineGlass = false;
        }

        public override void init()
        {
            base.init();
            hasRepelling = false;
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

            //anim.removeAllStateAnims();
        }

        public void ApplyInfoToHero(EntityInfo info)
        {
            var pos = info?.PosVector;
            if (pos == null) return;

            double curX = cx + xr, curY = cy + yr;
            double nX = pos.CX + pos.XR, nY = pos.CY + pos.XY;
            double dX = nX - curX, dY = nY - curY;
            if (dX * dX + dY * dY > PosCorrectDistSq)
                setPosCase(pos.CX, pos.CY, pos.XR, pos.XY);

            netCx = pos.CX; netCy = pos.CY;
            netXr = pos.XR; netYr = pos.XY;
            netDir = pos.DIR;
            netDx = pos.DX; netDy = pos.DY;
            netBdx = pos.BDX; netBdy = pos.BDY;

            hasData = true;
        }

        public void UpdateAnim(EntityInfo info)
        {
            var animinfo = info.animInfo;
            var anim = spr.get_anim();
            if (spr == null || info == null || info.MainSprite == null || animinfo == null || anim == null) return;

            if (lastGroup != info.MainSprite.GroupName)
            {
                lastGroup = info.MainSprite.GroupName;
                var cur = anim.stack?.getDyn(0) as AnimInstance;
                if (cur != null) cur.plays = 0;
                anim.play(info.MainSprite.GroupName.AsHaxeString(), info.animInfo.Plays, null).loop(null);
            }

            var stack = anim.stack?.getDyn(0) as AnimInstance;
            if (stack != null)
            {
                stack.speed = info.animInfo.Speed;
                stack.paused = info.animInfo.Paused;
                stack.playDuration = info.animInfo.playDuration;
            }
        }


        /// <summary>
        /// 更新完后获取信息
        /// </summary>
        public override void postUpdate()
        {
            base.postUpdate();
        }

        /// <summary>
        /// 帧更新之前写入
        /// </summary>
        public override void fixedUpdate()
        {
            if (!hasData) { base.fixedUpdate(); return; }

            dx = netDx; dy = netDy;
            bdx = netBdx; bdy = netBdy;
            if (dir != netDir) dir = netDir;

            base.fixedUpdate();
        }


        // private bool RunConditionNoCinematic()
        // {
        //     double absDx = dx < 0.0 ? -dx : dx;
        //     if (absDx <= runSpd * 0.25) return false;

        //     if (dy < -0.1 || dy > 0.1) return false;

        //     return true;
        // }
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
    }
}