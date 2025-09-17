using Hacknet;
using HarmonyLib;
using Pathfinder.Event.Loading;
using Pathfinder.Meta.Load;
using System;
using System.Linq;

namespace Stuxnet_HN.Patches
{
    [HarmonyPatch]
    public class ApplyCustomReplacements
    {
        [Event()]
        public static void ApplyReplacements(TextReplaceEvent textReplaceEvent)
        {
            Console.WriteLine(textReplaceEvent.Replacement);
            StuxnetCore.customReplacements.Aggregate(textReplaceEvent.Replacement,
                (current, value) => current.Replace(value.Key, value.Value));
            Console.WriteLine(textReplaceEvent.Replacement);
        }

        public static string CustomFilter(string s)
        {
            foreach (var replacement in StuxnetCore.customReplacements)
            {
                s = s.Replace(replacement.Key, replacement.Value);
            }

            return s;
        }
    }

    [HarmonyPatch]
    public class ReloadComputerFiles
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Computer), "connect")]
        public static void Postfix(Computer __instance)
        {
            foreach (Folder folder in __instance.files.root.folders)
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

            if (__instance.Memory != null) refilterMemory();

            void CheckFolders(Folder targetFolder)
            {
                foreach (Folder folder in targetFolder.folders)
                {
                    CheckFolders(folder);
                }

                foreach (FileEntry file in targetFolder.files)
                {
                    if (file.name.EndsWith(".exe")) { continue; }
                    foreach (var replacement in StuxnetCore.customReplacements)
                    {
                        file.data = file.data.Replace(replacement.Key, replacement.Value);
                    }
                }
            }

            void refilterMemory()
            {
                var memory = __instance.Memory;
                if (memory.CommandsRun.Any())
                {
                    for (int cmdIdx = 0; cmdIdx < memory.CommandsRun.Count; cmdIdx++)
                    {
                        var cmd = memory.CommandsRun[cmdIdx];
                        cmd = ApplyCustomReplacements.CustomFilter(cmd);
                        memory.CommandsRun[cmdIdx] = cmd;
                    }
                }

                if (memory.DataBlocks.Any())
                {
                    for (int blockIdx = 0; blockIdx < memory.DataBlocks.Count; blockIdx++)
                    {
                        var block = memory.DataBlocks[blockIdx];
                        block = ApplyCustomReplacements.CustomFilter(block);
                        memory.DataBlocks[blockIdx] = block;
                    }
                }
            }
        }
    }
}
