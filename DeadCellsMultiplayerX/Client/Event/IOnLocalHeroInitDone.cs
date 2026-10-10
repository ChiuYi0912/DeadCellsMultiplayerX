using dc.en;
using ModCore.Events;

namespace DeadCellsMultiplayerX.Client.Event
{

    [Event]
    public interface IOnLocalHeroInitDone
    {
        void OnHeroInitDone(Hero hero);
    }
}