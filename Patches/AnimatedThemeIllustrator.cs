using Hacknet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using HarmonyLib;
using Stuxnet_HN.Gui;
using Pathfinder.Event.Gameplay;
using System.Collections.ObjectModel;
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

            foreach(var elem in CurrentTheme.Elements)
            {
                elem.Update(updateEvent.GameTime);
            }

            CurrentTheme.SortInstructions();
            CurrentTheme.Lifetime += (float)updateEvent.GameTime.ElapsedGameTime.TotalSeconds;
            foreach(var inst in CurrentTheme.Instructions)
            {
                inst.Element = CurrentTheme.FindElementByID(inst.Element.ID);
                if(CurrentTheme.LastDelayNeedsReset)
                {
                    CurrentTheme.LastDelayTriggered = -1;
                    CurrentTheme.Lifetime = 0;
                    CurrentTheme.LastDelayNeedsReset = false;
                    CurrentTheme.ResetInstructions();
                }
                if(inst.DelayFromStart < CurrentTheme.LastDelayTriggered) continue;
                
                if(inst.DelayFromStart <= CurrentTheme.Lifetime && !inst.Activated)
                {
                    CurrentTheme.LastDelayTriggered = CurrentTheme.Lifetime;
                    inst.Activate();
                }
            }
        }
    }

    public class AnimatedTheme
    {
        public string ThemePath;
        public string Filepath { get; private set; }
        public List<AnimatedElement> Elements = new();
        public List<AnimatedElementInstruction> Instructions = new();

        public bool HasLoaded = false;

        public bool LastDelayNeedsReset = false;

        public float LastDelayTriggered { get; set; } = -1;

        private float _lifetime = 0;
        public float Lifetime
        {
            get { return _lifetime; }
            set
            {
                float fullDuration = GetFullDuration();
                if(value >= fullDuration)
                {
                    LastDelayTriggered = -1;
                    LastDelayNeedsReset = true;
                    Reset();
                } else
                {
                    _lifetime = value;
                }
            }
        }

        public AnimatedTheme(string themePath)
        {
            ThemePath = themePath;
        }

        private bool HasSorted = false;

        public void SortInstructions()
        {
            if (HasSorted) return;
            Instructions.Sort(AnimatedElementInstruction.CompareByDelay);
            HasSorted = true;
        }

        public AnimatedElement FindElementByID(string id)
        {
            return Elements.FirstOrDefault(elem => elem.ID == id);
        }

        public float GetFullDuration()
        {
            SortInstructions();
            float fullDuration = Instructions.Last().DelayFromStart + Instructions.Last().Duration;
            List<AnimatedElementInstruction> lastInstructions = new();

            foreach(var elem in Elements)
            {
                if (!Instructions.Any(inst => inst.Element.ID == elem.ID)) continue;
                lastInstructions.Add(Instructions.Last(inst => inst.Element.ID == elem.ID));
            }

            foreach(var inst in lastInstructions)
            {
                fullDuration = Math.Max(fullDuration, inst.Duration + inst.DelayFromStart);
            }

            return fullDuration;
        }

        public void Reset()
        {
            for(int idx = 0; idx < Elements.Count; idx++)
            {
                var elem = Elements[idx];
                if (elem.RotateIndefinitely) continue;
                Elements[idx].Reset();
            }
            Lifetime = 0.0f;
            LastDelayTriggered = -1.0f;
        }

        public void ResetInstructions()
        {
            foreach (var inst in Instructions) { inst.Activated = false; }
        }
        
        public static AnimatedTheme LoadFromXml(string filepath)
        {
            if(!filepath.Contains(Utils.GetFileLoadPrefix()))
            {
                filepath = Utils.GetFileLoadPrefix() + filepath;
            }
            if(StuxnetCache.TryGetCachedTheme(filepath, out var cachedTheme))
            {
                if(AnimatedThemeIllustrator.CurrentTheme != cachedTheme)
                {
                    cachedTheme.Reset();
                }
                return cachedTheme;
            }
            FileStream themeFileStream = File.OpenRead(filepath);
            XDocument themeDocument = XDocument.Load(themeFileStream);
            AnimatedTheme theme = LoadFromXml(themeDocument);
            theme.Filepath = filepath;
            themeFileStream.Close();
            StuxnetCache.CacheTheme(theme);
            return theme;
        }

        public static bool IsProbablyValidAnimatedTheme(string filepath)
        {
            if (!File.Exists(filepath)) return false;

            string firstLine = File.ReadLines(filepath).First();
            return firstLine.Contains("StuxnetAnimatedTheme");
        }

        public static string GetBaseThemeFromAnimatedTheme(string filepath)
        {
            if (!IsProbablyValidAnimatedTheme(filepath)) return null;

            AnimatedTheme theme = LoadFromXml(filepath);
            return theme.ThemePath;
        }

        public static AnimatedTheme LoadFromXml(XDocument document)
        {
            XElement baseElement = document.Element("StuxnetAnimatedTheme");

            string baseTheme = baseElement.Element("BaseTheme").Value;
            AnimatedTheme theme = new(baseTheme);

            var elementsChild = baseElement.Element("Elements");
            foreach(var elem in elementsChild.Elements())
            {
                string id = elem.Attribute("ID").Value;
                if (theme.Elements.Any(el => el.ID == id)) continue;
                AnimatedElement animatedElement = AnimatedElement.LoadFromXml(elem);
                theme.Elements.Add(animatedElement);
            }

            var instructionsChild = baseElement.Element("Instructions");
            foreach(var inst in instructionsChild.Elements())
            {
                if(!theme.Elements.Any(el => el.ID == inst.Attribute("ElementID").Value))
                {
                    throw new FormatException(
                        string.Format("Invalid animated element instruction -- no element exists with an ID of {0}",
                        inst.Attribute("ElementID").Value)
                        );
                }
                AnimatedElement elem = theme.Elements.Find(el => el.ID == inst.Attribute("ElementID").Value);

                AnimatedElementInstruction elemInst;
                switch(inst.Name.LocalName)
                {
                    case "Translate":
                        elemInst = new TranslateElementInstruction(elem, inst);
                        break;
                    case "Rotate":
                        elemInst = new RotateElementInstruction(elem, inst);
                        break;
                    case "Resize":
                        elemInst = new ResizeElementInstruction(elem, inst);
                        break;
                    case "Fade":
                        elemInst = new FadeElementInstruction(elem, inst);
                        break;
                    default:
                        throw new FormatException(
                            string.Format("Invalid animated element instruction -- instruction {0} not recognized. " +
                            "(Did you misspell it? Instructions are case-sensitive!)", inst.Name)
                            );
                }
                theme.Instructions.Add(elemInst);
            }

            theme.HasLoaded = true;
            return theme;
        }
    }
}
