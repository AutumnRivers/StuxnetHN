using Pathfinder.Action;

namespace Stuxnet_HN.Cutscenes.Actions
{
    public class StopCutscene : PathfinderAction
    {
        public override void Trigger(object os_obj)
        {
            if (StuxnetCore.CurrentlyLoadedCutscene == null) return;
            if (!StuxnetCore.CutsceneIsActive) return;

            StuxnetCore.CurrentlyLoadedCutscene.Active = false;
            StuxnetCore.CurrentlyLoadedCutscene = null;
        }
    }
}
