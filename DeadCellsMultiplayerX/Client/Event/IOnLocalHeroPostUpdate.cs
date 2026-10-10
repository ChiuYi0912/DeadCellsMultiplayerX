
using dc.en;
using ModCore.Events;

namespace DeadCellsMultiplayerX.Client.Event
{
    [Event]
    public interface IOnLocalHeroPostUpdate
    {
        void OnPostUpdate(Hero hero);
    }
}