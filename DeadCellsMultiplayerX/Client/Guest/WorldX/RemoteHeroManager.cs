using System.Collections.Generic;
using dc.en;
using DeadCellsMultiplayerX.Client.Event;
using DeadCellsMultiplayerX.Client.Guest.WorldX.Remote;
using DeadCellsMultiplayerX.Common;
using DeadCellsMultiplayerX.Common.Data.Snapshot;

namespace DeadCellsMultiplayerX.Client.Guest.WorldX
{
    /// <summary>
    /// 管理所有远端玩家。
    /// 由 <see cref="RemoteWorld"/> 分发 WorldSnapshot 驱动。
    /// </summary>
    internal class RemoteHeroManager : DisposableEventReceiver,
    IOnLocalHeroInitDone
    {
        private readonly GuestClientSession session;
        private readonly RemoteWorld remoteWorld;
        private readonly Dictionary<string, RemoteHero> heroes = [];
        private readonly Dictionary<string, HeroSpawn> spawnCache = [];
        public IEnumerable<RemoteHero> All => heroes.Values;

        private string localLevelId => remoteWorld.LocalHero.Context.CurrentLevelId;
        private int localSubLevel => remoteWorld.LocalHero.Context.SubLevelIndex;

        public RemoteHeroManager(GuestClientSession session, RemoteWorld remoteWorld)
        {
            this.session = session;
            this.remoteWorld = remoteWorld;
        }

        public void Spawn(HeroSpawn spawn)
        {
            spawnCache[spawn.GUID] = spawn;

            if (!IsLocalLevel(spawn.LevelId, spawn.SubLevelId)) return;

            if (heroes.TryGetValue(spawn.GUID, out var existing))
            {
                existing.ApplySpawn(spawn);
                return;
            }

            CreateHero(spawn);
        }

        public void Update(HeroDynamic dyn, double serverTimeMs)
        {
            if (!heroes.TryGetValue(dyn.GUID, out var hero))
            {
                if (!CanRebuild(dyn)) return;

                // 从缓存重建
                var spawn = spawnCache[dyn.GUID];
                CreateHero(spawn);
                hero = heroes[dyn.GUID];
            }

            if (dyn.HasLevel)
            {
                hero.LevelId = dyn.LevelId;
                hero.SubLevelIndex = dyn.SubLevelIndex;

                if (!IsLocalLevel(hero))
                {
                    Despawn(dyn.GUID);
                    return;
                }
            }

            hero.PushState(dyn, serverTimeMs);
        }

        public void Despawn(string guid)
        {
            if (heroes.Remove(guid, out var hero))
                hero?.destroy();
        }

        public bool TryGet(string guid, out RemoteHero hero)
            => heroes.TryGetValue(guid, out hero!);

        protected override void MyDispose()
        {
            foreach (var h in heroes.Values)
                h?.destroy();
            heroes.Clear();
        }

        //遍历所有 hero，销毁 level 不匹配的
        public void RefreshLevel()
        {
            var toRemove = new List<string>();
            foreach (var (guid, hero) in heroes)
            {
                if (!IsLocalLevel(hero))
                    toRemove.Add(guid);
            }
            foreach (var guid in toRemove)
                Despawn(guid);
        }

        private void CreateHero(HeroSpawn spawn)
        {
            var hero = new RemoteHero(session, session.Game.curLevel);
            hero.init();
            hero.ApplySpawn(spawn);
            heroes[spawn.GUID] = hero;
        }

        private bool CanRebuild(HeroDynamic dyn)
        {
            if (!spawnCache.ContainsKey(dyn.GUID)) return false;
            if (!dyn.HasLevel) return false;
            return IsLocalLevel(dyn.LevelId, dyn.SubLevelIndex);
        }

        private bool IsLocalLevel(RemoteHero hero)
            => hero.LevelId == localLevelId && hero.SubLevelIndex == localSubLevel;

        private bool IsLocalLevel(string levelId, int subLevel)
            => levelId == localLevelId && subLevel == localSubLevel;

        void IOnLocalHeroInitDone.OnHeroInitDone(Hero hero)
        {
            RefreshLevel();
        }
    }
}