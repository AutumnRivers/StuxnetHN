using Hacknet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stuxnet_HN.Commands
{
    public class ToggleScanLinesFix
    {
        public static void ToggleFix(OS os, string[] args)
        {
            StuxnetCore.useScanLinesFix = !StuxnetCore.useScanLinesFix;
        }
    }
}
