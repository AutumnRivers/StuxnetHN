using Hacknet;
using Hacknet.Extensions;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using static Pathfinder.Event.Menu.DrawMainMenuTitlesEvent;

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
            string themePath = FileEncrypter.DecryptString(encryptedThemePath)[2]; // ??? what are the first two values
            themePath = Utils.GetFileLoadPrefix() + themePath;

            return new string[2] { baseThemeID, themePath };
        }

        [HarmonyPrefix]
        // We load these prefixes as first because they return the vanilla theme path,
        // which means it's basically just back to vanilla when we're done.
        // This way, we don't butt heads with any other plugins!
        [HarmonyPriority(Priority.First)]
        [HarmonyPatch(typeof(ThemeChangerExe), "ApplyTheme")]
        /*
         * This patch intercepts the theme chosen by the user, then passes back
         * the vanilla theme to it. (Assuming it's a valid animated theme file.)
         * 
         * The way it's done makes it so the theme still kinda gets loaded the
         * vanilla way, and hopefully shouldn't interfere with any other theme
         * logic.
         */
        public static void LoadAnimatedThemeFromFile(ref string fileData)
        {
            if (!fileData.Contains(ThemeManager.CustomThemeIDSeperator))
            {
                // Hacknet will load TerminalOnlyBlack when it errors on theme switch here,
                // so we don't want the animated theme running.
                AnimatedThemeIllustrator.CurrentTheme = null;
                return;
            }

            string[] themeData = GetThemeDataFromFileData(fileData);
            string themePath = themeData[1];
            themePath = GetPrefixedFilepath(themePath);
            if (!AnimatedTheme.IsProbablyValidAnimatedTheme(themePath))
            {
                // We assume it's a valid vanilla theme, and unload the current animated theme
                AnimatedThemeIllustrator.CurrentTheme = null;
                return;
            }

            AnimatedTheme animatedTheme = AnimatedTheme.LoadFromXml(themePath);
            AnimatedThemeIllustrator.CurrentTheme = animatedTheme;
            fileData = ThemeManager.getThemeDataStringForCustomTheme(animatedTheme.ThemePath);
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        [HarmonyPatch(typeof(ThemeManager), "getThemeForDataString")]
        public static void LoadVanillaThemeFromAnimatedThemeData(ref string data)
        {
            if (!data.Contains(ThemeManager.CustomThemeIDSeperator)) return;

            string[] themeData = GetThemeDataFromFileData(data);
            string themePath = themeData[1];
            themePath = GetPrefixedFilepath(themePath);

            if (!AnimatedTheme.IsProbablyValidAnimatedTheme(themePath)) return;

            data = ThemeManager.getThemeDataStringForCustomTheme(AnimatedTheme.GetBaseThemeFromAnimatedTheme(themePath));
        }

        // This patch works basically the same way as above.
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        [HarmonyPatch(typeof(SASwitchToTheme), "Trigger")]
        public static void LoadAnimatedThemeFromAction(SASwitchToTheme __instance)
        {
            // We don't wanna interfere with loading built-in themes
            if (Enum.TryParse<OSTheme>(__instance.ThemePathOrName, out var _))
            {
                AnimatedThemeIllustrator.CurrentTheme = null;
                return;
            }

            string fullThemePath = GetPrefixedFilepath(__instance.ThemePathOrName);
            if (!AnimatedTheme.IsProbablyValidAnimatedTheme(fullThemePath))
            {
                AnimatedThemeIllustrator.CurrentTheme = null;
                return;
            }

            AnimatedTheme animatedTheme = AnimatedTheme.LoadFromXml(fullThemePath);
            AnimatedThemeIllustrator.CurrentTheme = animatedTheme;
            __instance.ThemePathOrName = animatedTheme.ThemePath;
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
