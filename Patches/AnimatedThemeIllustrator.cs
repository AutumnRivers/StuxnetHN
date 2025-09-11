using Hacknet;
using System.Linq;
using System.IO;
using HarmonyLib;
using Stuxnet_HN.Gui;
using Pathfinder.Event.Gameplay;
using System.Xml.Linq;

namespace Stuxnet_HN.Patches
{
    [HarmonyPatch]
    public static class AnimatedThemeIllustrator
    {
        public static AnimatedTheme CurrentTheme;
        internal static AnimatedTheme LastLoadedAnimatedTheme;

        public static bool Visible { get; set; } = true;

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(OS), "drawBackground")]
        public static void DrawAnimatedElements(OS __instance)
        {
            if (CurrentTheme == null || !Visible) return;
            if (!CurrentTheme.HasLoaded) return;

            foreach(var element in CurrentTheme.Elements)
            {
                element.Draw(__instance.fullscreen);
            }
        }

        public static void SwitchToAnimatedTheme(AnimatedTheme theme, float flickerDuration)
        {
            CurrentTheme = theme;
            if(flickerDuration > 0.0f)
            {
                OS.currentInstance.EffectsUpdater.StartThemeSwitch(flickerDuration,
                    OSTheme.Custom, OS.currentInstance, theme.ThemePath);
            } else
            {
                ThemeManager.switchTheme(OS.currentInstance, theme.ThemePath);
            }
        }

        [Pathfinder.Meta.Load.Event()]
        public static void UpdateTheme(OSUpdateEvent updateEvent)
        {
            if (CurrentTheme == null || !Visible) return;

            CurrentTheme.Update(updateEvent.GameTime);
        }
    }

    public class AnimatedTheme : TimedAnimation
    {
        public string ThemePath;
        public bool HasLoaded = false;
        public bool LastDelayNeedsReset = false;

        public AnimatedTheme() : base() { }

        public AnimatedTheme(string themePath) : base()
        {
            ThemePath = themePath;
        }

        public override void Reset()
        {
            for(int idx = 0; idx < Elements.Count; idx++)
            {
                var elem = Elements[idx];
                if (elem.RotateIndefinitely) continue;
                Elements[idx].Reset();
            }
            Lifetime = 0.0f;
            LastDelayTriggered = -1.0f;
            ResetInstructions();
        }

        public static bool IsProbablyValidAnimatedTheme(string filepath)
        {
            if (!File.Exists(filepath)) return false;

            var lines = File.ReadAllLines(filepath);

            string firstLine = lines.First();
            bool probablyValid = firstLine.Contains("StuxnetAnimatedTheme");
            
            if(!probablyValid)
            {
                string secondLine = lines[1];
                probablyValid = secondLine.Contains("StuxnetAnimatedTheme");
            }

            return probablyValid;
        }

        public static string GetBaseThemeFromAnimatedTheme(string filepath)
        {
            if (!IsProbablyValidAnimatedTheme(filepath)) return null;

            AnimatedTheme theme = new();
            theme.LoadFromXml(filepath);
            return theme.ThemePath;
        }

        public override void LoadFromXml(string filepath)
        {
            XDocument document = GetXDocumentFromFilepath(filepath);
            FilePath = filepath;
            LoadFromXml(document);
            StuxnetCache.CacheTheme(this);
        }

        public override void LoadFromXml(XDocument document)
        {
            base.LoadFromXml(document, "StuxnetAnimatedTheme");
            XElement baseElement = document.Element("StuxnetAnimatedTheme");

            ThemePath = baseElement.Element("BaseTheme").Value;
            HasLoaded = true;
        }
    }
}
