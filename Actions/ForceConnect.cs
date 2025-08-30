using System;
using System.Timers;

using Hacknet;

using Pathfinder.Action;
using Pathfinder.Meta.Load;
using Pathfinder.Util;

namespace Stuxnet_HN.Actions
{
    [Action("ForceConnectPlayer")]
    public class ForceConnect : DelayablePathfinderAction
    {
        [XMLStorage]
        public string TargetCompID;

        [XMLStorage]
        public string LikeReallyForce = "false";

        [XMLStorage]
        public string Intense = "false";

        private static Timer forceTimer;

        private int loopCounter = 0;

        public override void Trigger(OS os)
        {
            Computer currentlyConnectedComputer = os.connectedComp;
            Computer targetComputer = ComputerLookup.FindById(TargetCompID);

            if(targetComputer == null)
            {
                throw new NullReferenceException($"Target computer with ID {TargetCompID} does not exist.");
            }

            bool reallyForce = LikeReallyForce == "true";

            if(reallyForce)
            {
                forceTimer = new Timer(2000);
                forceTimer.Elapsed += ForceConnectToComp;
                forceTimer.AutoReset = true;
                forceTimer.Enabled = true;

                forceTimer.Start();
            } else
            {
                Programs.connect(new string[2] { "connect", targetComputer.ip }, os);
            }
        }

        private void ForceConnectToComp(object source, ElapsedEventArgs e)
        {
            loopCounter++;

            Computer currentlyConnectedComputer = OS.currentInstance.connectedComp;
            Computer targetComputer = ComputerLookup.FindById(TargetCompID);

            Programs.connect(new string[2] { "connect", targetComputer.ip }, OS.currentInstance);

            if (currentlyConnectedComputer != targetComputer || loopCounter < 3) { return; }

            forceTimer.Stop();
            forceTimer.Dispose();
        }
    }
}
