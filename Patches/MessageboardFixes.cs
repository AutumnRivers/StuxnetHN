using System;
using System.Linq;
using Hacknet;
using HarmonyLib;

namespace Stuxnet_HN.Patches
{
    [HarmonyPatch]
    public class MessageboardFixes
    {
        // This fix guarantees every thread file will have a unique ID
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MessageBoardDaemon),"ParseThread")]
        public static void ReplaceThreadIDs(MessageBoardDaemon __instance, string threadData, ref MessageBoardThread __result)
        {
            if (!StuxnetCore.Configuration.EnableMessageBoardFix) return;

            var threads = __instance.threadsFolder.files;
            var threadFile = threads.FirstOrDefault(tf => tf.data == threadData);
            if(threadFile == null)
            {
                __result.id = DateTime.Now.ToUniversalTime().Millisecond.ToString();
                return;
            }

            var threadID = threadFile.name.Split('.')[0];
            __result.id = threadID;
        }
    }
}
