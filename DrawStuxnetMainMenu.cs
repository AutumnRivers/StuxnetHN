using Pathfinder.Event.Menu;
using Pathfinder.Meta.Load;

namespace Stuxnet_HN
{
    public class DrawStuxnetMainMenu
    {
        [Event()]
        public static void DrawStuxnetTextOnMainMenu(DrawMainMenuTitlesEvent titlesEvent)
        {
            titlesEvent.Sub.Title += "+Stuxnet " + StuxnetCore.ModVer;
        }
    }
}
