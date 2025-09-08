using BepInEx;
using Hacknet;
using Hacknet.Gui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathfinder.Util.XML;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using static Stuxnet_HN.Extensions.GuiHelpers;

namespace Stuxnet_HN.Gui
{
    public class AnimatedProperty<T>
    {
        private float _duration = 1.0f;
        private float _progress = 0.0f;
        private readonly Action _onCompleted;

        public T Origin { get; set; }
        public T Current { get; set; }
        public T Target { get; set; }

        public float AnimationDuration
        {
            get => _duration;
            set => _duration = Math.Max(0.1f, value);
        }

        public float AnimationProgress
        {
            get => _progress;
            set
            {
                if (value >= 1.0f)
                {
                    _progress = 0.0f;
                    _onCompleted?.Invoke();
                    return;
                }
                _progress = MathHelper.Clamp(value, 0.0f, 1.0f);
            }
        }

        public AnimatedProperty(Action onCompleted, T initialValue)
        {
            Origin = initialValue;
            Current = initialValue;
            Target = initialValue;
            _onCompleted = onCompleted;
        }
    }

    public class AnimatedElement : IUpdateableGuiElement
    {
        public string ID { get; private set; }

        public AnimatedProperty<Vector2> Position { get; private set; }
        public AnimatedProperty<Vector2> Size { get; private set; }
        public AnimatedProperty<float> Rotation { get; private set; }
        public AnimatedProperty<float> Opacity { get; private set; }

        public bool RotateIndefinitely = false;
        public float RotationSpeed = 360.0f; // angles per second

        public Texture2D Image;
        public string ImagePath = string.Empty;
        public Color Color = Color.White;

        public bool NeedsRotation
        {
            get
            {
                return RotateIndefinitely || Rotation.Current != Rotation.Target;
            }
        }

        public bool NeedsTranslation
        {
            get
            {
                return Position.Current != Position.Target;
            }
        }

        public bool NeedsResize
        {
            get
            {
                return Size.Current != Size.Target;
            }
        }

        private Vector2 GetParsedVector2(Vector2 vector2)
        {
            if(vector2.X < 1.0f && vector2.X > -1.0f)
            {
                vector2.X *= OS.currentInstance.fullscreen.Width;
            }
            if(vector2.Y < 1.0f && vector2.Y > -1.0f)
            {
                vector2.Y *= OS.currentInstance.fullscreen.Height;
            }
            return vector2;
        }

        internal AnimatedElement DeepCopy()
        {
            if(!ImagePath.IsNullOrWhiteSpace())
            {
                return new AnimatedElement(ID, Position.Current, Size.Current, ImagePath, Rotation.Current);
            } else
            {
                return new AnimatedElement(ID, Position.Current, Size.Current, Rotation.Current, Color);
            }
        }

        public AnimatedElement(string id)
        {
            ID = id;

            Position = new(OnTranslationCompleted, Vector2.Zero);
            Size = new(OnResizeCompleted, Vector2.Zero);
            Rotation = new(OnRotationCompleted, 0);
            Opacity = new(OnFadeCompleted, 1);
        }

        public AnimatedElement(string id, Vector2 startingPosition, Vector2 startingSize, Color color)
        {
            ID = id;

            startingPosition = GetParsedVector2(startingPosition);
            startingSize = GetParsedVector2(startingSize);

            Position = new(OnTranslationCompleted, startingPosition);
            Size = new(OnResizeCompleted, startingSize);
            Rotation = new(OnRotationCompleted, 0);
            Opacity = new(OnFadeCompleted, 1);

            Color = color;
        }

        public AnimatedElement(string id, Vector2 startingPosition, Vector2 startingSize, float rotation, Color color)
        {
            ID = id;

            startingPosition = GetParsedVector2(startingPosition);
            startingSize = GetParsedVector2(startingSize);

            Position = new(OnTranslationCompleted, startingPosition);
            Size = new(OnResizeCompleted, startingSize);
            Rotation = new(OnRotationCompleted, rotation);
            Opacity = new(OnFadeCompleted, 1);

            Color = color;
        }

        public AnimatedElement(string id, Vector2 startingPosition, Vector2 startingSize, string imageFilepath, float rotation = 0f)
        {
            ID = id;

            startingPosition = GetParsedVector2(startingPosition);
            startingSize = GetParsedVector2(startingSize);

            Position = new(OnTranslationCompleted, startingPosition);
            Size = new(OnResizeCompleted, startingSize);
            Rotation = new(OnRotationCompleted, rotation);
            Opacity = new(OnFadeCompleted, 1);

            imageFilepath = Utils.GetFileLoadPrefix() + imageFilepath;
            var imgStream = File.OpenRead(imageFilepath);
            Image = Texture2D.FromStream(GuiData.spriteBatch.GraphicsDevice, imgStream);
            imgStream.Close();
            ImagePath = imageFilepath;
        }

        public void Draw(Rectangle bounds)
        {
            Rectangle targetRect = new()
            {
                X = (int)Position.Current.X + bounds.X,
                Y = (int)Position.Current.Y + bounds.Y,
                Width = (int)Size.Current.X,
                Height = (int)Size.Current.Y
            };
            float rotation = MathHelper.ToRadians(Rotation.Current);
            DrawDynamicRectangle(targetRect, Color, rotation, Image);
        }

        public void Update(GameTime gameTime)
        {
            float t = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (NeedsTranslation)
            {
                Position.AnimationProgress += t / Position.AnimationDuration;
                Position.Current = Vector2.Lerp(Position.Origin, Position.Target, Position.AnimationProgress);
            }

            if (NeedsRotation)
            {
                if (RotateIndefinitely)
                {
                    Rotation.Current += t * RotationSpeed;
                }
                else
                {
                    Rotation.AnimationProgress += t / Rotation.AnimationDuration;
                    Rotation.Current = MathHelper.Lerp(Rotation.Origin, Rotation.Target, Rotation.AnimationProgress);
                }
            }

            if (NeedsResize)
            {
                Size.AnimationProgress += t / Size.AnimationDuration;
            }
        }

        public void InitiateTranslation(Vector2 newPosition, float duration = 1.0f)
        {
            if(OS.DEBUG_COMMANDS && StuxnetCore.Configuration.ShowDebugText)
            {
                StuxnetCore.Logger.LogDebug("Initiated translation on " + ID);
                StuxnetCore.Logger.LogDebug("New target position: " + newPosition.X + "," + newPosition.Y);
            }

            if (duration > 0f)
            {
                Position.Origin = Position.Current;
                Position.Target = newPosition;
                Position.AnimationDuration = duration;
                Position.AnimationProgress = 0.0f;
            }
            else
            {
                Position.Target = Position.Origin = Position.Current = newPosition;
                Position.AnimationProgress = 0.0f;
            }
        }

        public void InitiateResize(Vector2 newSize, float duration = 1.0f)
        {
            if (duration > 0f)
            {
                Size.Target = newSize;
                Size.Origin = Size.Current;
                Size.AnimationDuration = duration;
                Size.AnimationProgress = 0.0f;
            }
            else
            {
                Size.Target = Size.Origin = Size.Current = newSize;
                Size.AnimationProgress = 0.0f;
            }
        }

        public void InitiateRotation(float newRotation, float duration = 1.0f)
        {
            if (OS.DEBUG_COMMANDS && StuxnetCore.Configuration.ShowDebugText)
            {
                StuxnetCore.Logger.LogDebug("Initiated rotation on " + ID);
                StuxnetCore.Logger.LogDebug("Target angle (deg): " + newRotation);
            }

            if (duration > 0f)
            {
                Rotation.Target = newRotation;
                Rotation.Origin = Rotation.Current;
                Rotation.AnimationDuration = duration;
                Rotation.AnimationProgress = 0.0f;
            }
            else
            {
                Rotation.Target = Rotation.Origin = Rotation.Current = newRotation;
                Rotation.AnimationProgress = 0.0f;
            }
        }

        public void InitiateRotation(bool forever, float speed = 360.0f)
        {
            if (speed == 0f) speed = 1.0f;

            RotateIndefinitely = forever;
            RotationSpeed = speed;

            if (RotateIndefinitely)
            {
                Rotation.AnimationProgress = Rotation.AnimationDuration = 0.0f;
                Rotation.Origin = Rotation.Target = Rotation.Current;
            }
        }

        public void InitiateFade(bool fadeIn, float duration = 1.0f)
        {
            if(fadeIn)
            {
                Opacity.Target = 1.0f;
            } else
            {
                Opacity.Target = 0.0f;
            }
            Opacity.AnimationDuration = duration;
            Opacity.AnimationProgress = 0.0f;
        }

        public void OnRotationCompleted()
        {
            Rotation.Origin = Rotation.Current;
            Rotation.AnimationProgress = 0.0f;
        }

        public void OnTranslationCompleted()
        {
            Position.Origin = Position.Current;
            Position.AnimationProgress = 0.0f;
        }

        public void OnResizeCompleted()
        {
            Size.Origin = Size.Current;
            Size.AnimationProgress = 0.0f;
        }

        public void OnFadeCompleted()
        {
            Opacity.AnimationProgress = 0.0f;
        }

        private void SetInitialRotation(float rotation)
        {
            Rotation.Origin = Rotation.Target = Rotation.Current = rotation;
        }

        public static AnimatedElement LoadFromXml(XElement xml)
        {
            AnimatedElement element;
            switch(xml.Name.LocalName)
            {
                case "Rectangle":
                    element = new(xml.Attribute("ID").Value,
                        GetVec2FromString(xml.Attribute("Position").Value),
                        GetVec2FromString(xml.Attribute("Size").Value),
                        Utils.convertStringToColor(xml.Attribute("Color").Value));
                    if(xml.Attributes().Any(a => a.Name == "Rotation"))
                    {
                        element.SetInitialRotation(float.Parse(xml.Attribute("Rotation").Value));
                    }
                    break;
                case "Image":
                    element = new(xml.Attribute("ID").Value,
                        GetVec2FromString(xml.Attribute("Position").Value),
                        GetVec2FromString(xml.Attribute("Size").Value),
                        xml.Attribute("FilePath").Value);
                    if (xml.Attributes().Any(a => a.Name == "Rotation"))
                    {
                        element.SetInitialRotation(float.Parse(xml.Attribute("Rotation").Value));
                    }
                    break;
                default:
                    throw new FormatException("Invalid animated element in file!");
            }
            return element;
        }

        public static Vector2 GetVec2FromString(string vec2string)
        {
            var vec2split = vec2string.Split(',');
            float x = float.Parse(vec2split[0]);
            float y = float.Parse(vec2split[1]);
            Rectangle fullscreen = OS.currentInstance.fullscreen;
            if((x < 1.0f && x > 0) || (x > -1.0f && x < 0))
            {
                x *= fullscreen.Width;
            }
            if ((y < 1.0f && y > 0) || (y > -1.0f && y < 0))
            {
                y *= fullscreen.Height;
            }
            return new(x, y);
        }
    }

    public abstract class AnimatedElementInstruction
    {
        public float Duration { get; set; }
        public float DelayFromStart { get; set; }
        public bool Activated { get; set; } = false;

        public AnimatedElement Element;

        public AnimatedElementInstruction(AnimatedElement element, XElement xml)
        {
            LoadFromXml(xml);
            Element = element;
        }

        public AnimatedElementInstruction(AnimatedElement element, float duration, float delay)
        {
            Element = element;
            Duration = duration;
            DelayFromStart = delay;
        }
        
        public static int CompareByDelay(AnimatedElementInstruction inst1, AnimatedElementInstruction inst2)
        {
            return inst1.DelayFromStart.CompareTo(inst2.DelayFromStart);
        }

        public abstract void Activate();

        public virtual void LoadFromXml(XElement rootElement)
        {
            Duration = float.Parse(rootElement.Attribute("Duration").Value);
            DelayFromStart = float.Parse(rootElement.Attribute("Delay").Value);
        }
    }

    public class TranslateElementInstruction : AnimatedElementInstruction
    {
        public Vector2 TargetPosition { get; set; }

        public TranslateElementInstruction(AnimatedElement element, XElement xml) : base(element, xml)
        {
            LoadFromXml(xml);
        }

        public TranslateElementInstruction(AnimatedElement element, float duration, float delay, Vector2 position)
            : base(element, duration, delay)
        {
            TargetPosition = position;
        }

        public override void Activate()
        {
            Activated = true;
            Element.InitiateTranslation(TargetPosition, Duration);
        }

        public override void LoadFromXml(XElement rootElement)
        {
            base.LoadFromXml(rootElement);
            TargetPosition = AnimatedElement.GetVec2FromString(rootElement.Attribute("TargetPosition").Value);
        }
    }

    public class ResizeElementInstruction : AnimatedElementInstruction
    {
        public Vector2 TargetSize { get; set; }

        public ResizeElementInstruction(AnimatedElement element, XElement xml) : base(element, xml)
        {
            LoadFromXml(xml);
        }

        public ResizeElementInstruction(AnimatedElement element, float duration, float delay, Vector2 size)
            : base(element, duration, delay)
        {
            TargetSize = size;
        }

        public override void Activate()
        {
            Activated = true;
            Element.InitiateResize(TargetSize, Duration);
        }

        public override void LoadFromXml(XElement rootElement)
        {
            base.LoadFromXml(rootElement);
            TargetSize = AnimatedElement.GetVec2FromString(rootElement.Attribute("TargetSize").Value);
        }
    }

    public class FadeElementInstruction : AnimatedElementInstruction
    {
        public bool FadeIn { get; set; } = false;

        public FadeElementInstruction(AnimatedElement element, XElement xml) : base(element, xml)
        {
            LoadFromXml(xml);
        }

        public FadeElementInstruction(AnimatedElement element, float duration, float delay, bool fadeIn)
            : base(element, duration, delay)
        {
            FadeIn = fadeIn;
        }

        public override void Activate()
        {
            Activated = true;
        }

        public override void LoadFromXml(XElement rootElement)
        {
            base.LoadFromXml(rootElement);
            FadeIn = bool.Parse(rootElement.Attribute("FadeIn").Value);
        }
    }

    public class RotateElementInstruction : AnimatedElementInstruction
    {
        public float TargetRotation { get; set; }
        public bool Indefinitely { get; set; } = false;
        public bool StopIndefiniteRotation { get; set; } = false;
        public float RotationSpeed { get; set; } = 0.0f;

        public RotateElementInstruction(AnimatedElement element, XElement xml) : base(element, xml)
        {
            LoadFromXml(xml);
        }

        public RotateElementInstruction(AnimatedElement element, float duration, float delay, float targetAngle)
            : base(element, duration, delay)
        {
            TargetRotation = targetAngle;
        }

        public RotateElementInstruction(AnimatedElement element, float duration, float delay, bool indefinitely, float speed)
            : base(element, duration, delay)
        {
            Indefinitely = indefinitely;
            RotationSpeed = speed;
        }

        public override void Activate()
        {
            Activated = true;
            if (Indefinitely)
            {
                Element.InitiateRotation(true, RotationSpeed);
            } else if(StopIndefiniteRotation)
            {
                Element.InitiateRotation(false);
            } else
            {
                Element.InitiateRotation(TargetRotation, Duration);
            }
        }

        public override void LoadFromXml(XElement rootElement)
        {
            base.LoadFromXml(rootElement);
            if(rootElement.Attributes().Any(a => a.Name == "Indefinitely"))
            {
                Indefinitely = bool.Parse(rootElement.Attribute("Indefinitely").Value);
                RotationSpeed = float.Parse(rootElement.Attribute("Speed").Value);
            } else
            {
                TargetRotation = float.Parse(rootElement.Attribute("TargetAngle").Value);
            }
        }
    }
}
