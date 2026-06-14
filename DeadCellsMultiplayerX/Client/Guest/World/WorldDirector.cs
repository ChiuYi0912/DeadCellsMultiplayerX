using dc.hxd.res;
using dc.libs.heaps.slib;
using dc.libs.heaps.slib.assets;
using dc.pr;
using DeadCellsMultiplayerX.Client.Guest.World;
using DeadCellsMultiplayerX.Common;
using DeadCellsMultiplayerX.Server;
using ModCore.Events.Interfaces.Game.Hero;
using ModCore.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeadCellsMultiplayerX.Client.Guest.Ghost
{
    /// <summary>
    /// 控制客户端的表现
    /// </summary>
    /// <param name="session"></param>
    internal class WorldDirector(GuestClientSession session) : DisposableEventReceiver,
        IOnHeroUpdate
    {


        // 客户端 Viewport 的 Tile 大小
        public static readonly int VIEWPORT_WIDTH = 64; 
        public static readonly int VIEWPORT_HEIGHT = 64;

        public GuestClientSession Session => session;

        private readonly Dictionary<string, EntityGhost> ghosts = [];

        private readonly Dictionary<string, SpriteLib> spriteLibLookup = [];

        protected override void MyDispose()
        {
            base.MyDispose();

            foreach(var v in ghosts.Values.ToArray())
            {
                v.Dispose();
            }
            ghosts.Clear();
        }

        public async Task Init()
        {
            _ = Loop();
        }

        private async Task Loop()
        {
            SynchronizationContext.SetSynchronizationContext(ModCore.Modules.Game.SynchronizationContext);

            while(true)
            {
                await Task.Delay(1000 / 30);
                DisposeToken.ThrowIfCancellationRequested();

                await UpdateWorld();
            }
        }

        public SpriteLib GetSpriteLib(string atlasPath)
        {
            if(!spriteLibLookup.TryGetValue(atlasPath, out var spriteLib))
            {
                spriteLib = dc.libs.heaps.slib.assets.Atlas.Class.load(atlasPath.AsHaxeString(), null, null, null);
                spriteLibLookup.Add(atlasPath, spriteLib);
            }
            return spriteLib;
        }

        private async Task UpdateWorld()
        {
            var hero = session.Game?.hero;

            if(hero == null)
            {
                return;
            }

            var lvl = hero._level;

            if(lvl == null)
            {
                return;
            }

            var gm = Game.Class.ME;
            var map = lvl.map;
            var vp = hero._level.viewport;
            var tileX = vp.realX / 24;
            var tileY = vp.realY / 24;

            int subLevelId = 0;

            for(int i = 0; i < gm.subLevels.length; i++)
            {
                if((Level) gm.subLevels.getDyn(i) == lvl)
                {
                    subLevelId = i;
                }
            }

            var request = new IServerRPC.AreaInfoRequest()
            {
                X = (int)tileX - VIEWPORT_WIDTH / 2,
                Y = (int)tileY - VIEWPORT_HEIGHT / 2,
                Width = VIEWPORT_HEIGHT,
                Height = VIEWPORT_WIDTH,
                SubLevelId = subLevelId 
            };

            if(request.X < 0)
            {
                request.X = 0;
            }
            if(request.Y < 0)
            {
                request.Y = 0;
            }
            if(request.X + request.Width >= map.wid)
            {
                request.Width = map.wid - request.X;
            }
            if(request.Y + request.Height >= map.hei)
            {
                request.Height = map.hei - request.Y;
            }

            var result = await session.Server.RequestAreaInfo(request);

            //同步碰撞箱

            if (result.Collision != null)
            {
                unsafe
                {
                    var dst = new Span<int>((void*)map.collisions.bytes, map.collisions.length);
                    for (int y = 0; y < result.Height; y++)
                    {
                        var src = new Span<int>(result.Collision, y * result.Width, result.Width);
                        src.CopyTo(dst.Slice((result.Y + y) * map.wid + result.X, map.wid));
                    }
                }
            }
            
            // 同步 Entity

            foreach(var v in ghosts.Values)
            {
               v.SetVisble(false); //隐藏不可见的 Entity (状态未知)
            }
            
            // 更新 Ghost 状态
            foreach(var v in result.Entities)
            {
                //Logger.Information("Syncing entity: {type} {pos}", v.TypeName, v.Position);
                if(!ghosts.TryGetValue(v.GUID, out var ghost))
                {
                    ghost = new(this, lvl, v.GUID);
                    ghosts.Add(v.GUID, ghost);
                }
                ghost.SetVisble(true);
                ghost.UpdateInfo(v);
            }

        }

        void IOnHeroUpdate.OnHeroUpdate(double dt)
        {
            foreach(var v in ghosts.Values.ToArray())
            {
                v.Update();
            }
        }
    }
}
