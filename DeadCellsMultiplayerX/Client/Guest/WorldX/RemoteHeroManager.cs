using System.Collections.Generic;
using DeadCellsMultiplayerX.Client.Guest.WorldX.Entitys;
using DeadCellsMultiplayerX.Common;
using DeadCellsMultiplayerX.Common.Data.Snapshot;

namespace DeadCellsMultiplayerX.Client.Guest.WorldX
{
    /// <summary>
    /// 管理所有远端玩家。
    /// 由 <see cref="RemoteWorld"/> 分发 WorldSnapshot 驱动。
    /// </summary>
    internal class RemoteHeroManager : Disposable
    {
        private readonly GuestClientSession session;
        private readonly Dictionary<string, RemoteHero> heroes = [];
        public IEnumerable<RemoteHero> All => heroes.Values;

        public RemoteHeroManager(GuestClientSession session)
        {
            this.session = session;
        }

        public void Spawn(HeroSpawn spawn)
        {
            if (heroes.TryGetValue(spawn.GUID, out var existing))
            {
                existing.ApplySpawn(spawn);
                return;
            }

            var hero = new RemoteHero(session, session.Game.curLevel, 0, 0);
            hero.init();
            hero.ApplySpawn(spawn);
            heroes[spawn.GUID] = hero;
        }

        public void Update(HeroDynamic dyn, double serverTimeMs)
        {
            if (heroes.TryGetValue(dyn.GUID, out var hero))
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
    }
}