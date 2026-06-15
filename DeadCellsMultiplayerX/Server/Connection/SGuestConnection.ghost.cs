using dc;
using dc.libs.heaps.slib;
using DeadCellsMultiplayerX.Common.Data;
using DeadCellsMultiplayerX.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace DeadCellsMultiplayerX.Server.Connection
{
    internal partial class SGuestConnection
    {

        private readonly Dictionary<nint, EntityInfo> entitiesInfo = [];
        private readonly Dictionary<string, EntityInfo> guid2entityInfoLookup = [];
        private readonly Dictionary<nint, SpriteInfo> spritesInfo = [];

        private SpriteInfo GetSpriteInfo(HSprite spr)
        {
            if(!spritesInfo.TryGetValue(spr.HashlinkPointer, out var result))
            {
                result = new();
                spritesInfo.Add(spr.HashlinkPointer, result);
            }
            return result;
        }
        private EntityInfo GetEntityInfo(Entity e)
        {
            if (!entitiesInfo.TryGetValue(e.HashlinkPointer, out var result))
            {
                result = new();
                entitiesInfo.Add(e.HashlinkPointer, result);
                guid2entityInfoLookup[result.GUID] = result;
            }
            return result;
        }

        private EntityInfo? GetEntityInfo(string guid)
        {
            if (guid2entityInfoLookup.TryGetValue(guid, out var result))
            {
                return result;
            }
            return null;
        }

        private void FillSpriteInfo(HSprite spr, string? parent, SpriteInfo inf)
        {
            if (ServerMain.Instance.spriteLib2altas.TryGetValue(spr.lib, out var atlasPath))
            {
                inf.AtlasName = atlasPath;
                inf.GroupName = spr.groupName.ToString();
                inf.Frame = spr.frame;
            }

            inf.PivotData.Serialize(spr.pivot, typeof(SpritePivot));
            inf.Parent = parent;

            var children = spr.children;

            inf.Children.Clear();
            for(int i = 0; i < children.length; i++)
            {
                var child = children.getDyn(i) as HSprite;
                if(child == null)
                {
                    continue;
                }

                var sinfo = GetSpriteInfo(child);
                inf.Children.Add(sinfo);
                FillSpriteInfo(child, inf.GUID, sinfo);
            }
        }

        private void FillEntityInfo(Entity e, EntityInfo inf)
        {
            inf.TypeName = e.GetType().FullName;

            inf.SubLevelId = e._level.GetSubLevelIndex();
            inf.EntityData.Serialize(e, typeof(Entity));

            if(e.spr != null)
            {
                var sinfo = GetSpriteInfo(e.spr);
                inf.MainSprite = sinfo;
                FillSpriteInfo(e.spr, inf.GUID, sinfo);
            }
        }

        private bool TryGetInfoIfVisable(Entity e, [NotNullWhen(true)] out EntityInfo? info)
        {
            if (lastRequest == null)
            {
                info = null;
                return false;
            }
            var rect = lastRequest.Rect;
            var rx = rect.X;
            var ry = rect.Y;
            var rxt = rect.X + rect.Width;
            var ryt = rect.Y + rect.Height;
            if (e.cx >= rx && e.cx <= rxt && e.cy >= ry && e.cy <= ryt && e.visible)
            {
                EntityInfo inf = GetEntityInfo(e);

                e.isOnScreen = true;

                inf.TypeName = e.GetType().FullName;

                info = inf;
                return true;
            }
            info = null;
            return false;
        }

        private bool TryUpdateEntity(Entity e)
        {
            if (TryGetInfoIfVisable(e, out var inf))
            {
                FillEntityInfo(e, inf);
                guest.UpdateEntity(inf);
                return true;
            }
            return false;
        }
    }
}
