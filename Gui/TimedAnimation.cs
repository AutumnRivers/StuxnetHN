using Hacknet;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Stuxnet_HN.Gui
{
    public abstract class TimedAnimation
    {
        public string FilePath { get; protected set; }
        public List<AnimatedElement> Elements { get; set; } = new();
        public List<AnimatedElementInstruction> Instructions { get; set; } = new();

        public TimedAnimation() { }

        public TimedAnimation(string filepath)
        {
            FilePath = filepath;
        }

        public TimedAnimation(string filepath, string baseElementName)
        {
            FilePath = filepath;
            LoadFromXml(FilePath, baseElementName);
        }

        public float LastDelayTriggered { get; set; } = -1;

        private float _lifetime = 0;
        public virtual float Lifetime
        {
            get { return _lifetime; }
            set
            {
                float fullDuration = GetFullDuration();
                if (value >= fullDuration)
                {
                    LastDelayTriggered = -1;
                    OnLifetimeEnd();
                }
                else
                {
                    _lifetime = value;
                }
            }
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

            List<AnimatedElementInstruction> instructionsWithElements = Instructions.Where(inst => inst.Element != null)
                .ToList();

            foreach (var elem in Elements)
            {
                if (!instructionsWithElements.Any(inst => inst.Element.ID == elem.ID)) continue;
                lastInstructions.Add(instructionsWithElements.Last(inst => inst.Element.ID == elem.ID));
            }

            lastInstructions.AddRange(Instructions.Where(inst => inst.Element == null));

            foreach (var inst in lastInstructions)
            {
                fullDuration = Math.Max(fullDuration, inst.Duration + inst.DelayFromStart);
            }

            return fullDuration;
        }

        public virtual void ResetInstructions()
        {
            foreach (var inst in Instructions) { inst.Activated = false; }
        }

        public virtual void Update(GameTime gameTime)
        {
            foreach(var element in Elements)
            {
                element.Update(gameTime);
            }

            SortInstructions();

            Lifetime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            foreach(var inst in Instructions)
            {
                if(inst.Element != null)
                {
                    inst.Element = FindElementByID(inst.Element.ID);
                }
                if (inst.DelayFromStart < LastDelayTriggered) continue;

                if(inst.DelayFromStart <= Lifetime && !inst.Activated)
                {
                    LastDelayTriggered = inst.DelayFromStart;
                    inst.Activate();
                }
            }
        }

        public abstract void Reset();

        public virtual void OnLifetimeEnd()
        {
            Reset();
        }

        public virtual void LoadFromXml(string filepath)
        {
            throw new NotImplementedException();
        }

        public virtual void LoadFromXml(string filepath, string baseElementName)
        {
            XDocument document = GetXDocumentFromFilepath(filepath);
            LoadFromXml(document, baseElementName);
        }

        public virtual void LoadFromXml(XDocument document)
        {
            throw new NotImplementedException();
        }

        public virtual void LoadFromXml(XDocument document, string baseElementName)
        {
            XElement baseElement = document.Element(baseElementName);

            var elementsChild = baseElement.Element("Elements") ?? throw new FormatException("Elements child node missing!");
            foreach (var elem in elementsChild.Elements())
            {
                string id = elem.Attribute("ID").Value;
                if (Elements.Any(el => el.ID == id)) continue;
                AnimatedElement animatedElement = AnimatedElement.LoadFromXml(elem);
                Elements.Add(animatedElement);
            }

            var instructionsChild = baseElement.Element("Instructions") ??
                throw new FormatException("Instructions child node missing!");
            foreach (var inst in instructionsChild.Elements())
            {
                AnimatedElement elem = null;
                if(inst.Attributes().Any(atr => atr.Name.LocalName == "ElementID"))
                {
                    if (Elements.Any(el => el.ID == inst.Attribute("ElementID").Value))
                    {
                        elem = Elements.Find(el => el.ID == inst.Attribute("ElementID").Value);
                    }
                }
                AnimatedElementInstruction elemInst = null;
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
                    case "ToggleVisibility":
                        elemInst = new ToggleVisibilityInstruction(elem, inst);
                        break;
                    default:
                        continue;
                }
                Instructions.Add(elemInst);
            }
        }

        protected static XDocument GetXDocumentFromFilepath(string filepath)
        {
            if(!File.Exists(filepath))
            {
                throw new FileNotFoundException(
                    string.Format("Couldn't find file at {0}", filepath)
                    );
            }

            FileStream fileStream = File.OpenRead(filepath);
            XDocument document = XDocument.Load(fileStream);
            fileStream.Close();
            return document;
        }
    }
}
