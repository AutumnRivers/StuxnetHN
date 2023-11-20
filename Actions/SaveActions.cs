using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using Hacknet;
using Hacknet.Extensions;

using Pathfinder.Action;
using Pathfinder.Util;

namespace Stuxnet_HN.Actions
{
    public class SaveActions
    {
        public class DenySaves : DelayablePathfinderAction
        {
            public override void Trigger(OS os)
            {
                StuxnetCore.saveFlag = "ignore_this_flag_and_disable_saves";
                ExtensionLoader.ActiveExtensionInfo.AllowSave = false;
            }
        }

        public class AllowSaves : DelayablePathfinderAction
        {
            public override void Trigger(OS os)
            {
                StuxnetCore.saveFlag = null;
                ExtensionLoader.ActiveExtensionInfo.AllowSave = true;
            }
        }

        public class RequireFlagForSaves : DelayablePathfinderAction
        {
            [XMLStorage]
            public string Flag;

            [XMLStorage]
            public string AvoidFlag;

            public override void Trigger(OS os)
            {
                if(Flag.IsNullOrWhiteSpace()) {
                    StuxnetCore.saveFlag = null;
                    return;
                }

                bool avoidFlag = AvoidFlag == "true";

                if(avoidFlag) { Flag = "!" + Flag; }

                StuxnetCore.saveFlag = Flag;
            }
        }
    }
}
