using Hacknet;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Stuxnet_HN.Extensions;
using System;
using System.Collections.Generic;
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

        public T Initial { get; private set; }

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
            Initial = initialValue;
            _onCompleted = onCompleted;
        }

        public void Reset()
        {
            Origin = Current = Target = Initial;
            AnimationDuration = 0.0f;
            AnimationProgress = 0.0f;
        }
    }

    public class AnimatedElement : IUpdateableGuiElement
    {
        public string ID { get; private set; }

        public AnimatedProperty<Vector2> Position { get; protected set; }
        public AnimatedProperty<Vector2> Size { get; protected set; }
        public AnimatedProperty<float> Rotation { get; protected set; }
        public AnimatedProperty<float> Opacity { get; protected set; }

        public bool RotateIndefinitely = false;
        public float RotationSpeed = 360.0f; // angles per second

        public Texture2D Image;
        public string ImagePath = string.Empty;
        public Color Color = Color.White;

        public virtual string XmlName => null;

        public bool Visible { get; set; } = false;

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
            Position = new(OnTranslationCompleted, startingPosition);
            Size = new(OnResizeCompleted, startingSize);
            Rotation = new(OnRotationCompleted, 0);
            Opacity = new(OnFadeCompleted, 1);

            Color = color;
        }

        public AnimatedElement(string id, Vector2 startingPosition, Vector2 startingSize, float rotation, Color color)
        {
            ID = id;
            Position = new(OnTranslationCompleted, startingPosition);
            Size = new(OnResizeCompleted, startingSize);
            Rotation = new(OnRotationCompleted, rotation);
            Opacity = new(OnFadeCompleted, 1);

            Color = color;
        }

        public AnimatedElement(string id, Vector2 startingPosition, Vector2 startingSize, string imageFilepath, float rotation = 0f)
        {
            ID = id;
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

        public virtual void Draw(Rectangle bounds)
        {
            if (!Visible) return;
            var position = ParseVector2Multipliers(Position.Current);
            var size = ParseVector2Multipliers(Size.Current);
            Rectangle targetRect = new()
            {
                X = (int)position.X + bounds.X,
                Y = (int)position.Y + bounds.Y,
                Width = (int)size.X,
                Height = (int)size.Y
            };
            float rotation = MathHelper.ToRadians(Rotation.Current);
            DrawDynamicRectangle(targetRect, Color, rotation, Image);
        }

        public virtual void Update(GameTime gameTime)
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

        public void OnInitiateAnything()
        {
            if (!Visible) Visible = true;
        }

        public void ToggleVisibility(bool visible)
        {
            Visible = visible;
        }

        public void InitiateTranslation(Vector2 newPosition, float duration = 1.0f)
        {
            OnInitiateAnything();

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
            OnInitiateAnything();

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
            OnInitiateAnything();

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
            OnInitiateAnything();

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
            OnInitiateAnything();

            if (fadeIn)
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

        public void Reset()
        {
            Visible = false;
            Position.Reset();
            Size.Reset();
            Rotation.Reset();
            Opacity.Reset();
        }

        private void SetInitialRotation(float rotation)
        {
            Rotation.Origin = Rotation.Target = Rotation.Current = rotation;
        }

        protected virtual void Register(XElement element)
        {
            throw new NotImplementedException();
        }

        public static List<Type> CustomElements = new();

        public static AnimatedElement LoadFromXml(XElement xml)
        {
            AnimatedElement element;
            List<string> attributes = new()
            {
                "ID", "Position", "Size"
            };
            string[] attrValues;
            if(!CustomElements.Any(t => t.IsSubclassOf(typeof(AnimatedElement)) && t.Name == xml.Name.LocalName))
            {
                var customType = CustomElements.Find(t => t.BaseType.Name == "AnimatedElement" && t.Name == xml.Name.LocalName);
                element = Activator.CreateInstance(customType, xml.GetAttributeValues(attributes)[0]) as AnimatedElement;
                element.Register(xml);
                return element;
            }
            switch(xml.Name.LocalName)
            {
                case "Rectangle":
                    attributes.Add("Color");
                    attrValues = xml.GetAttributeValues(attributes);
                    element = new(attrValues[0],
                        GetVec2FromString(attrValues[1]),
                        GetVec2FromString(attrValues[2]),
                        Utils.convertStringToColor(attrValues[3]));
                    if(xml.Attributes().Any(a => a.Name == "Rotation"))
                    {
                        element.SetInitialRotation(float.Parse(xml.Attribute("Rotation").Value));
                    }
                    break;
                case "Image":
                    attributes.Add("FilePath");
                    attrValues = xml.GetAttributeValues(attributes);
                    element = new(attrValues[0],
                        GetVec2FromString(attrValues[1]),
                        GetVec2FromString(attrValues[2]),
                        attrValues[3]);
                    if (xml.Attributes().Any(a => a.Name == "Rotation"))
                    {
                        element.SetInitialRotation(float.Parse(xml.Attribute("Rotation").Value));
                    }
                    break;
                case "RaindropsFX":
                    attributes = new()
                    {
                        "ID", "FallRate", "MaxDropsRadius", "DropsPerSecond", "DropsColor"
                    };
                    attrValues = xml.GetAttributeValues(attributes);
                    element = new RaindropsEffectElement(
                        attrValues[0],
                        float.Parse(attrValues[1]),
                        float.Parse(attrValues[2]),
                        float.Parse(attrValues[3])
                        );
                    if (attrValues[4] != null)
                    {
                        element.Color = Utils.convertStringToColor(attrValues[4]);
                    }
                    break;
                case "Text":
                    attributes[2] = "StartingValue";
                    attributes.Add("FontScale");
                    attributes.Add("Color");
                    attrValues = xml.GetAttributeValues(attributes);
                    element = new AnimatedTextElement(
                        attrValues[0],
                        GetVec2FromString(attrValues[1]),
                        Vector2.Zero,
                        Utils.convertStringToColor(attrValues[4]),
                        attrValues[2],
                        float.Parse(attrValues[3])
                        );
                    break;
                case "GridFX":
                    attributes.Add("Color");
                    attrValues = xml.GetAttributeValues(attributes);
                    element = new AnimatedGridEffectElement(
                        attrValues[0],
                        GetVec2FromString(attrValues[1]),
                        GetVec2FromString(attrValues[2]),
                        Utils.convertStringToColor(attrValues[3])
                        );
                    break;
                default:
                    throw new FormatException("Invalid animated element in file!");
            }
            return element;
        }

        public static Vector2 ParseVector2Multipliers(Vector2 vector)
        {
            Vector2 result = new(vector.X, vector.Y);

            Rectangle fullscreen = OS.currentInstance.fullscreen;
            if ((result.Y < 1.0f && result.Y > 0) || (result.Y > -1.0f && result.Y < 0))
            {
                result.Y *= fullscreen.Height;
            }
            if(result.X == default)
            {
                result.X = result.Y;
            }
            if ((result.X < 1.0f && result.X > 0) || (result.X > -1.0f && result.X < 0))
            {
                result.X *= fullscreen.Width;
            }

            return result;
        }

        public static Vector2 GetVec2FromString(string vec2string)
        {
            float x = default;
            float y;
            if(vec2string.Contains(","))
            {
                var vec2split = vec2string.Split(',');
                x = float.Parse(vec2split[0]);
                y = float.Parse(vec2split[1]);
            } else
            {
                y = float.Parse(vec2string);
            }
            return new(x, y);
        }
    }

    public abstract class AnimatedElementInstruction
    {
        public float Duration { get; set; } = 0f;
        public float DelayFromStart { get; set; } = 0f;
        public bool Activated { get; set; } = false;

        public virtual bool NeedsElement => true;

        public AnimatedElement Element;

        public AnimatedElementInstruction() { }

        public AnimatedElementInstruction(AnimatedElement element, XElement xml)
        {
            if(NeedsElement && element == null)
            {
                throw new FormatException("Instruction needs an element -- did you forget the ElementID attribute?");
            }

            LoadFromXml(xml);
            Element = element;
        }

        public AnimatedElementInstruction(AnimatedElement element, float duration, float delay)
        {
            if (NeedsElement && element == null)
            {
                throw new FormatException("Instruction needs an element!");
            }

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

    public class ToggleVisibilityInstruction : AnimatedElementInstruction
    {
        public bool IsVisible { get; set; } = false;

        public ToggleVisibilityInstruction(AnimatedElement element, XElement xml) : base(element, xml) { }

        public ToggleVisibilityInstruction(AnimatedElement element, float delay, bool visible) :
            base(element, 0f, delay)
        {
            IsVisible = visible;
        }

        public override void Activate()
        {
            Element.ToggleVisibility(IsVisible);
        }

        public override void LoadFromXml(XElement rootElement)
        {
            DelayFromStart = float.Parse(rootElement.Attribute("Delay").Value);
            IsVisible = bool.Parse(rootElement.Attribute("Visible").Value);
        }
    }

    public class TranslateElementInstruction : AnimatedElementInstruction
    {
        public Vector2 TargetPosition { get; set; }

        public TranslateElementInstruction(AnimatedElement element, XElement xml) : base(element, xml) { }

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
