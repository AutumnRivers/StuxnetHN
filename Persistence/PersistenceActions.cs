using Hacknet;
using Pathfinder.Action;
using Pathfinder.Meta.Load;
using Pathfinder.Util;
using Stuxnet_HN.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stuxnet_HN.Persistence
{
    public class PersistenceActions
    {
        public class AddPersistentFlag : DelayablePathfinderAction
        {
            [XMLStorage]
            public string Flag;

            public override void Trigger(OS os)
            {
                PersistenceManager.AddFlag(Flag);
            }
        }

        public class RemovePersistentFlag : DelayablePathfinderAction
        {
            [XMLStorage]
            public string Flag;

            public override void Trigger(OS os)
            {
                PersistenceManager.TakeFlag(Flag);
            }
        }

        public class ClearPersistentFlags : DelayablePathfinderAction
        {
            public override void Trigger(OS os)
            {
                PersistenceManager.ResetFlags();
            }
        }

        public static void RegisterActions()
        {
            ActionManager.RegisterAction<AddPersistentFlag>("AddPersistentFlag");
            ActionManager.RegisterAction<RemovePersistentFlag>("RemovePersistentFlag");
            ActionManager.RegisterAction<ClearPersistentFlags>("ClearPersistentFlags");
            ConditionManager.RegisterCondition<HasPersistentFlags>("HasPersistentFlags");
            ConditionManager.RegisterCondition<DoesNotHavePersistentFlags>("DoesNotHavePersistentFlags");
        }

        public class HasPersistentFlags : StuxnetConditional
        {
            [XMLStorage]
            public string Flags;

            public override bool Check(object os_obj)
            {
                var flags = Flags.Split(',');
                return CheckOnceAndDiscard(PersistenceManager.HasGlobalFlags(flags));
            }
        }

        public class DoesNotHavePersistentFlags : StuxnetConditional
        {
            [XMLStorage]
            public string Flags;

            public override bool Check(object os_obj)
            {
                var flags = Flags.Split(',');
                return CheckOnceAndDiscard(!PersistenceManager.HasGlobalFlags(flags));
            }
        }
    }
}
