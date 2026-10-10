using DeadCellsMultiplayerX.Common.Data.Snapshot;
using ModCore.Events;

namespace DeadCellsMultiplayerX.Client.Event
{
    [Event]
    public interface IOnBeforeSendHero
    {
        void OnBeforeSendHero(ref HeroUpload upload);
    }
}