using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;

using Microsoft.Xna.Framework;

using Stuxnet_HN.Extensions;

namespace Stuxnet_HN.Cutscenes
{
    public class StuxnetCutsceneRegister
    {
        public static StuxnetCutscene ReadFromFile(string filePath)
        {
            FileStream file = File.OpenRead(filePath);
            XmlReader xmlReader = XmlReader.Create(file);
            var cutscene = ReadFromXml(xmlReader);

            return cutscene;
        }

        public static StuxnetCutscene ReadFromXml(XmlReader xml)
        {
            StuxnetCutscene cutscene = new StuxnetCutscene();
            bool isRegistering = false;
            bool isReadingInstructions = false;

            while(xml.Name != "StuxnetCutscene")
            {
                xml.Read();
                if (xml.EOF)
                {
                    throw new FormatException("Unexpected end of file looking for StuxnetCutscene tag.");
                }
            }

            do
            {
                if(xml.Name == "StuxnetCutscene" && xml.IsStartElement())
                {
                    cutscene.id = xml.ReadRequiredAttribute("id");
                }

                xml.Read();

                if(xml.Name == "StuxnetCutscene" && !xml.IsStartElement())
                {
                    return cutscene;
                }

                if(xml.Name == "RegisterObjects")
                {
                    isRegistering = xml.IsStartElement();
                }

                // Register things
                if(xml.Name == "Rectangle" && isRegistering)
                {
                    Rectangle rect = new Rectangle();

                    string id = xml.ReadRequiredAttribute("id");

                    string size = xml.ReadRequiredAttribute("size");
                    Vector2 actualSize = new Vector2().FromString(size);

                    rect.X = 0;
                    rect.Y = 0;
                    rect.Width = (int)actualSize.X;
                    rect.Height = (int)actualSize.Y;

                    cutscene.RegisterRectangle(id, rect);
                }

                if(xml.Name == "Image" && isRegistering)
                {
                    string id = xml.ReadRequiredAttribute("id");
                    string path = xml.ReadRequiredAttribute("path");
                    Vector2 size = new Vector2().FromString(xml.ReadRequiredAttribute("size"));

                    cutscene.RegisterImage(id, path, size);
                }

                // Instructions
                if(xml.Name == "Instructions")
                {
                    isReadingInstructions = xml.IsStartElement();
                }

                if(xml.Name == "ShowRectangle" && isReadingInstructions)
                {
                    StuxnetCutsceneInstruction inst;

                    string id = xml.ReadRequiredAttribute("id");
                    float delay = float.Parse(xml.ReadRequiredAttribute("delay"));

                    inst = StuxnetCutsceneInstruction.CreateInstantTransition(StuxnetCutsceneInstruction.StuxnetCutsceneObjectTypes.Rectangle,
                        id, true);
                    inst.Delay = delay;
                    inst.Cutscene = cutscene;

                    cutscene.RegisterInstruction(inst);
                }

                if (xml.Name == "HideRectangle" && isReadingInstructions)
                {
                    StuxnetCutsceneInstruction inst;

                    string id = xml.ReadRequiredAttribute("id");
                    float delay = float.Parse(xml.ReadRequiredAttribute("delay"));

                    inst = StuxnetCutsceneInstruction.CreateInstantTransition(StuxnetCutsceneInstruction.StuxnetCutsceneObjectTypes.Rectangle,
                        id, false);
                    inst.Delay = delay;
                    inst.Cutscene = cutscene;

                    cutscene.RegisterInstruction(inst);
                }

                if (xml.Name == "MoveRectangle" && isReadingInstructions)
                {
                    StuxnetCutsceneInstruction inst;
                    float tweenDuration = 0f;

                    string id = xml.ReadRequiredAttribute("id");
                    string pos = xml.ReadRequiredAttribute("pos");
                    bool tween = bool.Parse(xml.ReadRequiredAttribute("tween"));

                    if(tween)
                    {
                        tweenDuration = float.Parse(xml.ReadRequiredAttribute("tweenDuration"));
                    }

                    Vector2 newPos = new Vector2().FromString(pos);

                    float delay = float.Parse(xml.ReadRequiredAttribute("delay"));

                    inst = StuxnetCutsceneInstruction.CreateMovementInstruction(StuxnetCutsceneInstruction.StuxnetCutsceneObjectTypes.Rectangle,
                        id, newPos, tween, tweenDuration);

                    inst.Delay = delay;
                    inst.Cutscene = cutscene;

                    cutscene.RegisterInstruction(inst);
                }

                if(xml.Name == "ShowImage" && isReadingInstructions)
                {
                    StuxnetCutsceneInstruction inst;

                    string id = xml.ReadRequiredAttribute("id");
                    float delay = xml.GetDelay();

                    inst = StuxnetCutsceneInstruction.CreateInstantTransition(StuxnetCutsceneInstruction.StuxnetCutsceneObjectTypes.Image,
                        id, true);

                    inst.Delay = delay;
                    inst.Cutscene = cutscene;

                    cutscene.RegisterInstruction(inst);
                }

                if(xml.Name == "HideImage" && isReadingInstructions)
                {
                    StuxnetCutsceneInstruction inst;

                    string id = xml.ReadRequiredAttribute("id");
                    float delay = xml.GetDelay();

                    inst = StuxnetCutsceneInstruction.CreateInstantTransition(StuxnetCutsceneInstruction.StuxnetCutsceneObjectTypes.Image,
                        id, false);

                    inst.Delay = delay;
                    inst.Cutscene = cutscene;

                    cutscene.RegisterInstruction(inst);
                }

                if(xml.Name == "MoveImage" && isReadingInstructions)
                {
                    StuxnetCutsceneInstruction inst;
                    float tweenDuration = 0f;

                    string id = xml.ReadRequiredAttribute("id");
                    Vector2 targetPos = new Vector2().FromString(xml.ReadRequiredAttribute("pos"));
                    bool tween = bool.Parse(xml.ReadRequiredAttribute("tween"));

                    if (tween)
                    {
                        tweenDuration = float.Parse(xml.ReadRequiredAttribute("tweenDuration"));
                    }

                    float delay = xml.GetDelay();

                    inst = StuxnetCutsceneInstruction.CreateMovementInstruction(StuxnetCutsceneInstruction.StuxnetCutsceneObjectTypes.Image,
                        id, targetPos, tween, tweenDuration);

                    inst.Delay = delay;
                    inst.Cutscene = cutscene;

                    cutscene.RegisterInstruction(inst);
                }

                if(xml.Name == "DelayEnding" && isReadingInstructions)
                {
                    StuxnetCutsceneInstruction inst;

                    float delay = float.Parse(xml.ReadRequiredAttribute("delay"));

                    inst = StuxnetCutsceneInstruction.CreateDelayEnding();
                    inst.Delay = delay;
                    inst.Cutscene = cutscene;

                    cutscene.RegisterInstruction(inst);
                }
            } while (!xml.EOF);
            throw new FormatException("Unexpected end-of-file while reading Stuxnet cutscene XML!");
        }
    }
}
