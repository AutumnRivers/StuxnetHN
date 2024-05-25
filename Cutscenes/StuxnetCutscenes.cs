using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Hacknet;
using Hacknet.Extensions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Stuxnet_HN.Cutscenes.Patches;

using Stuxnet_HN.Extensions;

namespace Stuxnet_HN.Cutscenes
{
    public class StuxnetCutscene
    {
        public string id;
        public string filepath;
        public string delayHostID = "delay";

        public Dictionary<string, Rectangle> rectangles = new Dictionary<string, Rectangle>();
        public Dictionary<string, StuxnetCutsceneImage> images = new Dictionary<string, StuxnetCutsceneImage>();

        public List<StuxnetCutsceneInstruction> instructions = new List<StuxnetCutsceneInstruction>();

        internal List<string> activeRectangles = new List<string>();
        internal List<string> activeImages = new List<string>();

        public StuxnetCutscene() { }

        public StuxnetCutscene(string filepath)
        {
            this.filepath = filepath;
        }

        public void ResetPositions()
        {
            if(rectangles.Any())
            {
                for (int i = 0; i < rectangles.Count; i++)
                {
                    var rect = rectangles.ElementAt(i);
                    Rectangle r = rect.Value;
                    r.X = 0;
                    r.Y = 0;
                    rectangles[rect.Key] = r;
                }
            }

            if(images.Any())
            {
                for (int i = 0; i < images.Count; i++)
                {
                    var img = images.ElementAt(i);
                    StuxnetCutsceneImage image = img.Value;
                    image.position.X = 0;
                    image.position.Y = 0;
                    image.currentRotation = 0;
                    images[img.Key] = image;
                }
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

        public void RegisterImage(string id, string imagePath, Vector2 size)
        {
            string extFolderPath = ExtensionLoader.ActiveExtensionInfo.FolderPath;
            FileStream imageStream = File.OpenRead(extFolderPath + "/" + imagePath);
            Texture2D image = Texture2D.FromStream(GuiData.spriteBatch.GraphicsDevice, imageStream);

            StuxnetCutsceneImage sImage = new StuxnetCutsceneImage()
            {
                image = image,
                position = new Vector2(0, 0),
                size = size
            };

            if(images.ContainsKey(id))
            {
                images[id] = sImage;
            } else
            {
                images.Add(id, sImage);
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
        public float currentRotation = 0f;
        public float opacity = 1.0f;

        public Vector2 GetCalculatedPosition()
        {
            Vector2 calcedPos = new Vector2
            {
                X = (float)Math.Floor(position.X - (size.X / 2)),
                Y = (float)Math.Floor(position.Y - (size.Y / 2))
            };
            return calcedPos;
        }

        public Vector2 GetCalculatedSize()
        {
            GraphicsDevice userGraphics = GuiData.spriteBatch.GraphicsDevice;
            Vector2 newSize = image.GetSizeAspect(size.X * userGraphics.Viewport.Width,
                size.Y * userGraphics.Viewport.Height);

            return newSize;
        }

        public void Resize(Vector2 newSize)
        {
            size = newSize;
        }
    }

    public class StuxnetCutsceneInstruction
    {
        public enum InstructionTypes
        {
            Move,
            Rotate,
            RotateForever,
            StopRotation,
            Tint,
            Resize,
            LockWidth,
            LockHeight,
            UnlockAspect,
            FadeIn,
            FadeOut,
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

        // Fade
        public float FadeDuration = 1.0f;

        // Movement Instructions
        public Vector2 newPosition;

        // Also applies to resizing
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

        // Rotation Instructions
        private float rotationAngle;
        public float RotationAngle
        {
            get
            {
                return rotationAngle;
            }
            set
            {
                if(value < 0f)
                {
                    rotationAngle = 0f;
                } else
                {
                    rotationAngle = value;
                }
            }
        }
        private float rotationTime = 0f;
        public float RotationTime
        {
            get { return rotationTime; }
            set
            {
                if(value < 0f)
                {
                    rotationTime = 0f;
                } else
                {
                    rotationTime = value;
                }
            }
        }
        public bool rotateClockwise = true;

        private float rotationSpeed = 1f;
        public float RotationSpeed
        {
            get { return rotationSpeed; }
            set
            {
                if(value < 0f)
                {
                    rotationSpeed = 0f;
                } else
                {
                    rotationSpeed = value;
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

        // Resize Instructions
        private Vector2 newSize;
        public Vector2 ResizeTo
        {
            get { return newSize; }
            set
            {
                Vector2 vec = new Vector2();

                if(value.X < 0) {
                    vec.X = 0;
                } else
                {
                    vec.X = value.X;
                }

                if(value.Y < 0)
                {
                    vec.Y = 0;
                } else
                {
                    vec.Y = value.Y;
                }

                newSize = vec;
            }
        }
        public bool maintainAspectRatio = true;

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
                    ExecuteInstantTrans(true);
                    break;
                case InstructionTypes.InstantOut:
                    ExecuteInstantTrans(false);
                    break;
                case InstructionTypes.Move:
                    ExecuteMovement();
                    break;
                case InstructionTypes.ResetCutscene:
                    ExecuteReset();
                    break;
                case InstructionTypes.Rotate:
                    ExecuteTimedRotation();
                    break;
                case InstructionTypes.RotateForever:
                    ExecuteInfiniteRotation();
                    break;
                case InstructionTypes.StopRotation:
                    ExecuteStopRotation();
                    break;
                case InstructionTypes.Resize:
                    ExecuteResize();
                    break;
                case InstructionTypes.FadeIn:
                    ExecuteFade(true);
                    break;
                case InstructionTypes.FadeOut:
                    ExecuteFade(false);
                    break;
            }
        }

        public void ExecuteFade(bool fadeIn)
        {
            CutsceneExecutor.AddFadeImage(typeID, fadeIn, FadeDuration);
        }

        public void ExecuteMovement()
        {
            StuxnetCutsceneObjectTypes type = StuxnetCutsceneObjectTypes.Rectangle;

            string objectType = this.type;
            string id = typeID;

            GraphicsDevice userGraphics = GuiData.spriteBatch.GraphicsDevice;

            Vector2 targetPos = new Vector2()
            {
                X = newPosition.X * userGraphics.Viewport.Width,
                Y = newPosition.Y * userGraphics.Viewport.Height
            };

            if (objectType == "Rectangle")
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
                } else if(type == StuxnetCutsceneObjectTypes.Image)
                {
                    StuxnetCutsceneImage image = cutscene.images[id];
                    image.position.X = targetPos.X;
                    image.position.Y = targetPos.Y;
                    cutscene.images[id] = image;
                }
            } else
            {
                if(type == StuxnetCutsceneObjectTypes.Rectangle)
                {
                    Rectangle rect = cutscene.rectangles[id];
                    CutsceneExecutor.AddTweenedRectangle(id, rect, targetPos, TweenDuration);
                } else if(type == StuxnetCutsceneObjectTypes.Image)
                {
                    StuxnetCutsceneImage image = cutscene.images[id];
                    CutsceneExecutor.AddTweenedImage(id, image, targetPos, TweenDuration);
                }
            }
        }

        public void ExecuteInstantTrans(bool activate = true)
        {
            string objectType = this.type;
            string id = typeID;

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

        public void ExecuteTimedRotation()
        {
            string id = typeID;

            CutsceneExecutor.AddTimedRotation(id, rotationAngle, rotationTime, rotateClockwise);
        }

        public void ExecuteInfiniteRotation()
        {
            string id = typeID;

            CutsceneExecutor.AddInfiniteRotation(id, rotationSpeed, rotateClockwise);
        }

        public void ExecuteStopRotation()
        {
            string id = typeID;

            if (CutsceneExecutor.targetRotations.FirstOrDefault(tp => tp.Item1 == id)
                == null)
            {
                Console.WriteLine(StuxnetCore.logPrefix + "WARN - Couldn't find image ID in targetRotations. Skipping...");
                return;
            }

            int index = CutsceneExecutor.targetRotations.FindIndex(tp => tp.Item1 == id);
            CutsceneExecutor.targetRotations.RemoveAt(index);
        }

        public void ExecuteResize()
        {
            string id = typeID;

            if(type == "Rectangle")
            {
                CutsceneExecutor.AddResizeRectangle(id, ResizeTo, maintainAspectRatio, tweenMovement, TweenDuration);
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
            CutsceneExecutor.targetVectorImgs.Clear();
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

        public static StuxnetCutsceneInstruction CreateRotationInstruction(string id, float targetAngle, float rotationTime = 0f,
            bool forever = false, float rotationForeverSpeed = 1f, bool clockwise = true)
        {
            return new StuxnetCutsceneInstruction()
            {
                instructionType = forever ? InstructionTypes.RotateForever : InstructionTypes.Rotate,
                type = "Image",
                typeID = id,
                RotationAngle = targetAngle,
                RotationTime = rotationTime,
                RotationSpeed = rotationForeverSpeed,
                rotateClockwise = clockwise
            };
        }

        public static StuxnetCutsceneInstruction CreateStopRotation(string id)
        {
            return new StuxnetCutsceneInstruction()
            {
                instructionType = InstructionTypes.StopRotation,
                type = "Image",
                typeID = id
            };
        }

        public static StuxnetCutsceneInstruction CreateResizeInstruction(StuxnetCutsceneObjectTypes objectType,
            string id, Vector2 newSize, bool maintainAspectRatio, float duration = 0)
        {
            return new StuxnetCutsceneInstruction()
            {
                instructionType = InstructionTypes.Resize,
                ResizeTo = newSize,
                type = objectType.ToString(),
                typeID = id,
                maintainAspectRatio = maintainAspectRatio,
                TweenDuration = duration,
                tweenMovement = duration > 0.01
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

        public static StuxnetCutsceneInstruction CreateFadeInstruction(StuxnetCutsceneObjectTypes objectType,
            string id, bool fadeIn, float duration = 1.0f)
        {
            return new StuxnetCutsceneInstruction()
            {
                instructionType = fadeIn ? InstructionTypes.FadeIn : InstructionTypes.FadeOut,
                type = objectType.ToString(),
                typeID = id,
                FadeDuration = duration
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
                case InstructionTypes.Rotate:
                    response.Append("rotate image with id of ");
                    response.Append(typeID);
                    response.Append(" to a target angle of ");
                    response.Append(rotationAngle);
                    response.Append(" degrees in ");
                    response.Append(rotationTime);
                    response.Append(" seconds. delay: ");
                    response.Append($"{Delay}s");
                    return response.ToString();
                case InstructionTypes.RotateForever:
                    response.Append("rotate image with id of ");
                    response.Append(typeID);
                    response.Append(" forever with a speed of ");
                    response.Append(rotationSpeed);
                    response.Append(". delay: ");
                    response.Append($"{Delay}s");
                    return response.ToString();
                case InstructionTypes.StopRotation:
                    response.Append("stop rotation of image with id of ");
                    response.Append(typeID);
                    response.Append($" delay: {Delay}s");
                    return response.ToString();
            }

            return base.ToString();
        }
    }
}
