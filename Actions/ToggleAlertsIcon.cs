using Hacknet;

using Pathfinder.Action;

namespace Stuxnet_HN.Actions
{
    public class DisableAlertsIcon : DelayablePathfinderAction
    {
        public override void Trigger(OS os)
        {
            os.DisableEmailIcon = true;
            StuxnetCore.disableAlerts = true;
        }
    }

    public class EnableAlertsIcon : DelayablePathfinderAction
    {
        public override void Trigger(OS os)
        {
            os.DisableEmailIcon = false;
            StuxnetCore.disableAlerts = false;
        }
    }
}
