using System.Collections.Generic;
using System.IO;

using Hacknet;
using Hacknet.Extensions;
using Pathfinder.Action;
using Pathfinder.Util;

using Newtonsoft.Json;
using Pathfinder.Meta.Load;

using Stuxnet_HN.Configuration;

namespace Stuxnet_HN.Actions
{
    public class SequencerActions
    {
        public class ChangeSequencerManually : PathfinderAction
        {
            [XMLStorage]
            public string RequiredFlag = ExtensionLoader.ActiveExtensionInfo.SequencerFlagRequiredForStart;

            [XMLStorage]
            public string TargetID = ExtensionLoader.ActiveExtensionInfo.SequencerTargetID;

            [XMLStorage]
            public string SpinUpTime = ExtensionLoader.ActiveExtensionInfo.SequencerSpinUpTime.ToString();

            [XMLStorage]
            public string ActionsToRun = ExtensionLoader.ActiveExtensionInfo.ActionsToRunOnSequencerStart;

            public override void Trigger(object os_obj)
            {
                SequencerInfo seqInfo = new SequencerInfo
                {
                    requiredFlag = RequiredFlag,
                    targetIDorIP = TargetID,
                    spinUpTime = float.Parse(SpinUpTime),
                    sequencerActions = ActionsToRun
                };

                StuxnetCore.currentSequencerInfo = seqInfo;
            }
        }

        public class ChangeSequencerFromID : PathfinderAction
        {
            [XMLStorage]
            public string SequencerID;

            private readonly ExtensionInfo activeInfo = ExtensionLoader.ActiveExtensionInfo;

            public override void Trigger(object os_obj)
            {
                SequencerInfo seqInfo = new()
                {
                    requiredFlag = activeInfo.SequencerFlagRequiredForStart,
                    targetIDorIP = activeInfo.SequencerTargetID,
                    spinUpTime = activeInfo.SequencerSpinUpTime,
                    sequencerActions = activeInfo.ActionsToRunOnSequencerStart
                };

                var sequencers = StuxnetCore.Configuration.Sequencers;

                if (!sequencers.ContainsKey(SequencerID))
                {
                    throw new KeyNotFoundException(
                        string.Format("Could not find sequencer ID '{0}' in Stuxnet configuration file.",
                        SequencerID)
                        );
                }

                // Now we can actually use the sequencer
                SequencerInfo targetSeq = sequencers[SequencerID];

                if(targetSeq.requiredFlag != null) { seqInfo.requiredFlag = targetSeq.requiredFlag; }
                if(targetSeq.targetIDorIP != null) { seqInfo.targetIDorIP = targetSeq.targetIDorIP; }
                if(targetSeq.sequencerActions != null) { seqInfo.sequencerActions = targetSeq.sequencerActions; }
                if(targetSeq.spinUpTime > 0) { seqInfo.spinUpTime = targetSeq.spinUpTime; }

                StuxnetCore.currentSequencerID = SequencerID;
                StuxnetCore.currentSequencerInfo = seqInfo;
            }
        }

        public class ClearCustomSequencer : DelayablePathfinderAction
        {
            public override void Trigger(OS os)
            {
                StuxnetCore.currentSequencerID = null;
                StuxnetCore.currentSequencerInfo = null;
            }
        }
    }
}
