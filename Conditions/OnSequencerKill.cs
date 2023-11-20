using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet;

using Pathfinder.Action;

namespace Stuxnet_HN.Conditions
{
    public class OnSequencerKill : PathfinderCondition
    {
        public override bool Check(object os_obj)
        {
            OS os = (OS)os_obj;

            return !os.exes.OfType<ExtensionSequencerExe>().Any();
        }
    }
}
