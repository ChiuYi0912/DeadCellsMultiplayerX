using dc.en;
using dc.pr;
using DeadCellsMultiplayerX.Client.Event;
using DeadCellsMultiplayerX.Common;
using DeadCellsMultiplayerX.Utils;

namespace DeadCellsMultiplayerX.Client.Guest.WorldX.LocalHero
{
    /// <summary>
    /// 本地 Hero 状态管理器
    /// 追踪本地英雄的状态
    /// </summary>
    internal class LocalHeroState : DisposableEventReceiver,
        IOnLocalHeroInitDone,
        IOnLocalHeroPostUpdate
    {
        private readonly GuestClientSession session;
        private readonly LocalHeroContext ctx = new();
        private readonly RemoteWorld remoteWorld;

        public LocalHeroContext Context => ctx;

        public LocalHeroState(GuestClientSession session, RemoteWorld remoteWorld)
        {
            this.session = session;
            this.remoteWorld = remoteWorld;
        }

        void IOnLocalHeroInitDone.OnHeroInitDone(Hero hero)
        {
            var game = Game.Class.ME;
            if (hero != game?.hero) return;

            ctx.HasHero = true;
            ctx.IsAlive = !hero.destroyed;

            SyncLevelSnapshot(game);
        }

        void IOnLocalHeroPostUpdate.OnPostUpdate(Hero hero)
        {
            var game = Game.Class.ME;
            if (hero != game?.hero) return;

            ctx.HasHero = !hero.destroyed;
            ctx.IsAlive = ctx.HasHero && hero.life > 0;
            ctx.InCinematic = game.hasCinematic();

            UpdateLevel(game);
        }

        private void UpdateLevel(Game game)
        {
            if (game == null)
            {
                ResetLevelInfo();
                return;
            }

            var curLevel = game.curLevel;
            if (curLevel == null)
            {
                return;
            }

            string newLevelId = curLevel.map?.id?.ToString() ?? string.Empty;
            bool inSubLevel = IsInSubLevel(game, curLevel);
            int subLevelIndex = SafeGetSubLevelIndex(curLevel);

            // 计算变化
            bool subLevelChanged = inSubLevel != ctx.InSubLevel;
            bool levelChanged = !string.IsNullOrEmpty(ctx.CurrentLevelId)
                             && newLevelId != ctx.CurrentLevelId;

            // 更新 context
            ctx.CurrentLevelId = newLevelId;
            ctx.SubLevelIndex = subLevelIndex;
            ctx.InSubLevel = inSubLevel;

            // 触发同步标志（互斥，避免同帧多次 Mark）
            if (subLevelChanged)
            {
                if (inSubLevel)
                    session.MarkHeroDespawn();
                else
                    session.MarkHeroRespawn();
            }
            else if (levelChanged)
            {
                session.MarkHeroRespawn();
            }
        }

        private void SyncLevelSnapshot(Game game)
        {
            if (game?.curLevel == null) return;

            ctx.CurrentLevelId = game.curLevel.map?.id?.ToString() ?? string.Empty;
            ctx.SubLevelIndex = SafeGetSubLevelIndex(game.curLevel);
            ctx.InSubLevel = IsInSubLevel(game, game.curLevel);
        }

        //子区域判断
        private static bool IsInSubLevel(Game game, Level curLevel)
        {
            var subLevels = game.subLevels;
            if (subLevels == null || subLevels.length == 0)
                return false;

            var mainLevel = subLevels.getDyn(0);
            return mainLevel != null && curLevel != mainLevel;
        }

        private static int SafeGetSubLevelIndex(Level level)
        {
            try { return level.GetSubLevelIndex(); }
            catch { return -1; }
        }

        private void ResetLevelInfo()
        {
            ctx.CurrentLevelId = string.Empty;
            ctx.SubLevelIndex = -1;
            ctx.InSubLevel = false;
        }

        //查询
        public bool HasHero() => ctx.HasHero;
        public bool IsAlive() => ctx.IsAlive;
        public bool IsInSubLevel() => ctx.InSubLevel;
        public bool IsInCinematic() => ctx.InCinematic;

        public bool ShouldBroadcast()
            => ctx.HasHero && ctx.IsAlive && !ctx.InSubLevel;

        protected override void MyDispose()
        {
            base.MyDispose();
        }
    }
}