using System;
using Hacknet;
using Pathfinder.Action;
using Pathfinder.Util;

namespace Stuxnet_HN.Actions
{
    public class DebugActions
    {
        public static void RegisterActions()
        {
            ActionManager.RegisterAction<SALogMessage>("LogMessage");
        }

        public class SALogMessage : DelayablePathfinderAction
        {
            [XMLStorage(IsContent = true)]
            public string Content;

            [XMLStorage]
            public string LogType = "debug";

            private Action<string> Log;

            public override void Trigger(OS os)
            {
                Content = ComputerLoader.filter(Content);
                switch(LogType.ToLower())
                {
                    case "debug":
                    default:
                        Log = StuxnetCore.Logger.LogDebug;
                        Content = "DEBUG: " + Content;
                        break;
                    case "warning":
                        Log = StuxnetCore.Logger.LogWarning;
                        Content = "WARN: " + Content;
                        break;
                    case "error":
                        Log = StuxnetCore.Logger.LogError;
                        Content = "ERROR: " + Content;
                        break;
                }
                if(OS.DEBUG_COMMANDS && StuxnetCore.Configuration.ShowDebugText)
                {
                    os.write(Content);
                }
                Content = "SALogMessage> " + Content;
                Log(Content);
            }
        }
    }
}
