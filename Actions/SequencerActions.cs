using System.Collections.Generic;
using System.IO;

using Hacknet;
using Hacknet.Extensions;
using Pathfinder.Action;
using Pathfinder.Util;

using Newtonsoft.Json;

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

            ExtensionInfo activeInfo = ExtensionLoader.ActiveExtensionInfo;

            public override void Trigger(object os_obj)
            {
                SequencerInfo seqInfo = new SequencerInfo
                {
                    requiredFlag = RequiredFlag,
                    targetIDorIP = TargetID,
                    spinUpTime = float.Parse(SpinUpTime),
                    sequencerActions = ActionsToRun
                };

                /*activeInfo.SequencerFlagRequiredForStart = seqInfo.requiredFlag;
                activeInfo.SequencerTargetID = seqInfo.targetIDorIP;
                activeInfo.SequencerSpinUpTime = seqInfo.spinUpTime;
                activeInfo.ActionsToRunOnSequencerStart = seqInfo.sequencerActions;*/

                StuxnetCore.currentSequencerInfo = seqInfo;
            }
        }

        public class ChangeSequencerFromID : PathfinderAction
        {
            [XMLStorage]
            public string SequencerID;

            private const string SequencerFileName = "/sequencers.json";

            ExtensionInfo activeInfo = ExtensionLoader.ActiveExtensionInfo;

            public override void Trigger(object os_obj)
            {
                SequencerInfo seqInfo = new SequencerInfo
                {
                    requiredFlag = activeInfo.SequencerFlagRequiredForStart,
                    targetIDorIP = activeInfo.SequencerTargetID,
                    spinUpTime = activeInfo.SequencerSpinUpTime,
                    sequencerActions = activeInfo.ActionsToRunOnSequencerStart
                };

                if(!File.Exists(activeInfo.FolderPath + SequencerFileName))
                {
                    throw new FileNotFoundException(SequencerFileName + " could not be found in the root folder of the extension.");
                }

                StreamReader sequencerFileStream = new StreamReader(activeInfo.FolderPath + SequencerFileName);
                string seqFileJSON = sequencerFileStream.ReadToEnd();

                Dictionary<string, SequencerInfo> sequencersJSON = JsonConvert.DeserializeObject<Dictionary<string, SequencerInfo>>(seqFileJSON);

                sequencerFileStream.Close();

                if (!sequencersJSON.ContainsKey(SequencerID))
                {
                    throw new KeyNotFoundException($"Could not find the Sequencer ID {SequencerID} in {SequencerFileName}.");
                }

                // Now we can actually use the sequencer
                SequencerInfo targetSeq = sequencersJSON[SequencerID];

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

    public class SequencerInfo
    {
        public string requiredFlag;
        public float spinUpTime;
        public string targetIDorIP;
        public string sequencerActions;
    }
}
