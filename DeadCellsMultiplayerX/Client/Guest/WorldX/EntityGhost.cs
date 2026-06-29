using dc;
using dc.pr;
using DeadCellsMultiplayerX.Common.Data;
using Hashlink.Virtuals;
using ModCore.Utilities;

namespace DeadCellsMultiplayerX.Client.Guest.WorldX
{
    public class EntityGhost : Ghost
    {
        public EntityGhost(Level lvl, string guid) : base(lvl, guid) { }

        public override void SetVisible(bool visible) => this.visible = visible;

        public override void Dispose()
        {
            spr?.remove();
            destroy();
        }

        protected override void OnApplyUpdate(EntityInfo info, bool firstTime)
        {

        }
    }
}
