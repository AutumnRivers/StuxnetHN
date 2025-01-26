using Hacknet;

using System;

namespace Stuxnet_HN.Commands
{
    [Obsolete("The scanlines fix is deprecated and will be removed in Stuxnet v2.0.0")]
    public class ToggleScanLinesFix
    {
        public static void ToggleFix(OS os, string[] args)
        {
            //StuxnetCore.useScanLinesFix = !StuxnetCore.useScanLinesFix;
            os.terminal.writeLine("WARN: The scanlines fix is deprecated and will be removed in Stuxnet v2.0.0");
            return;
        }
    }
}
