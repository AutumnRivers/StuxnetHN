using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet.Extensions;

using Pathfinder.Action;
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

            StuxnetCore.activeCutsceneID = cutscene.id;
            StuxnetCore.cutsceneIsActive = true;
        }
    }

    public class TriggerInstruction : PathfinderAction
    {
        StuxnetCutsceneInstruction inst;

        public TriggerInstruction(StuxnetCutsceneInstruction instruction)
        {
            inst = instruction;
        }

        public override void Trigger(object os_obj)
        {
            inst.Execute();
        }
    }
}
