using Hacknet;
using Hacknet.Effects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Stuxnet_HN.Extensions;
using System;
using System.Linq;
using System.Xml.Linq;

namespace Stuxnet_HN.Gui
{
    public class RaindropsEffectElement : AnimatedElement
    {
        public float FallRate { get; set; } = 0.5f;
        public float MaxVerticalLanding { get; set; } = 0.06f;
        public float MaxDropRadius { get; set; } = 5.0f;
        public float DropsPerSecond { get; set; } = 30f;
        public RaindropsEffect RaindropsEffect { get; private set; }

        private bool HasInitialized { get; set; } = false;

        public RaindropsEffectElement(string id) : base(id)
        {
            RaindropsEffect = new RaindropsEffect();
            Position = new(OnTranslationCompleted, Vector2.Zero);
            Size = new(OnResizeCompleted, new Vector2(1.0f, 1.0f));
        }

        public RaindropsEffectElement(string id, float fallRate, float maxDropRadius, float dropsPerSecond) : base(id)
        {
            RaindropsEffect = new RaindropsEffect();
            FallRate = fallRate;
            MaxDropRadius = maxDropRadius;
            DropsPerSecond = dropsPerSecond;
        }

        public RaindropsEffectElement(string id, float fallRate, float maxDropRadius, float dropsPerSecond,
            Color dropsColor) : base(id)
        {
            RaindropsEffect = new RaindropsEffect();
            FallRate = fallRate;
            MaxDropRadius = maxDropRadius;
            DropsPerSecond = dropsPerSecond;
            Color = dropsColor;
        }

        public void Initialize()
        {
            if (HasInitialized) return;
            RaindropsEffect.Init(OS.currentInstance.content);
            HasInitialized = true;
        }

        public override void Draw(Rectangle bounds)
        {
            if (!Visible) return;
            if (!HasInitialized) Initialize();
            RaindropsEffect.Render(bounds, GuiData.spriteBatch, Color * Opacity.Current, MaxDropRadius, 30f);
        }

        public override void Update(GameTime gameTime)
        {
            if (!HasInitialized) Initialize();
            RaindropsEffect.Update((float)gameTime.ElapsedGameTime.TotalSeconds, DropsPerSecond);
        }

        public override string XmlName => "RaindropsFX";
    }

    public class AnimatedGridEffectElement : AnimatedElement
    {
        public ShiftingGridEffect GridEffect { get; private set; }

        public AnimatedGridEffectElement(string id) : base(id)
        {
            GridEffect = new();
        }

        public AnimatedGridEffectElement(string id, Vector2 startingPosition, Vector2 startingSize, Color color) :
            base(id, startingPosition, startingSize, color)
        {
            GridEffect = new();
        }

        public override void Draw(Rectangle bounds)
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
            Color lightColor = Color.Lerp(Color, Color.White, 0.35f);
            Color darkColor = Color.Lerp(Color, Color.Black, 0.35f);
            GridEffect.RenderGrid(targetRect, GuiData.spriteBatch, lightColor, Color, darkColor, true);
        }
    }

    public class AnimatedTextElement : AnimatedElement
    {
        public AnimatedProperty<string> Text { get; private set; }
        public AnimatedProperty<float> FontScale { get; private set; }
        public SpriteFont Font
        {
            get { return GuiData.font; }
        }

        public override string XmlName => "Text";

        public AnimatedTextElement(string id) : base(id)
        {
            Text = new(OnTypewriterCompleted, "Hello, World!");
            FontScale = new(OnScaleCompleted, 1.0f);
        }

        public AnimatedTextElement(string id, Vector2 startingPosition, Vector2 startingSize,
            Color textColor, string text, float fontScale)
            : base(id, startingPosition, startingSize, textColor)
        {
            Text = new(OnTypewriterCompleted, text);
            FontScale = new(OnScaleCompleted, fontScale);
        }

        public override void Draw(Rectangle bounds)
        {
            var textToShow = GetTypewriterValue();
            var textVector = Font.MeasureString(textToShow) * FontScale.Current;

            Vector2 parsedCurrent = ParseVector2Multipliers(Position.Current);
            Vector2 parsedSize = ParseVector2Multipliers(Size.Current);

            float x = bounds.X + parsedCurrent.X - (textVector.X / 2);
            float y = bounds.Y + parsedCurrent.Y - (textVector.Y / 2);

            textToShow = Utils.SuperSmartTwimForWidth(textToShow, (int)parsedSize.X, Font);

            GuiData.spriteBatch.DrawString(Font, textToShow, new(x, y), Color * Opacity.Current);
        }

        public void OnTypewriterCompleted()
        {
            Text.Origin = Text.Target;
            Text.AnimationDuration = 0.0f;
            Text.AnimationProgress = 0.0f;
        }

        public void OnScaleCompleted()
        {
            FontScale.Origin = FontScale.Current = FontScale.Target;
            FontScale.AnimationDuration = 0.0f;
            FontScale.AnimationProgress = 0.0f;
        }

        public string GetTypewriterValue()
        {
            var targetChars = Text.Target.ToCharArray();
            int charsToShow = (int)Math.Floor(MathHelper.Lerp(0, targetChars.Length, Text.AnimationProgress));

            var displayChars = targetChars.Take(charsToShow).ToArray();
            return new string(displayChars);
        }

        public void InitiateTypewriter(string target, float duration)
        {
            OnInitiateAnything();
            Text.AnimationDuration = duration;
            Text.AnimationProgress = 0.0f;
            if (duration <= 0)
            {
                Text.Current = Text.Origin = Text.Target = target;
                return;
            }

            if(string.IsNullOrWhiteSpace(target))
            {
                Text.Origin = Text.Current;
                Text.Target = "";
            } else
            {
                Text.Origin = Text.Current = "";
                Text.Target = target;
            }
        }

        public void InitiateFontScale(float target, float duration)
        {
            OnInitiateAnything();
            FontScale.AnimationDuration = duration;
            FontScale.AnimationProgress = 0.0f;
            if(duration <= 0)
            {
                FontScale.Current = FontScale.Origin = FontScale.Target = target;
                return;
            }

            FontScale.Current = FontScale.Origin = FontScale.Target;
            FontScale.Target = target;
        }
    }

    public class TypewriterInstruction : AnimatedElementInstruction
    {
        public string TargetText { get; set; }

        public TypewriterInstruction(AnimatedElement element, XElement inst) : base(element, inst)
        {
            Element = element;
            LoadFromXml(inst);
        }

        public TypewriterInstruction(AnimatedElement element, float duration, float delay, string targetText) :
            base(element, duration, delay)
        {
            TargetText = targetText;
        }

        public override void Activate()
        {
            if(Element is AnimatedTextElement animatedText)
            {
                animatedText.InitiateTypewriter(TargetText, Duration);
            } else
            {
                StuxnetCore.Logger.LogError("Tried to use TypewriterInstruction on an element that is not " +
                    "a text element!");
            }
        }

        public override void LoadFromXml(XElement rootElement)
        {
            base.LoadFromXml(rootElement);
            if(rootElement.TryGetAttribute("Text", out string target))
            {
                TargetText = target;
            } else
            {
                throw new FormatException("Typewriter instruction is missing Text attribute!");
            }
        }
    }

    public class FontScaleInstruction : AnimatedElementInstruction
    {
        public float TargetScale { get; set; }

        public FontScaleInstruction(AnimatedElement element, XElement inst) : base(element, inst)
        {
            LoadFromXml(inst);
        }

        public override void Activate()
        {
            if (Element is AnimatedTextElement animatedText)
            {
                animatedText.InitiateFontScale(TargetScale, Duration);
            }
            else
            {
                StuxnetCore.Logger.LogError("Tried to use FontScaleInstruction on an element that is not " +
                    "a text element!");
            }
        }

        public override void LoadFromXml(XElement rootElement)
        {
            base.LoadFromXml(rootElement);
            if (rootElement.TryGetAttribute("TargetScale", out string target))
            {
                TargetScale = float.Parse(target);
            }
            else
            {
                throw new FormatException("FontScale instruction is missing TargetScale attribute!");
            }
        }
    }
}
