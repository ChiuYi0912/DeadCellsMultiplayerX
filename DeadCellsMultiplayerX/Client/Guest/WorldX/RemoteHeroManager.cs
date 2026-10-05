using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dc.en;
using dc.pr;
using DeadCellsMultiplayerX.Client.Event;
using DeadCellsMultiplayerX.Client.Guest.WorldX.Entitys;
using DeadCellsMultiplayerX.Common;
using DeadCellsMultiplayerX.Common.Data;
using DeadCellsMultiplayerX.Utils;

namespace DeadCellsMultiplayerX.Client.Guest.WorldX
{
    internal class RemoteHeroManager : Disposable
    {
        private readonly GuestClientSession session;
        private readonly Dictionary<string, RemoteHero> heroes = [];
        public IEnumerable<RemoteHero> All => heroes.Values;

        public RemoteHeroManager(GuestClientSession session)
        {
            this.session = session;
        }

        public void ApplyOrCreate(EntityInfo info)
        {
            if (!heroes.TryGetValue(info.GUID, out var hero))
            {
                hero = CreateRemoteHero(info);
                heroes[info.GUID] = hero;
            }
            hero.ApplyInfoToHero(info);
        }

        public void Remove(string guid)
        {
            if (heroes.Remove(guid, out var hero))
                hero?.destroy();
        }

        public bool TryGet(string guid, out RemoteHero hero) => heroes.TryGetValue(guid, out hero!);


        protected override void MyDispose()
        {
            foreach (var h in heroes.Values)
                h?.destroy();
            heroes.Clear();
        }

        private RemoteHero CreateRemoteHero(EntityInfo info)
        {
            var hero = new RemoteHero(session.Game.curLevel, info.PosVector.CX, info.PosVector.CY);
            hero.init();
            return hero;
        }

    }
}