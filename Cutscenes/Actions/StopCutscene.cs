using System;
using Pathfinder.Action;
using Pathfinder.Meta.Load;
using Stuxnet_HN.Cutscenes.Patches;

namespace Stuxnet_HN.Cutscenes.Actions
{
    [Action("StopActiveCutscene")]
    public class StopCutscene : PathfinderAction
    {
        public override void Trigger(object os_obj)
        {
            if (StuxnetCore.activeCutsceneID == "NONE") return;

            StuxnetCutsceneInstruction resetInst = StuxnetCutsceneInstruction.CreateResetInstruction();
            resetInst.Cutscene = CutsceneExecutor.ActiveCutscene;
            PathfinderAction resetAction = new TriggerInstruction(resetInst);
            resetAction.Trigger(os_obj);
        }
    }
}
