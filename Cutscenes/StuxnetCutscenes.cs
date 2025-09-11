using System;
using System.Linq;
using System.Xml.Linq;
using Hacknet;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Pathfinder.Event.Gameplay;
using Stuxnet_HN.Gui;

namespace Stuxnet_HN.Cutscenes
{
    public class StuxnetCutscene : TimedAnimation
    {
        public bool Active { get; set; } = false;

        private const string CUTSCENES_FOLDER = "Cutscenes/";
        public static string CutscenesFolder
        {
            get
            {
                return Utils.GetFileLoadPrefix() + CUTSCENES_FOLDER;
            }
        }

        public const string BASE_ELEMENT_NAME = "StuxnetCutscene";

        public void Draw()
        {
            if (OS.currentInstance == null) return;
            Rectangle bounds = OS.currentInstance.fullscreen;

            foreach(var elem in Elements)
            {
                elem.Draw(bounds);
            }
        }

        public override void Reset()
        {
            foreach(var element in Elements)
            {
                element.Reset();
            }
        }

        public override void OnLifetimeEnd()
        {
            StuxnetCore.CurrentlyLoadedCutscene = null;
            base.OnLifetimeEnd();
        }

        public void LoadInCutscene()
        {
            if(StuxnetCore.CurrentlyLoadedCutscene != null)
            {
                StuxnetCore.Logger.LogWarning("Tried to load in cutscene when one is already active -- skipping");
                return;
            }

            Reset();
            StuxnetCore.CurrentlyLoadedCutscene = this;
        }

        public override void LoadFromXml(string filepath)
        {
            var document = GetXDocumentFromFilepath(filepath);
            LoadFromXml(document);
        }

        public override void LoadFromXml(XDocument document)
        {
            base.LoadFromXml(document, "StuxnetCutscene");
            var baseElement = document.Element("StuxnetCutscene");

            var instructionsChild = baseElement.Element("Instructions");
            foreach (var inst in instructionsChild.Elements())
            {
                AnimatedElement elem = null;
                if (inst.Attributes().Any(atr => atr.Name.LocalName == "ElementID"))
                {
                    if (Elements.Any(el => el.ID == inst.Attribute("ElementID").Value))
                    {
                        elem = Elements.Find(el => el.ID == inst.Attribute("ElementID").Value);
                    }
                }
                AnimatedElementInstruction elemInst = null;
                switch (inst.Name.LocalName)
                {
                    case "DelayEnd":
                        elemInst = new DelayEndInstruction(inst);
                        break;
                    default:
                        continue;
                }
                Instructions.Add(elemInst);
            }
        }
    }

    public static class StuxnetCutsceneIllustrator
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(OS), "drawModules")]
        public static void DrawCurrentCutsceneIfAny()
        {
            if (StuxnetCore.CurrentlyLoadedCutscene == null || !StuxnetCore.CutsceneIsActive) return;

            StuxnetCore.CurrentlyLoadedCutscene.Draw();
        }
    }

    public static class StuxnetCutsceneUpdater
    {
        [Pathfinder.Meta.Load.Event()]
        public static void UpdateCurrentCutscene(OSUpdateEvent updateEvent)
        {
            if (StuxnetCore.CurrentlyLoadedCutscene == null || !StuxnetCore.CutsceneIsActive) return;

            StuxnetCore.CurrentlyLoadedCutscene.Update(updateEvent.GameTime);
        }
    }

    public class DelayEndInstruction : AnimatedElementInstruction
    {
        public DelayEndInstruction(XElement xml)
        {
            LoadFromXml(xml);
        }

        public override bool NeedsElement => false;

        public override void Activate()
        {
            // The delay end instruction only exists to - you guessed it - delay the end of the cutscene.
            // Because of this, we can just return. The only thing that matters is its Duration + Delay.
            return;
        }

        public override void LoadFromXml(XElement rootElement)
        {
            DelayFromStart = float.Parse(rootElement.Attribute("Delay").Value);
        }
    }
}
