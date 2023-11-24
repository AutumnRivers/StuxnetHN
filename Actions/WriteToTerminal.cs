using Hacknet;

using Pathfinder.Action;
using Pathfinder.Util;

namespace Stuxnet_HN.Actions
{
    class WriteToTerminal : DelayablePathfinderAction
    {
        [XMLStorage]
        public string Quietly = "true";

        [XMLStorage(IsContent = true)]
        public string message = "";

        public override void Trigger(OS os)
        {
            bool isQuiet = (Quietly == "true");

            if (!isQuiet) {
                os.warningFlash();
                os.beepSound.Play();
            };

            os.terminal.writeLine(" ");
            os.terminal.writeLine(message);
            os.terminal.writeLine(" ");
        }
    }
}