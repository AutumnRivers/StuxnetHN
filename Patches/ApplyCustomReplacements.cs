using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet;

using HarmonyLib;
using Pathfinder.Util;

namespace Stuxnet_HN.Patches
{
    [HarmonyPatch]
    public class ApplyCustomReplacements
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ComputerLoader), "filter")]
        static void Postfix(ref string s, ref string __result)
        {
            string text = s;

            foreach(var replacement in StuxnetCore.customReplacements)
            {
                text = s.Replace(replacement.Key, replacement.Value);
            }

            __result = text;
        }
    }

    [HarmonyPatch]
    public class ReloadComputerFiles
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Computer), "connect")]
        static void Postfix(Computer __instance)
        {
            foreach(Folder folder in __instance.files.root.folders)
            {
                CheckFolders(folder);
            }

            foreach (FileEntry file in __instance.files.root.files)
            {
                if (file.name.EndsWith(".exe")) { continue; }
                foreach (var replacement in StuxnetCore.customReplacements)
                {
                    file.data = file.data.Replace(replacement.Key, replacement.Value);
                }
            }

            void CheckFolders(Folder targetFolder)
            {
                foreach(Folder folder in targetFolder.folders)
                {
                    CheckFolders(folder);
                }

                foreach(FileEntry file in targetFolder.files)
                {
                    if(file.name.EndsWith(".exe")) { continue; }
                    foreach(var replacement in StuxnetCore.customReplacements)
                    {
                        file.data = file.data.Replace(replacement.Key, replacement.Value);
                    }
                }
            }
        }
    }
}
