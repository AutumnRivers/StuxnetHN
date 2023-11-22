using HarmonyLib;

using Hacknet;
using Hacknet.Extensions;

using Stuxnet_HN.Actions;

namespace Stuxnet_HN.Patches
{
    [HarmonyPatch]
    public class ExtSequencerPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ExtensionSequencerExe))]
        [HarmonyPatch(nameof(ExtensionSequencerExe.Update))]
        static bool Prefix(ExtensionSequencerExe __instance)
        {
            SequencerInfo customSequencerInfo = StuxnetCore.currentSequencerInfo;

            if(customSequencerInfo == null) { return true; }

            __instance.beatDropTime = customSequencerInfo.spinUpTime;
            __instance.targetID = customSequencerInfo.targetIDorIP;
            __instance.flagForProgressionName = customSequencerInfo.requiredFlag;

            ExtensionLoader.ActiveExtensionInfo.SequencerSpinUpTime = customSequencerInfo.spinUpTime;
            ExtensionLoader.ActiveExtensionInfo.ActionsToRunOnSequencerStart = customSequencerInfo.sequencerActions;

            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ExtensionSequencerExe), "Killed")]
        static void Postfix(ExtensionSequencerExe __instance)
        {
            // This fixes a bug where nodes would continue to be dimmed after the sequencer is killed
            __instance.os.netMap.DimNonConnectedNodes = false;
        }
    };
}
