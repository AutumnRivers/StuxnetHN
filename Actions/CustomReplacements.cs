using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet;

using Pathfinder.Action;
using Pathfinder.Util;

namespace Stuxnet_HN.Actions
{
    public class AddCustomReplacements : PathfinderAction
    {
        [XMLStorage]
        public string Name;

        [XMLStorage]
        public string Value;

        public override void Trigger(object os_obj)
        {
            StuxnetCore.UpdateCustomReplacement(Name, Value);
        }
    }

    public class AddNodeCustomReplacement : PathfinderAction
    {
        [XMLStorage]
        public string Name;

        [XMLStorage]
        public string CompID;

        public override void Trigger(object os_obj)
        {
            Computer targetComp = ComputerLookup.FindById(CompID) ?? throw new NullReferenceException($"Computer ID {CompID} Not Found");

            StuxnetCore.UpdateCustomReplacement($"{Name.ToUpper()}_IP", targetComp.ip);
        }
    }

    public class AddAdminPassCustomReplacement : PathfinderAction
    {
        [XMLStorage]
        public string Name;

        [XMLStorage]
        public string CompID;

        public override void Trigger(object os_obj)
        {
            Computer targetComp = ComputerLookup.FindById(CompID) ?? throw new NullReferenceException($"Computer ID {CompID} Not Found");

            StuxnetCore.UpdateCustomReplacement($"{Name.ToUpper()}_PASS", targetComp.adminPass);
        }
    }
}
