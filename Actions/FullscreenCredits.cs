using Hacknet;
using Hacknet.Extensions;
using Hacknet.Gui;
using Microsoft.Xna.Framework.Graphics;
using Pathfinder.Action;
using Pathfinder.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Stuxnet_HN.Extensions;

namespace Stuxnet_HN.Actions
{
    public class FullscreenCredits
    {
        public static string ExtensionFolder => ExtensionLoader.ActiveExtensionInfo.FolderPath + "/";

        public static bool IsActive { get; internal set; } = false;

        internal static string[] CreditsData;

        public class ShowFullscreenCredits : DelayablePathfinderAction
        {
            [XMLStorage]
            public string CreditsPath;

            public override void Trigger(OS os)
            {
                if (File.Exists(ExtensionFolder + CreditsPath))
                {
                    CreditsData = File.ReadAllLines(ExtensionFolder + CreditsPath);
                }

                IsActive = true;
            }
        }

        public const float CREDITS_LENGTH_SECONDS = 4.0f;
        public const float BASE_Y_OFFSET = 10;
        private static float YOffset = BASE_Y_OFFSET;
        private static float LastBaseYOffset = YOffset;

        public static Viewport Viewport
        {
            get
            {
                return GuiData.spriteBatch.GraphicsDevice.Viewport;
            }
        }

        public static void DrawFullscreenCredits()
        {
            YOffset = LastBaseYOffset;

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
                Vector2 pos = new(Viewport.Bounds.Center.X - (textVector.X / 2), YOffset);
                GuiData.font.DrawScaledText(text, pos, Color.White, 1.25f);
                YOffset += textVector.Y + 5;
            }

            void drawSmallFont(string text)
            {
                text = text.Substring(1);

                var textVector = GuiData.font.MeasureString(text);
                Vector2 pos = new(Viewport.Bounds.Center.X - (textVector.X / 2), YOffset);
                GuiData.font.DrawScaledText(text, pos, Color.White, 1);
                YOffset += textVector.Y + 5;
            }

            void drawDefaultFont(string text)
            {
                text = text.Substring(1);

                var textVector = GuiData.smallfont.MeasureString(text);
                Vector2 pos = new(Viewport.Bounds.Center.X - (textVector.X / 2), YOffset);
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
            ConditionManager.RegisterCondition<OnCreditsEnd>("OnFullscreenCreditsEnd");
        }
    }
}
