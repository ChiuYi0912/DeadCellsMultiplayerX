using System.Collections.Generic;
using dc.en;
using DeadCellsMultiplayerX.Client.Guest.WorldX.Remote;
using DeadCellsMultiplayerX.Client.Guest.WorldX.LocalHero;
using DeadCellsMultiplayerX.Common;
using DeadCellsMultiplayerX.Common.Data.Snapshot;

namespace DeadCellsMultiplayerX.Client.Guest.WorldX
{
    /// <summary>
    /// 客户端世界层。
    /// 收到 WorldSnapshot → 分发给各 Manager。
    /// </summary>
    internal class RemoteWorld : DisposableEventReceiver
    {
        private readonly RemoteHeroManager heroes;
        private readonly LocalHeroState localHero;
        private readonly string localGuid;

        public RemoteWorld(GuestClientSession session, string localGuid)
        {
            this.localGuid = localGuid;
            heroes = new RemoteHeroManager(session,this);
            localHero = new LocalHeroState(session, this);
        }

        public LocalHeroState LocalHero => localHero;
        public RemoteHeroManager Heros => heroes;
        public IEnumerable<RemoteHero> AllHeroes => heroes.All;

        public bool TryGetHero(string guid, out RemoteHero hero)
            => heroes.TryGet(guid, out hero);

        public void OnSnapshot(WorldSnapshot snap)
        {
            //处理需要销毁的remotehero
            foreach (var d in snap.Updated)
            {
                if (d.GUID == localGuid) continue;
                if (d.NeedDespawn || d.NeedRespawn)
                    heroes.Despawn(d.GUID);
            }

            //生成
            foreach (var s in snap.Spawned)
            {
                if (s.GUID == localGuid) continue;
                heroes.Spawn(s);
            }

            //更新
            foreach (var d in snap.Updated)
            {
                if (d.GUID == localGuid) continue;
                if (d.NeedDespawn || d.NeedRespawn) continue;
                heroes.Update(d, snap.ServerTime);
            }

            //销毁
            foreach (var g in snap.Despawned)
            {
                if (g == localGuid) continue;
                heroes.Despawn(g);
            }
        }

        protected override void MyDispose()
        {
            heroes.Dispose();
            base.MyDispose();
        }
    }
}