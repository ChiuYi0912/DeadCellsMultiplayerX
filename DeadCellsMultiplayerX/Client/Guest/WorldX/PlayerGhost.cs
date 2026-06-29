using dc;
using dc.pr;
using DeadCellsMultiplayerX.Common.Data;
using DeadCellsMultiplayerX.Utils;
using Hashlink.Virtuals;
using HaxeProxy.Runtime;
using ModCore.Utilities;

namespace DeadCellsMultiplayerX.Client.Guest.WorldX
{
    public class PlayerGhost : Ghost
    {

        public PlayerGhost(Level lvl, string guid) : base(lvl, guid) { }

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
