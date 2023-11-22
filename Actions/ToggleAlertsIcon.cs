using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet;

using Pathfinder.Action;

namespace Stuxnet_HN.Actions
{
    public class DisableAlertsIcon : DelayablePathfinderAction
    {
        public override void Trigger(OS os)
        {
            os.DisableEmailIcon = true;
        }
    }

    public class EnableAlertsIcon : DelayablePathfinderAction
    {
        public override void Trigger(OS os)
        {
            os.DisableEmailIcon = false;
        }
    }
}
