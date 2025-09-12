using Hacknet;
using Hacknet.Gui;
using Pathfinder.Action;
using Pathfinder.Util;
using System.IO;
using Microsoft.Xna.Framework;
using Stuxnet_HN.Extensions;
using Pathfinder.GUI;

namespace Stuxnet_HN.Actions
{
    public class FullscreenCredits
    {
        public static bool IsActive { get; internal set; } = false;

        internal static string[] CreditsData;

        public class ShowFullscreenCredits : DelayablePathfinderAction
        {
            [XMLStorage]
            public string CreditsPath;

            [XMLStorage]
            public float ShowButtonDelay = -1;

            public override void Trigger(OS os)
            {
                if (File.Exists(Utils.GetFileLoadPrefix() + CreditsPath))
                {
                    CreditsData = File.ReadAllLines(Utils.GetFileLoadPrefix() + CreditsPath);
                }

                IsActive = true;
            }
        }

        public class HideFullscreenCredits : DelayablePathfinderAction
        {
            public override void Trigger(OS os)
            {
                IsActive = false;
                CreditsData = null;
            }
        }

        public const float CREDITS_LENGTH_SECONDS = 4.0f;
        public const float BASE_Y_OFFSET = 10;
        private static float YOffset = BASE_Y_OFFSET;
        private static float LastBaseYOffset = YOffset;
        private static float _exitButtonDelay = -1;
        private static float Lifetime = 0;
        internal static bool IsNewCredits = true;

        private static int ExitButtonID = PFButton.GetNextID();

        public static void DrawFullscreenCredits()
        {
            YOffset = LastBaseYOffset;
            Rectangle fullscreen = OS.currentInstance.fullscreen;

            if(Lifetime >= _exitButtonDelay)
            {
                int width = fullscreen.Width / 6;
                int height = fullscreen.Height / 15;
                bool hideScreen = Button.doButton(ExitButtonID,
                    fullscreen.Center.X - (width / 2),
                    fullscreen.Y + fullscreen.Height - height - 15,
                    width, height, "Continue...",
                    OS.currentInstance.unlockedColor);
                if(hideScreen)
                {
                    IsActive = false;
                    CreditsData = null;
                    return;
                }
            }
            Lifetime *= (float)OS.currentInstance.lastGameTime.ElapsedGameTime.TotalSeconds;

            for(int lineIdx = 0; lineIdx < CreditsData.Length; lineIdx++)
            {
                var line = CreditsData[lineIdx];

                switch (line[0])
                {
                    case '%':
                        drawBigFont(line);
                        break;
                    case '^':
                        drawSmallFont(line);
                        break;
                    default:
                        drawDefaultFont(line);
                        break;
                }

                if(lineIdx == CreditsData.Length - 1 && YOffset <= -50)
                {
                    IsActive = false;
                }
            }

            LastBaseYOffset -= (float)OS.currentInstance.lastGameTime.ElapsedGameTime.TotalSeconds * CREDITS_LENGTH_SECONDS;

            void drawBigFont(string text)
            {
                text = text.Substring(1);

                var textVector = GuiData.font.MeasureString(text) * 1.25f;
                Vector2 pos = new(OS.currentInstance.fullscreen.Center.X - (textVector.X / 2), YOffset);
                GuiData.font.DrawScaledText(text, pos, Color.White, 1.25f);
                YOffset += textVector.Y + 5;
            }

            void drawSmallFont(string text)
            {
                text = text.Substring(1);

                var textVector = GuiData.font.MeasureString(text);
                Vector2 pos = new(OS.currentInstance.fullscreen.Center.X - (textVector.X / 2), YOffset);
                GuiData.font.DrawScaledText(text, pos, Color.White, 1);
                YOffset += textVector.Y + 5;
            }

            void drawDefaultFont(string text)
            {
                text = text.Substring(1);

                var textVector = GuiData.smallfont.MeasureString(text);
                Vector2 pos = new(OS.currentInstance.fullscreen.Center.X - (textVector.X / 2), YOffset);
                GuiData.smallfont.DrawScaledText(text, pos, Color.White, 1);
                YOffset += textVector.Y + 5;
            }
        }

        public class OnCreditsEnd : StuxnetConditional
        {
            private bool HasBeenActiveBefore = false;

            public override bool Check(object os_obj)
            {
                if (!HasBeenActiveBefore && IsActive) HasBeenActiveBefore = true;

                return HasBeenActiveBefore && !IsActive;
            }
        }

        public static void RegisterActions()
        {
            ActionManager.RegisterAction<ShowFullscreenCredits>("ShowFullscreenCredits");
            ActionManager.RegisterAction<HideFullscreenCredits>("HideFullscreenCredits");
            ConditionManager.RegisterCondition<OnCreditsEnd>("OnFullscreenCreditsEnd");
        }
    }
}
