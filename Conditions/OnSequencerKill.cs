using System.Linq;

using Hacknet;

using Pathfinder.Action;
using Pathfinder.Meta.Load;

namespace Stuxnet_HN.Conditions
{
    [Condition("OnExtSequencerKill")]
    public class OnSequencerKill : PathfinderCondition
    {
        public override bool Check(object os_obj)
        {
            OS os = (OS)os_obj;

            return !os.exes.OfType<ExtensionSequencerExe>().Any();
        }
    }
}
