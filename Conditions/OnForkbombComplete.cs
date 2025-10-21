using Hacknet;
using Pathfinder.Action;
using Pathfinder.Util;
using HarmonyLib;

namespace Stuxnet_HN.Conditions
{
    public class SCOnForkbombComplete : PathfinderCondition
    {
        [XMLStorage]
        public bool PreventCrash = false;

        [XMLStorage]
        public bool DiscardOnKilled = false;

        public static bool PreventForkbombCompletion = false;
        public static bool ForkbombCompleted = false;

        public override bool Check(object os_obj)
        {
            if (!PreventForkbombCompletion) PreventForkbombCompletion = PreventCrash;

            if(ForkbombCompleted)
            {
                ForkbombCompleted = false;
                return true;
            }

            return false;
        }
    }

    [HarmonyPatch]
    public class ForkbombPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ForkBombExe), "Completed")]
        public static bool PreventForkbombCompletion(ForkBombExe __instance)
        {
            SCOnForkbombComplete.ForkbombCompleted = true;

            if (SCOnForkbombComplete.PreventForkbombCompletion)
            {
                __instance.needsRemoval = true;
                SCOnForkbombComplete.PreventForkbombCompletion = false;
                return false;
            }

            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ForkBombExe), "Killed")]
        public static void ClearSCForkbombDetections()
        {
            OS os = OS.currentInstance;

            os.delayer.Post(ActionDelayer.NextTick(), () =>
            {
                os.ConditionalActions.Actions.RemoveAll(ca => ca.Condition is SCOnForkbombComplete complete
                    && complete.DiscardOnKilled);
            });
        }
    }
}
