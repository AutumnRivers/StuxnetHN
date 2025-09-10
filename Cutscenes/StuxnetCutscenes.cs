using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Hacknet;
using Hacknet.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathfinder.Event.Gameplay;
using Stuxnet_HN.Cutscenes.Patches;

using Stuxnet_HN.Extensions;
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
                if (!Elements.Any(el => el.ID == inst.Attribute("ElementID").Value))
                {
                    throw new FormatException(
                        string.Format("Invalid animated element instruction -- no element exists with an ID of {0}",
                        inst.Attribute("ElementID").Value)
                        );
                }
                AnimatedElement elem = Elements.Find(el => el.ID == inst.Attribute("ElementID").Value);
                AnimatedElementInstruction elemInst;
                switch(inst.Name.LocalName)
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

    public static class StuxnetCutsceneUpdater
    {
        [Pathfinder.Meta.Load.Event()]
        public static void UpdateCurrentCutscene(OSUpdateEvent updateEvent)
        {
            if (StuxnetCore.CurrentlyLoadedCutscene == null) return;

            StuxnetCore.CurrentlyLoadedCutscene.Update(updateEvent.GameTime);
        }
    }

    public class DelayEndInstruction : AnimatedElementInstruction
    {
        public DelayEndInstruction(XElement xml) : base(null, xml) { }

        public override void Activate()
        {
            // The delay end instruction only exists to - you guessed it - delay the end of the cutscene.
            // Because of this, we can just return. The only thing that matters is its Duration + Delay.
            return;
        }
    }
}
