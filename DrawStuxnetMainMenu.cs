using BepInEx.Hacknet;
using Pathfinder.Event.Menu;
using Pathfinder.Meta.Load;
using System.Linq;

namespace Stuxnet_HN
{
    public class DrawStuxnetMainMenu
    {
        public const string SN_AUDIO_GUID = StuxnetCore.ModGUID + ".audio";

        [Event()]
        public static void DrawStuxnetTextOnMainMenu(DrawMainMenuTitlesEvent titlesEvent)
        {
            string title = "Stuxnet";
            if(HacknetChainloader.Instance.Plugins.Any(p => p.Key == SN_AUDIO_GUID))
            {
                title += "(.Audio)";
            }
            titlesEvent.Sub.Title += "+" + title + " " + StuxnetCore.ModVer;
        }
    }
}
