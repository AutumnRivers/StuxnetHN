using Hacknet;

using Pathfinder.Action;
using Pathfinder.Meta.Load;

namespace Stuxnet_HN.Actions
{
    [Action("DisableAlertsIcon")]
    public class DisableAlertsIcon : DelayablePathfinderAction
    {
        public override void Trigger(OS os)
        {
            os.DisableEmailIcon = true;
            StuxnetCore.disableAlerts = true;
        }
    }

    [Action("EnableAlertsIcon")]
    public class EnableAlertsIcon : DelayablePathfinderAction
    {
        public override void Trigger(OS os)
        {
            os.DisableEmailIcon = false;
            StuxnetCore.disableAlerts = false;
        }
    }
}
