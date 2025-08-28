using Hacknet;
using HarmonyLib;

namespace Stuxnet_HN.SMS
{
    [HarmonyPatch]
    public class SMSSetLockPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SASetLock), "Trigger")]
        // This doesn't need to return a bool, because Hacknet doesn't throw an error when an invalid
        // module is set for the Module attribute. Yay...?
        public static void AddSMSToSetLock(SASetLock __instance)
        {
            if (__instance.Module.ToLowerInvariant() != "sms") return;

            var module = SMSModule.GlobalInstance;

            module.InputLocked = __instance.IsLocked;
            module.visible = !__instance.IsHidden;
        }
    }
}
