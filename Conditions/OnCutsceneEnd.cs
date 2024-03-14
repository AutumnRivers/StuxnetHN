using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet;

using Pathfinder.Action;

namespace Stuxnet_HN.Conditions
{
    public class OnCutsceneEnd : PathfinderCondition
    {
        bool hasStarted;

        public override bool Check(object os_obj)
        {
            if(StuxnetCore.activeCutsceneID == "NONE" && !hasStarted)
            {
                return false;
            }

            if(StuxnetCore.activeCutsceneID != "NONE" && !hasStarted)
            {
                hasStarted = true;
                return false;
            }

            if(StuxnetCore.activeCutsceneID != "NONE")
            {
                return true;
            }

            return true;
        }
    }
}
