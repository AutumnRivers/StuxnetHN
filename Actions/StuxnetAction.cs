using Hacknet;
using Pathfinder.Action;
using Pathfinder.Util;
using System;

namespace Stuxnet_HN.Actions
{
    public class StuxnetConditional : PathfinderCondition
    {
        [XMLStorage]
        public bool CheckOnce = false;

        public override bool Check(object os_obj)
        {
            throw new NotImplementedException();
        }

        public virtual bool CheckOnceAndDiscard(bool toCheck)
        {
            if(!toCheck && CheckOnce)
            {
                OS.currentInstance.delayer.Post(ActionDelayer.NextTick(), () =>
                {
                    OS.currentInstance.ConditionalActions.Actions.RemoveAll(ca => ca.Condition == this);
                });
            }

            return toCheck;
        }
    }
}
