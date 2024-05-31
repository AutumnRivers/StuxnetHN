using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet;

using Pathfinder.Action;
using Pathfinder.Util;

using Stuxnet_HN.Cutscenes.Patches;

namespace Stuxnet_HN.Cutscenes.Actions
{
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
