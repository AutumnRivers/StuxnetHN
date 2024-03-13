using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet;
using Hacknet.Extensions;

using Stuxnet_HN.Cutscenes;

namespace Stuxnet_HN.Commands
{
    public class CutsceneDebugCommands
    {
        public static void LogCutsceneData(OS os, string[] args)
        {
            if(!OS.DEBUG_COMMANDS) { return; }

            if(args.Length < 2)
            {
                os.terminal.writeLine("ERROR: Need filepath");
                return;
            }

            string path = args[1];
            string extFolderPath = ExtensionLoader.ActiveExtensionInfo.FolderPath;

            StuxnetCutscene cs = StuxnetCutsceneRegister.ReadFromFile(extFolderPath + "/" + path);

            Console.WriteLine(cs.ToString());
            os.terminal.writeLine(cs.ToString());

            os.terminal.writeLine("Start of cutscene data.");

            foreach (var inst in cs.instructions)
            {
                Console.WriteLine(inst.ToString());
                os.terminal.writeLine(inst.ToString());
            }

            os.terminal.writeLine("End of cutscene data.");
        }
    }
}
