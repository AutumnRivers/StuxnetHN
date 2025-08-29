using Hacknet;
using HarmonyLib;

namespace Stuxnet_HN.SMS
{
    [HarmonyPatch]
    public class SMSSetLockPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SASetLock), "Trigger")]
        public static void AddSMSToSetLock(SASetLock __instance)
        {
            if (__instance.Module.ToLowerInvariant() != "sms") return;

            var module = SMSModule.GlobalInstance;

            module.InputLocked = __instance.IsLocked;
            if(SMSSystem.Active)
            {
                module.visible = !__instance.IsHidden;
            }
        }
    }
}
