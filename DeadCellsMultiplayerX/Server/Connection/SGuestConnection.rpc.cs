using dc;
using dc.en;
using dc.libs.heaps.slib;
using DeadCellsMultiplayerX.Client;
using DeadCellsMultiplayerX.Common;
using DeadCellsMultiplayerX.Common.Data;
using DeadCellsMultiplayerX.Utils;
using ModCore.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DeadCellsMultiplayerX.Server
{
    internal partial class SGuestConnection : IServerRPC
    {
        public Task<bool> CheckVersion(string version)
        {
            if (Version.Parse(version) != VersionUtils.ModVersion)
            {
                Logger.Error("Dismatch version: {ver}", guestInfo.Guid, version);
                Task.Delay(TimeSpan.FromSeconds(5)).ContinueWith(_ =>
                {
                    if (rpc == null)
                    {
                        return;
                    }
                    if (!rpc.IsDisposed)
                    {
                        Dispose();
                    }
                });
                return Task.FromResult(false);
            }
            return Task.FromResult(true);
        }

        public Task SetGuestInfo(GuestInfo info)
        {
            GuestInfo = info;
            return Task.CompletedTask;
        }

        public async Task UploadSavedata(byte[] data)
        {
            await Task.Delay(1).ConfigureAwait(false);

            var savePath = ServerMain.Instance.savePath;

            await File.WriteAllBytesAsync(savePath, data);

            Logger.Information("Saving savedata to {path}", savePath);

            Logger.Information("Building game...");

            await Main.LaunchGame();

            await Task.Delay(1).ConfigureAwait(false);
        }

        public async Task<byte[]> DownloadSavedata()
        {
            while (string.IsNullOrEmpty(Main.savePath))
            {
                await Task.Yield();
            }
            return File.ReadAllBytes(Main.savePath);
        }

        public async Task<AreaInfo> RequestAreaInfo(IServerRPC.AreaInfoRequest request)
        {
            if (request.X < 0)
            {
                request.X = 0;
            }
            if (request.Y < 0)
            {
                request.Y = 0;
            }
            var areaInfo = new AreaInfo()
            {
                X = request.X,
                Y = request.Y,
                Width = request.Width,
                Height = request.Height
            };

            var lvl = (dc.pr.Level)dc.pr.Game.Class.ME.subLevels.getDyn(request.SubLevelId);
            var map = lvl.map;

            areaInfo.Collision = new int[areaInfo.Width * areaInfo.Height];

            // 碰撞箱
            unsafe
            {
                var src = new Span<int>((void*)map.collisions.bytes, map.collisions.length);
                for (int y = 0; y < areaInfo.Height; y++)
                {
                    var dst = new Span<int>(areaInfo.Collision, y * areaInfo.Width, areaInfo.Width);
                    src.Slice((areaInfo.Y + y) * map.wid + areaInfo.X, areaInfo.Width).CopyTo(dst);
                }
            }

            // Entity
            {
                var rx = areaInfo.X;
                var ry = areaInfo.Y;
                var rxt = areaInfo.X + areaInfo.Width;
                var ryt = areaInfo.Y + areaInfo.Height;

                foreach (Entity v in lvl.entities)
                {
                    if(v is Interactive)
                    {
                        continue;
                    }

                    if (v.cx >= rx && v.cx <= rxt && v.cy >= ry && v.cy <= ryt && v.visible)
                    {
                        EntityInfo inf = GetEntityInfo(v).info;

                        v.isOnScreen = true;

                        inf.TypeName = v.GetType().FullName;

                        inf.EntityData.Serialize(v, typeof(Entity));
                        inf.SpritePivotData.Serialize(v.spr?.pivot, typeof(SpritePivot));

                        var spr = v.spr;
                        if (spr != null)
                        {
                            if (ServerMain.Instance.spriteLib2altas.TryGetValue(spr.lib, out var atlasPath))
                            {
                                inf.AtlasName = atlasPath;
                                inf.GroupName = spr.groupName.ToString();
                                inf.Frame = spr.frame;
                            }
                        }

                        areaInfo.Entities.Add(inf);
                    }
                }
            }

            return areaInfo;
        }

        public Task<EntityInfo> RequestEntityInfo(string guid)
        {
            throw new NotImplementedException();
        }

        public Task<long> GetTimeStamp()
        {
            return Task.FromResult(Session.CurrentTimeStamp);
        }


    }
}
