using Hacknet;
using Pathfinder.Action;
using Pathfinder.Util;
using Stuxnet_HN.Actions;
using System;

namespace Stuxnet_HN.Conditions
{
    public class HasFlagsReplacements
    {
        public static void RegisterConditions()
        {
            ConditionManager.RegisterCondition<StxCHasFlags>("HasFlagsStx");
            ConditionManager.RegisterCondition<StxCDoesNotHaveFlags>("DoesNotHaveFlagsStx");
        }

        public static bool HasFlags(string rawList)
        {
            string[] flagsArray = rawList.Split(Utils.commaDelim, StringSplitOptions.RemoveEmptyEntries);
            for(int idx = 0; idx < flagsArray.Length; idx++)
            {
                string flag = flagsArray[idx];
                if (!OS.currentInstance.Flags.HasFlag(flag)) return false;
            }
            return true;
        }

        public class StxCHasFlags : StuxnetConditional
        {
            [XMLStorage]
            public string requiredFlags;

            public override bool Check(object os_obj)
            {
                if (string.IsNullOrWhiteSpace(requiredFlags)) return CheckOnceAndDiscard(false);
                return CheckOnceAndDiscard(HasFlags(requiredFlags));
            }
        }

        public class StxCDoesNotHaveFlags : StuxnetConditional
        {
            [XMLStorage]
            public string Flags;

            public override bool Check(object os_obj)
            {
                if (string.IsNullOrWhiteSpace(Flags)) return CheckOnceAndDiscard(false);
                return CheckOnceAndDiscard(!HasFlags(Flags));
            }
        }
    }
}
