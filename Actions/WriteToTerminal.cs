using Hacknet;

using Pathfinder.Action;
using Pathfinder.Meta.Load;
using Pathfinder.Util;

using Stuxnet_HN.Patches;

namespace Stuxnet_HN.Actions
{
    [Action("WriteToTerminal")]
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

            string filteredMessage = ComputerLoader.filter(message);
            filteredMessage = ApplyCustomReplacements.CustomFilter(filteredMessage);

            os.terminal.writeLine(" ");
            os.terminal.writeLine(filteredMessage);
            os.terminal.writeLine(" ");
        }
    }
}