using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet;

using HarmonyLib;

using Microsoft.Xna.Framework;

namespace Stuxnet_HN.Patches
{
    [HarmonyPatch]
    public class ScanlinesColorPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(OS), "drawScanlines")]
        public static void Postfix_ChangeScanlinesTexture(OS __instance)
        {
            if(StuxnetCore.useScanLinesFix)
            {
                __instance.scanLines = StuxnetCore.texCache["ScanLinesFix"];
            } else
            {
                if(StuxnetCore.originalScanlines == null) { return; }
                __instance.scanLines = StuxnetCore.originalScanlines;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ThemeManager), "switchThemeColors")]
        public static void Postfix_DontBlindPlayer(OS os)
        {
            if(os.scanlinesColor.R >= 255 &&
                os.scanlinesColor.G >= 255 &&
                os.scanlinesColor.B >= 255)
            {
                os.scanlinesColor = new Color(0, 0, 0, os.scanlinesColor.A);
            }
        }
    }
}
