using dc;
using dc.libs;
using dc.libs.misc;
using dc.pr;
using DeadCellsMultiplayerX.Client.Guest.World;
using DeadCellsMultiplayerX.Common.Data;

namespace DeadCellsMultiplayerX.Client.Guest.WorldX
{
    public abstract class Ghost : Entity, IWorldGhost
    {
        public string GUID { get; }

        public EntityInfo? PrevState { get; private set; }
        public EntityInfo? CurrentState { get; private set; }
        public bool IsFirstUpdate => PrevState == null;

        protected Ghost(Level lvl, string guid) : base(lvl, 0, 0)
        {
            GUID = guid;
            DisableGameplay();
        }

        public override void init()
        {
            const double fps = 60.0;
            this.delayer = new Delayer(fps);
            this.tw = new Tweenie(fps);
            this.createAttackSource();
            this.createAttackTarget();

            //手动执行 spr装填
            //this.initGfx();
            //this.initClonesGfx();
        }

        /// <summary>
        /// 初始化spr后执行
        /// </summary>
        public virtual void EntitySprinitDone()
        {
            if (this._level != null && this._level.minimap != null && !this._level.minimap.destroyed)
                this.minimapTracking();

            this.initDone = true;
            this.isOnScreen = false;
            this.isOutOfGame = true;
            if (!this.isInQuadTree()) return;
            this._level?.qTree.tryInsert(this.cx, this.cy, this);
        }

        public override void initGfx()
        {
            base.initGfx();



            base.initClonesGfx();
        }

        public void ApplyUpdate(EntityInfo incoming)
        {
            bool firstTime = IsFirstUpdate;
            PrevState = CurrentState;
            CurrentState = incoming;


            CurrentState.EntityData.Deserialize(this, typeof(Entity));

            setPosCase(cx, cy, xr, yr);


            OnApplyUpdate(incoming, firstTime);
        }

        protected abstract void OnApplyUpdate(EntityInfo incoming, bool firstTime);

        public abstract void SetVisible(bool visible);
        public abstract void Dispose();

        protected void DisableGameplay()
        {
            set_targetable(false);
            circularRepel = 0;
            hasRepelling = false;
            detectsWater = false;
        }
    }
}
