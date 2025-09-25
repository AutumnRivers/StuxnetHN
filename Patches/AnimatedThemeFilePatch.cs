using Hacknet;
using HarmonyLib;
using Pathfinder.Event.Saving;
using Pathfinder.Meta.Load;
using System;
using System.Linq;

namespace Stuxnet_HN.Patches
{
    [HarmonyPatch]
    public class AnimatedThemeFilePatch
    {
        public static string[] GetThemeDataFromFileData(string data)
        {
            string[] separated = data.Split(new string[1] { ThemeManager.CustomThemeIDSeperator },
                StringSplitOptions.RemoveEmptyEntries);
            string baseThemeID = separated[0];
            string encryptedThemePath = separated[1];
            string themePath = FileEncrypter.DecryptString(encryptedThemePath)[2];
            themePath = Utils.GetFileLoadPrefix() + themePath;

            return new string[2] { baseThemeID, themePath };
        }

        private static string lastLoadedThemeFilePath;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ThemeChangerExe), "ApplyTheme")]
        public static void InterceptThemeChangerForAnimatedTheme(ref string fileData)
        {
            if (!fileData.Contains(ThemeManager.CustomThemeIDSeperator)) return;

            var themeData = GetThemeDataFromFileData(fileData);
            var themePath = themeData[1];

            if(AnimatedTheme.IsProbablyValidAnimatedTheme(themePath))
            {
                AnimatedTheme theme = new();
                theme.LoadFromXml(themePath);
                AnimatedThemeIllustrator.CurrentTheme = theme;
                fileData = ThemeManager.getThemeDataStringForCustomTheme(theme.ThemePath);
                UpdateLastSavedCustomThemePath(theme.ThemePath);
            } else
            {
                if(AnimatedThemeIllustrator.CurrentTheme != null)
                {
                    lastLoadedThemeFilePath = AnimatedThemeIllustrator.CurrentTheme.FilePath;
                }

                AnimatedThemeIllustrator.CurrentTheme = null;
                UpdateLastSavedCustomThemePath(null);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ThemeChangerExe), "ApplyTheme")]
        public static void ReplaceBackedUpXServer()
        {
            var player = OS.currentInstance.thisComputer;
            var sys = player.files.root.searchForFolder("sys");
            if (sys.files.Count <= 0) return;
            if (string.IsNullOrWhiteSpace(lastLoadedThemeFilePath)) return;
            if (!sys.files.Any(f => f.name.StartsWith("x-server") && f.name.Contains("BACKUP"))) return; // This should Never Happen
            var lastBackedUpXServer = sys.files.Last(f => f.name.StartsWith("x-server") && f.name.Contains("BACKUP"));

            var newThemeData = ThemeManager.getThemeDataStringForCustomTheme(lastLoadedThemeFilePath);
            lastBackedUpXServer.data = newThemeData;

            lastLoadedThemeFilePath = null;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SASwitchToTheme), "Trigger")]
        public static void InterceptSwitchToThemeForAnimatedTheme(SASwitchToTheme __instance)
        {
            if (__instance.ThemePathOrName.Contains(".xml")) return;
            string fullPath = GetPrefixedFilepath(__instance.ThemePathOrName);

            if(AnimatedTheme.IsProbablyValidAnimatedTheme(fullPath))
            {
                AnimatedTheme theme = new();
                theme.LoadFromXml(fullPath);
                AnimatedThemeIllustrator.CurrentTheme = theme;
                __instance.ThemePathOrName = theme.ThemePath;
                UpdateLastSavedCustomThemePath(theme.ThemePath);
            } else
            {
                AnimatedThemeIllustrator.CurrentTheme = null;
                UpdateLastSavedCustomThemePath(null);
            }
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.High)]
        [HarmonyPatch(typeof(ThemeManager), "getThemeForDataString")]
        public static void InterceptGetThemeForDataStringPatch(string data, ref OSTheme __result)
        {
            if (__result == OSTheme.TerminalOnlyBlack)
            {
                // An animated theme was probably passed.
                if (!data.Contains(ThemeManager.CustomThemeIDSeperator)) return;

                string themePath;

                try
                {
                    string[] themeData = GetThemeDataFromFileData(data);
                    themePath = GetPrefixedFilepath(themeData[1]);
                }
                catch (Exception e)
                {
                    if (OS.DEBUG_COMMANDS && StuxnetCore.Configuration.ShowDebugText)
                    {
                        StuxnetCore.Logger.LogWarning("Error when trying to load possible theme " +
                            "(it's likely not actually a theme, and you can probably ignore this message): " +
                            string.Format("{0}\n{1}", e.ToString(), (e.InnerException ?? e).StackTrace));
                        StuxnetCore.Logger.LogWarning("Relevant data: " + data);
                    }
                    return;
                }

                if (!AnimatedTheme.IsProbablyValidAnimatedTheme(themePath)) return;

                try
                {
                    AnimatedTheme animatedTheme = new();
                    animatedTheme.LoadFromXml(themePath);
                    AnimatedThemeIllustrator.LastLoadedAnimatedTheme = animatedTheme;
                    CustomTheme vanillaTheme = CustomTheme.Deserialize(GetPrefixedFilepath(animatedTheme.ThemePath));
                    ThemeManager.LastLoadedCustomTheme = vanillaTheme;
                    __result = OSTheme.Custom;
                }
                catch (Exception e)
                {
                    StuxnetCore.Logger.LogError("Error when trying to load custom vanilla theme " +
                        "(or when trying to load animated theme): " +
                        string.Format("{0}\n{1}", e.ToString(), (e.InnerException ?? e).StackTrace));
                    return;
                }
            }
            return;
        }

        private static string lastSavedCustomThemePath;

        private static void UpdateLastSavedCustomThemePath(string newPath)
        {
            lastSavedCustomThemePath = newPath;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CrashModule), "completeReboot")]
        public static void CheckIfThemeWasReplaced()
        {
            var sys = OS.currentInstance.thisComputer.files.root.searchForFolder("sys");
            var xserver = sys.searchForFile("x-server.sys");
            if(xserver == null)
            {
                AnimatedThemeIllustrator.CurrentTheme = null;
                lastLoadedThemeFilePath = null;
                UpdateLastSavedCustomThemePath(null);
                return;
            }

            var xserverPath = GetThemeDataFromFileData(xserver.data)[1];
            if(xserverPath != lastSavedCustomThemePath)
            {
                var fullPath = GetPrefixedFilepath(xserverPath);
                if(AnimatedTheme.IsProbablyValidAnimatedTheme(fullPath))
                {
                    AnimatedTheme theme = new();
                    theme.LoadFromXml(xserverPath);
                    AnimatedThemeIllustrator.CurrentTheme = theme;
                    lastLoadedThemeFilePath = theme.ThemePath;
                } else
                {
                    AnimatedThemeIllustrator.CurrentTheme = null;
                }

                UpdateLastSavedCustomThemePath(null);
            }
        }

        [Event()]
        public static void ReplaceSavedXServer(SaveComputerEvent saveComputerEvent)
        {
            var comp = saveComputerEvent.Comp;
            if (comp.idName != "playerComp") return;
            if (AnimatedThemeIllustrator.CurrentTheme == null) return;

            var sys = comp.files.root.searchForFolder("sys");
            var xserver = sys.searchForFile("x-server.sys");
            var newPath = AnimatedThemeIllustrator.CurrentTheme.FilePath;

            xserver.data = ThemeManager.getThemeDataStringForCustomTheme(newPath);
        }

        private static string GetPrefixedFilepath(string filepath)
        {
            if(!filepath.Contains(Utils.GetFileLoadPrefix()))
            {
                filepath = Utils.GetFileLoadPrefix() + filepath;
            }
            return filepath;
        }
    }
}
