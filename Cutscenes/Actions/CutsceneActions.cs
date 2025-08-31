using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet.Extensions;

using Pathfinder.Action;
using Pathfinder.Meta.Load;
using Pathfinder.Util;

namespace Stuxnet_HN.Cutscenes.Actions
{
    public class RegisterCutscene : PathfinderAction
    {
        [XMLStorage]
        public string FilePath;

        public override void Trigger(object os_obj)
        {
            string path = ExtensionLoader.ActiveExtensionInfo.FolderPath +
                "/" + FilePath;

            StuxnetCutscene cutscene = StuxnetCutsceneRegister.ReadFromFile(path);
            cutscene.filepath = path;

            if(!StuxnetCore.cutscenes.ContainsKey(cutscene.id))
            {
                StuxnetCore.cutscenes.Add(cutscene.id, cutscene);
            }
        }
    }

    public class TriggerCutscene : PathfinderAction
    {
        [XMLStorage]
        public string CutsceneID;

        public override void Trigger(object os_obj)
        {
            StuxnetCutscene cutscene;

            if(StuxnetCore.cutscenes.ContainsKey(CutsceneID))
            {
                cutscene = StuxnetCore.cutscenes[CutsceneID];
            } else
            {
                throw new KeyNotFoundException($"Cutscene ID '{CutsceneID}' could not be found - did you register it?");
            }

            if(StuxnetCore.cutsceneIsActive == true || StuxnetCore.activeCutsceneID != "NONE")
            {
                Console.Error.WriteLine(StuxnetCore.logPrefix + " There's already an active cutscene. " +
                    "Please refrain from triggering another cutscene until the current one finishes. Skipping...");
                return;
            }

            StuxnetCore.activeCutsceneID = cutscene.id;
            StuxnetCore.cutsceneIsActive = true;
        }
    }

    public class TriggerInstruction : PathfinderAction
    {
        readonly StuxnetCutsceneInstruction inst;

        public TriggerInstruction(StuxnetCutsceneInstruction instruction)
        {
            inst = instruction;
        }

        public override void Trigger(object os_obj)
        {
            if (!StuxnetCore.cutsceneIsActive || StuxnetCore.activeCutsceneID == "NONE")
            {
                Console.WriteLine(StuxnetCore.logPrefix + "WARN:Tried to run a cutscene action, but there isn't an active cutscene.");
                return;
            }

            inst.Execute();
        }
    }
}
