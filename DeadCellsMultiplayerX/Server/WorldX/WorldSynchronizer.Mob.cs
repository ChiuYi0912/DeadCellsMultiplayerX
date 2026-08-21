using System;
using System.Collections.Generic;
using DeadCellsMultiplayerX.Common.Data;
using DeadCellsMultiplayerX.Common.Serializers;
using DeadCellsMultiplayerX.Server.Events;

namespace DeadCellsMultiplayerX.Server.WorldX
{
    internal partial class WorldSynchronizer :
    IOnAttachMob,
    IOnCreateMob
    {
        /// <summary>
        /// 用于传输给客户端的dc.level.Mob
        /// </summary>
        public Dictionary<string, byte[]> LevelMobs = [];

        /// <summary>
        /// 用于传输给客户端游戏运行时生成的Mob
        /// 
        /// 会包含LevelMobs
        /// </summary>
        public Dictionary<string, byte[]> dynamicLevelMobs = [];


        // 基于索引数组存储
        private EntityInfo[] mobinfos = new EntityInfo[1024];
        private readonly Dictionary<nint, int> pointerToIndex = new();
        private readonly Dictionary<string, int> guidToIndex = new();
        private int count; // 当前实体数量

        /// <summary>
        /// 注册实体
        /// </summary>
        public int RegisterEntity(nint pointer, EntityInfo info)
        {
            if (pointerToIndex.ContainsKey(pointer)) return 0;

            if (count >= mobinfos.Length)
            {
                Array.Resize(ref mobinfos, mobinfos.Length * 2);
            }

            int index = count++;
            mobinfos[index] = info;
            pointerToIndex[pointer] = index;
            guidToIndex[info.GUID] = index;
            return index;
        }

        /// <summary>
        /// 通过指针获取实体
        /// </summary>
        public EntityInfo? GetEntityByPointer(nint pointer)
        {
            if (pointerToIndex.TryGetValue(pointer, out int index))
            {
                return mobinfos[index];
            }
            return null;
        }

        /// <summary>
        /// 通过 GUID 获取实体
        /// </summary>
        public EntityInfo? GetEntityByGuid(string guid)
        {
            if (guidToIndex.TryGetValue(guid, out int index))
            {
                return mobinfos[index];
            }
            return null;
        }

        /// <summary>
        /// 通过指针移除实体
        /// </summary>
        public bool RemoveEntityByPointer(nint pointer)
        {
            if (pointerToIndex.Remove(pointer, out int index))
            {
                var info = mobinfos[index];
                guidToIndex.Remove(info.GUID);
                mobinfos[index] = null!;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 通过 GUID 移除实体
        /// </summary>
        public bool RemoveEntityByGuid(string guid)
        {
            if (guidToIndex.Remove(guid, out int index))
            {
                var info = mobinfos[index];
                foreach (var kv in pointerToIndex)
                {
                    if (kv.Value == index)
                    {
                        pointerToIndex.Remove(kv.Key);
                        break;
                    }
                }
                mobinfos[index] = null!;
                return true;
            }
            return false;
        }

        void IOnAttachMob.OnAttachMob(IOnAttachMob.Data data)
        {
            var info = new EntityInfo();
            LevelMobs.Add(info.GUID, DCMXSerializers.MessagePack.Serialize(data.mobdata));
        }

        void IOnCreateMob.OnCretaMob(IOnCreateMob.Data data)
        {
            var info = new EntityInfo();
            var runtimeMob = new dc.level.Mob(data.k, data.cx, data.cy, data.dmgTier, data.lifeTier, data.mob.elite)
            {
                loots = data.mob.loots
            };

            dynamicLevelMobs.Add(info.GUID, DCMXSerializers.MessagePack.Serialize(runtimeMob));
            RegisterEntity(data.mob.HashlinkPointer, info);
        }
    }
}