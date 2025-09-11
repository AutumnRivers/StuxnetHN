using Pathfinder.Action;
using Pathfinder.Meta.Load;

namespace Stuxnet_HN.Conditions
{
    [Condition("OnCutsceneEnd")]
    public class OnCutsceneEnd : PathfinderCondition
    {
        bool hasStarted;

        public override bool Check(object os_obj)
        {
            if(StuxnetCore.CurrentlyLoadedCutscene == null && !hasStarted)
            {
                return false;
            }

            if(StuxnetCore.CutsceneIsActive && !hasStarted)
            {
                hasStarted = true;
                return false;
            }

            if (StuxnetCore.CurrentlyLoadedCutscene != null) return false;

            if(StuxnetCore.CurrentlyLoadedCutscene == null && hasStarted)
            {
                return true;
            }

            StuxnetCore.Logger.LogWarning("Undefined action in OnCutsceneEnd conditional");
            return true;
        }
    }
}
