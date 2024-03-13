using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet;
using Hacknet.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Stuxnet_HN.Cutscenes.Patches;

namespace Stuxnet_HN.Cutscenes
{
    public class StuxnetCutscene
    {
        public string id;
        public string filepath;
        public string delayHostID = "delay";

        public Dictionary<string, Rectangle> rectangles = new Dictionary<string, Rectangle>();
        public Dictionary<string, Texture2D> images = new Dictionary<string, Texture2D>();

        public List<StuxnetCutsceneInstruction> instructions = new List<StuxnetCutsceneInstruction>();

        internal List<string> activeRectangles = new List<string>();
        internal List<string> activeImages = new List<string>();

        public StuxnetCutscene() { }

        public StuxnetCutscene(string delayHostID)
        {
            this.delayHostID = delayHostID;
        }

        public void ResetPositions()
        {
            for(int i = 0; i < rectangles.Count; i++)
            {
                var rect = rectangles.ElementAt(i);
                Rectangle r = rect.Value;
                r.X = 0;
                r.Y = 0;
                rectangles[rect.Key] = r;
            }
        }

        public void HideAllObjects()
        {
            activeRectangles.Clear();
            activeImages.Clear();
        }

        public void RegisterRectangle(string id, Rectangle rect)
        {
            if(rectangles.ContainsKey(id))
            {
                rectangles[id] = rect;
            } else
            {
                rectangles.Add(id, rect);
            }
        }
        public void DeregisterRectangle(string id)
        {
            if(rectangles.ContainsKey(id))
            {
                rectangles.Remove(id);
            }
        }

        public void RegisterImage(string id, string imagePath)
        {
            string extFolderPath = ExtensionLoader.ActiveExtensionInfo.FolderPath;
            FileStream imageStream = File.OpenRead(extFolderPath + "/" + imagePath);
            Texture2D image = Texture2D.FromStream(GuiData.spriteBatch.GraphicsDevice, imageStream);

            if(images.ContainsKey(id))
            {
                images[id] = image;
            } else
            {
                images.Add(id, image);
            }

            imageStream.Dispose();
        }
        public void DeregisterImage(string id)
        {
            if(images.ContainsKey(id))
            {
                images.Remove(id);
            }
        }

        public void RegisterInstruction(StuxnetCutsceneInstruction instruction)
        {
            if(instructions.Contains(instruction))
            {
                Console.WriteLine(StuxnetCore.logPrefix +
                    "WARN - Instruction already registered, skipping");
                return;
            }

            instructions.Add(instruction);
        }

        public override string ToString()
        {
            StringBuilder response = new StringBuilder("STUXNET CUTSCENE: ");
            response.Append($"[ID: {id}] ");
            response.Append($"[Path: {filepath}] ");
            response.Append($"[DelayHost: {delayHostID}]");
            return response.ToString();
        }
    }

    public class StuxnetCutsceneImage
    {
        public Texture2D image;
        public Vector2 position;
        public Vector2 size;
    }

    public class StuxnetCutsceneInstruction
    {
        public enum InstructionTypes
        {
            Move,
            BlipIn,
            BlipOut,
            Rotate,
            Flicker,
            FadeIn,
            FadeOut,
            Tint,
            InstantIn,
            InstantOut,
            ResetCutscene,
            DelayEnd
        }

        public enum StuxnetCutsceneObjectTypes
        {
            Rectangle,
            Image
        }

        public InstructionTypes instructionType;

        public string type;
        public string typeID;

        private StuxnetCutscene cutscene;
        public StuxnetCutscene Cutscene
        {
            get { return cutscene; }
            internal set
            {
                cutscene = value;
            }
        }

        private float delay;
        public float Delay
        {
            get { return delay; }
            set
            {
                if(value < 0f)
                {
                    delay = 0f;
                } else
                {
                    delay = value;
                }
            }
        }

        // Movement Instructions
        public Vector2 newPosition;
        public bool tweenMovement = false;
        private float tweenDuration;
        public float TweenDuration
        {
            get { return tweenDuration; }
            set
            {
                if(value < 0f)
                {
                    tweenDuration = 0f;
                } else
                {
                    tweenDuration = value;
                }
            }
        }

        private float rotationAngle;
        public float RotationAngle
        {
            get
            {
                return rotationAngle;
            }
            set
            {
                if(value < 0f || value > 359.9f)
                {
                    rotationAngle = 0f;
                } else
                {
                    rotationAngle = value;
                }
            }
        }

        // Transition Instructions
        private float transitionDuration;
        public float TransitionDuration
        {
            get
            {
                return transitionDuration;
            }
            set
            {
                if(value < 0f)
                {
                    transitionDuration = 0f;
                } else
                {
                    transitionDuration = value;
                }
            }
        }

        // Flicker Instructions
        private float flickerFrequency;
        public float FlickerFrequency
        {
            get { return flickerFrequency; }
            set
            {
                if(value < 0f)
                {
                    flickerFrequency = 0f;
                } else if(value > 1f)
                {
                    flickerFrequency = 1f;
                } else
                {
                    flickerFrequency = value;
                }
            }
        }

        // Tint Instructions
        private float tintOpacity;
        public float TintOpacity
        {
            get { return tintOpacity; }
            set
            {
                if (value < 0f)
                {
                    tintOpacity = 0f;
                }
                else if (value > 1f)
                {
                    tintOpacity = 1f;
                }
                else
                {
                    tintOpacity = value;
                }
            }
        }
        public Color tintColor;

        public StuxnetCutsceneInstruction() { }

        public StuxnetCutsceneInstruction(StuxnetCutscene associatedCutscene)
        {
            Cutscene = associatedCutscene;
        }

        public void Execute()
        {
            switch(instructionType)
            {
                case InstructionTypes.InstantIn:
                    ExecuteInstantTrans(type, typeID, true);
                    break;
                case InstructionTypes.InstantOut:
                    ExecuteInstantTrans(type, typeID, false);
                    break;
                case InstructionTypes.Move:
                    ExecuteMovement(type, typeID);
                    break;
                case InstructionTypes.ResetCutscene:
                    ExecuteReset();
                    break;
            }
        }

        public void ExecuteMovement(string objectType, string id)
        {
            Vector2 targetPos = newPosition;
            StuxnetCutsceneObjectTypes type = StuxnetCutsceneObjectTypes.Rectangle;

            if(objectType == "Rectangle")
            {
                type = StuxnetCutsceneObjectTypes.Rectangle;
            } else if(objectType == "Image")
            {
                type = StuxnetCutsceneObjectTypes.Image;
            }

            if(!tweenMovement)
            {
                if(type == StuxnetCutsceneObjectTypes.Rectangle)
                {
                    Rectangle rect = cutscene.rectangles[id];
                    rect.X = (int)targetPos.X;
                    rect.Y = (int)targetPos.Y;
                    cutscene.rectangles[id] = rect;
                }
            } else
            {
                if(type == StuxnetCutsceneObjectTypes.Rectangle)
                {
                    Rectangle rect = cutscene.rectangles[id];
                    CutsceneExecutor.AddTweenedRectangle(id, rect, targetPos, TweenDuration);
                }
            }
        }

        public void ExecuteInstantTrans(string objectType, string id, bool activate = true)
        {
            if(activate)
            {
                if(objectType == "Rectangle")
                {
                    if(cutscene.activeRectangles.Contains(id)) { return; }
                    cutscene.activeRectangles.Add(id);
                } else if(objectType == "Image")
                {
                    if (cutscene.activeImages.Contains(id)) { return; }
                    cutscene.activeImages.Add(id);
                }
            } else
            {
                if (objectType == "Rectangle")
                {
                    if (!cutscene.activeRectangles.Contains(id)) { return; }
                    cutscene.activeRectangles.Remove(id);
                }
                else if (objectType == "Image")
                {
                    if (!cutscene.activeImages.Contains(id)) { return; }
                    cutscene.activeImages.Remove(id);
                }
            }
        }

        public void ExecuteReset()
        {
            cutscene.HideAllObjects();
            cutscene.ResetPositions();

            StuxnetCore.cutsceneIsActive = false;
            StuxnetCore.activeCutsceneID = "NONE";

            CutsceneExecutor.hasSetDelays = false;
            CutsceneExecutor.targetVectorRects.Clear();
        }

        public static StuxnetCutsceneInstruction CreateMovementInstruction(StuxnetCutsceneObjectTypes objectType,
            string id, Vector2 newPosition, bool tweenMovement, float tweenDuration = 0f)
        {
            return new StuxnetCutsceneInstruction()
            {
                instructionType = InstructionTypes.Move,
                type = objectType.ToString(),
                typeID = id,
                newPosition = newPosition,
                tweenMovement = tweenMovement,
                TweenDuration = tweenDuration
            };
        }

        public static StuxnetCutsceneInstruction CreateInstantTransition(StuxnetCutsceneObjectTypes objectType,
            string id, bool transIn)
        {
            return new StuxnetCutsceneInstruction()
            {
                instructionType = transIn ? InstructionTypes.InstantIn : InstructionTypes.InstantOut,
                type = objectType.ToString(),
                typeID = id
            };
        }

        public static StuxnetCutsceneInstruction CreateResetInstruction()
        {
            return new StuxnetCutsceneInstruction()
            {
                instructionType = InstructionTypes.ResetCutscene
            };
        }

        public static StuxnetCutsceneInstruction CreateDelayEnding()
        {
            return new StuxnetCutsceneInstruction()
            {
                instructionType = InstructionTypes.DelayEnd
            };
        }

        public override string ToString()
        {
            StringBuilder response = new StringBuilder();

            switch(instructionType)
            {
                case InstructionTypes.Move:
                    response.Append("move ");
                    response.Append(type);
                    response.Append(" with id of ");
                    response.Append(typeID);
                    response.Append(" to new position at ");
                    response.Append($"{newPosition.X}, {newPosition.Y}");
                    response.Append(" and ");
                    response.Append(tweenMovement ? "do" : "do not");
                    response.Append(" tween movement. Delay: ");
                    response.Append($"{Delay}s");
                    return response.ToString();
                case InstructionTypes.InstantIn:
                    response.Append("instantly show ");
                    response.Append(type);
                    response.Append(" with id of ");
                    response.Append(typeID);
                    response.Append($" delay: {Delay}s");
                    return response.ToString();
                case InstructionTypes.InstantOut:
                    response.Append("instantly hide ");
                    response.Append(type);
                    response.Append(" with id of ");
                    response.Append(typeID);
                    response.Append($" delay: {Delay}s");
                    return response.ToString();
                case InstructionTypes.DelayEnd:
                    response.Append("delay ending of cutscene by ");
                    response.Append(Delay);
                    response.Append(" seconds");
                    return response.ToString();
            }

            return base.ToString();
        }
    }
}
