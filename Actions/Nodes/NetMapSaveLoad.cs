using System.Collections.Generic;
using System.Linq;
using Pathfinder.Action;
using Hacknet;
using Pathfinder.Util;

namespace Stuxnet_HN.Actions.Nodes
{
    public class NetMapCache
    {
        public static List<int> CachedVisibleNodes = null;
        public static NetmapSortingAlgorithm CachedAlgorithm = NetmapSortingAlgorithm.Scatter;
    }

    public class NetMapSaveLoadActions
    {
        public static void RegisterActions()
        {
            ActionManager.RegisterAction<SASaveNetMapState>("SaveNetMap");
            ActionManager.RegisterAction<SALoadNetMapState>("LoadNetMap");
        }

        public class SASaveNetMapState : DelayablePathfinderAction
        {
            [XMLStorage]
            public bool Overwrite = false;

            [XMLStorage]
            public bool AutoClear = false;

            public override void Trigger(OS os)
            {
                NetMapCache.CachedAlgorithm = os.netMap.SortingAlgorithm;

                var visible = os.netMap.visibleNodes;
                if (NetMapCache.CachedVisibleNodes != null && !Overwrite) return;
                NetMapCache.CachedVisibleNodes = visible.ToList();

                if (AutoClear) os.netMap.visibleNodes.Clear();
            }
        }

        public class SALoadNetMapState : DelayablePathfinderAction
        {
            [XMLStorage]
            public bool Append = false;

            public override void Trigger(OS os)
            {
                if(NetMapCache.CachedVisibleNodes == null)
                {
                    StuxnetCore.Logger.LogError(
                        "Attempted to run SALoadNetMapState, but there was no cached netmap state!"
                        );
                    return;
                }

                if(Append)
                {
                    os.netMap.visibleNodes = NetMapCache.CachedVisibleNodes.Union(os.netMap.visibleNodes).ToList();
                } else
                {
                    os.netMap.visibleNodes = NetMapCache.CachedVisibleNodes;
                }
                os.netMap.SortingAlgorithm = NetMapCache.CachedAlgorithm;

                NetMapCache.CachedVisibleNodes = null;
            }
        }
    }
}
