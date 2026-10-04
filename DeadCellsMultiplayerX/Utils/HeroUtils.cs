using System;
using System.Runtime.CompilerServices;
using dc;
using dc.en;
using dc.en.hero;
using dc.libs.heaps.slib;
using dc.libs.heaps.slib._AnimManager;
using DeadCellsMultiplayerX.Common.Data;
using DeadCellsMultiplayerX.Common.Serializers;
using Hashlink.Virtuals;
using HaxeProxy.Runtime;
using ModCore.Utilities;

namespace DeadCellsMultiplayerX.Utils
{
    /// 采集Hero同步信息
    public static class HeroUtils
    {
        private static readonly ConditionalWeakTable<HSprite, SpriteInfo> spriteCache = new();

        /// <summary>
        /// 收集一个 Hero 的完整信息
        /// </summary>
        /// <param name="hero"></param>
        /// <param name="guid"></param>
        /// <param name="remoteTime">当前服务器/会话时间戳</param>
        /// <param name="atlasResolver">atlasPath</param>
        public static EntityInfo Collect(
            Entity hero,
            string guid,
            long remoteTime,
            Func<SpriteLib, string?>? atlasResolver = null)
        {
            var info = new EntityInfo { GUID = guid };
            FillEntityInfo(hero, info, remoteTime, atlasResolver);
            return info;
        }

        public static void FillEntityInfo(
            Entity e,
            EntityInfo inf,
            long remoteTime,
            Func<SpriteLib, string?>? atlasResolver = null)
        {
            inf.TypeName = e.GetType().FullName;

            if (!e.initDone) return;

            inf.SubLevelId = e._level.GetSubLevelIndex();
            inf.remoteTime = remoteTime;

            if (e.spr != null)
            {
                inf.PosVector = new PosVector(e.cx, e.cy, e.xr, e.yr, e.dir,e.dx, e.dy, e.bdx, e.bdy);

                var sinfo = GetSpriteInfo(e.spr);
                inf.MainSprite = sinfo;
                FillSpriteInfo(e.spr, inf.GUID, sinfo, atlasResolver);
                FillEntityAnimInfo(inf, e.spr);
                FillEntityGlowkeyData(e, inf);
            }
        }

        private static SpriteInfo GetSpriteInfo(HSprite spr)
        {
            if (!spriteCache.TryGetValue(spr, out var result))
            {
                result = new SpriteInfo();
                spriteCache.Add(spr, result);
            }
            return result;
        }

        private static void FillSpriteInfo(
            HSprite spr,
            string? parent,
            SpriteInfo inf,
            Func<SpriteLib, string?>? atlasResolver)
        {
            if (atlasResolver != null && spr.lib != null)
            {
                var atlasPath = atlasResolver(spr.lib);
                if (atlasPath != null)
                {
                    inf.AtlasName = atlasPath;
                    inf.GroupName = spr.groupName.ToString();
                }
            }

            inf.PivotData = DCMXSerializers.MessagePack.Serialize(spr?.pivot);
            inf.Parent = parent;

            var children = spr?.children;
            inf.Children.Clear();

            if (children != null)
            {
                for (int i = 0; i < children.length; i++)
                {
                    var child = children.getDyn(i) as HSprite;
                    if (child == null) continue;

                    var sinfo = GetSpriteInfo(child);
                    inf.Children.Add(sinfo);
                    FillSpriteInfo(child, inf.GUID, sinfo, atlasResolver);
                }
            }
        }

        public static void FillEntityAnimInfo(EntityInfo inf, HSprite spr)
        {
            var anim = spr.get_anim();
            if (spr != null && anim != null && !anim.destroyed && anim.stack.length > 0)
            {
                var current = anim.stack.getDyn(0) as AnimInstance;
                var transitions = anim.transitions;
                if (current != null)
                {
                    AnimInfo info = new AnimInfo
                    {
                        Speed = current.speed,
                        Paused = current.paused,
                        Frame = spr.frame,
                        Plays = current.plays,
                        playDuration = current.playDuration,
                    };

                    if (transitions != null
                        && inf.animInfo.AnimTransitions.Count == 0
                        && transitions.length > 0)
                    {
                        foreach (Transition data in transitions)
                        {
                            var tr = new AnimTransitions
                            {
                                Anim = data.anim.ToString(),
                                From = data.from.ToString(),
                                To = data.to.ToString(),
                                reverse = data.reverse,
                                speed = data.spd,
                            };
                            inf.animInfo.AnimTransitions.Add(tr);
                        }
                    }
                    inf.animInfo = info;
                }
            }
        }

        public static void FillEntityGlowkeyData(Entity e, EntityInfo info)
        {
            var glow = (dc.shader.GlowKey)e.spr.getShader(dc.shader.GlowKey.Class);
            if (info.GlowData.Count == 0 && glow != null)
            {
                var array = glow.getGlowDatas();
                for (int i = 0; i < array.length; i++)
                {
                    var data = array.getDyn(i);
                    var virtuals = ((HaxeProxyBase)data)
                        .ToVirtual<virtual_animationIntensity_animationScale_animationSpeed_animationTextureMask_inner_key_outer_power_>();
                    info.GlowData.Add(i, DCMXSerializers.MessagePack.Serialize(virtuals));
                }
            }
        }

        public static void ClearSpriteCache()
        {
            spriteCache.Clear();
        }
    }
}