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
            string themePath = FileEncrypter.DecryptString(encryptedThemePath)[2];
            themePath = Utils.GetFileLoadPrefix() + themePath;

            return new string[2] { baseThemeID, themePath };
        }

        [HarmonyPrefix]
        // We load these prefixes as first because they return the vanilla theme path,
        // which means it's basically just back to vanilla when we're done.
        // This way, we don't butt heads with any other plugins!
        [HarmonyPriority(Priority.First)]
        [HarmonyPatch(typeof(ThemeManager), "switchTheme", new Type[] { typeof(object), typeof(string) })]
        /*
         * This patch intercepts the theme chosen by the user, then passes back
         * the vanilla theme to it. (Assuming it's a valid animated theme file.)
         * 
         * The way it's done makes it so the theme still kinda gets loaded the
         * vanilla way, and hopefully shouldn't interfere with any other theme
         * logic.
         */
        public static void InterceptSwitchThemePatch(ref string customThemePath)
        {
            string fullPath = GetPrefixedFilepath(customThemePath);
            if (!AnimatedTheme.IsProbablyValidAnimatedTheme(fullPath))
            {
                // We assume it's a valid vanilla theme, and unload the current animated theme
                AnimatedThemeIllustrator.CurrentTheme = null;
                return;
            }

            AnimatedTheme animatedTheme = new();
            animatedTheme.LoadFromXml(fullPath);
            AnimatedThemeIllustrator.CurrentTheme = animatedTheme;
            StuxnetCore.Logger.LogDebug("Loading animated theme...");
            customThemePath = animatedTheme.ThemePath;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ThemeManager), "switchTheme", new Type[] { typeof(object), typeof(OSTheme) })]
        public static void InterceptOtherSwitchThemePatch(OSTheme theme)
        {
            if (AnimatedThemeIllustrator.LastLoadedAnimatedTheme == null) return;

            if(theme != OSTheme.Custom)
            {
                if(OS.currentInstance.EffectsUpdater.themeSwapTimeRemaining > 0 &&
                    AnimatedThemeIllustrator.CurrentTheme != null)
                {
                    AnimatedThemeIllustrator.Visible = false;
                } else
                {
                    AnimatedThemeIllustrator.CurrentTheme = null;
                }
                return;
            }

            AnimatedThemeIllustrator.CurrentTheme = AnimatedThemeIllustrator.LastLoadedAnimatedTheme;
            AnimatedThemeIllustrator.LastLoadedAnimatedTheme = null;
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.First)]
        [HarmonyPatch(typeof(ThemeManager), "getThemeForDataString")]
        public static void InterceptGetThemeForDataStringPatch(string data, ref OSTheme __result)
        {
            if(__result == OSTheme.TerminalOnlyBlack)
            {
                // An animated theme was probably passed.
                if (!data.Contains(ThemeManager.CustomThemeIDSeperator)) return;

                string themePath;

                try
                {
                    string[] themeData = GetThemeDataFromFileData(data);
                    themePath = GetPrefixedFilepath(themeData[1]);
                } catch(Exception e)
                {
                    if(OS.DEBUG_COMMANDS && StuxnetCore.Configuration.ShowDebugText)
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
                    CustomTheme vanillaTheme = CustomTheme.Deserialize(Utils.GetFileLoadPrefix() + animatedTheme.ThemePath);
                    ThemeManager.LastLoadedCustomTheme = vanillaTheme;
                    __result = OSTheme.Custom;
                } catch(Exception e)
                {
                    StuxnetCore.Logger.LogError("Error when trying to load custom vanilla theme " +
                        "(or when trying to load animated theme): " +
                        string.Format("{0}\n{1}", e.ToString(), (e.InnerException ?? e).StackTrace));
                    return;
                }
            }
            return;
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
