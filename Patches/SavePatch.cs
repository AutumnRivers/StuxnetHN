using Hacknet;
using HarmonyLib;

namespace Stuxnet_HN.Patches
{
    [HarmonyPatch]
    public class SavePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(OS), "threadedSaveExecute")]
        public static bool BlockSavesPatch()
        {
            if(StuxnetCore.CanSave && StuxnetCore.Configuration.ShowDebugText)
            {
                StuxnetCore.Logger.LogDebug("Blocking save via BlockSavesPatch");
            }
            return StuxnetCore.CanSave;
        }
    }
}
