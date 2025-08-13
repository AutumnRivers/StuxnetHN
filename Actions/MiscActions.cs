using Hacknet;
using Pathfinder.Action;
using Pathfinder.Meta.Load;
using System;

namespace Stuxnet_HN.Actions
{
    [Action("ForceCloseGame")]
    public class ForceQuitAction : DelayablePathfinderAction
    {
        public override void Trigger(OS os)
        {
            MusicManager.stop();
            Game1.threadsExiting = true;
            Game1.getSingleton().Exit();
        }
    }
}
