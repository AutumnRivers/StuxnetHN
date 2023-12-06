using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BepInEx;

using Hacknet;
using Hacknet.Gui;

using HarmonyLib;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using States = Stuxnet_HN.Static.States.IllustratorStates;

namespace Stuxnet_HN.Patches
{
    [HarmonyPatch]
    public class Illustrator
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(OS), "drawScanlines")]
        public static bool Prefix(OS __instance)
        {
            switch(StuxnetCore.illustState)
            {
                case States.DrawTitle:
                    DrawTitle(__instance);

                    goto default;
                case States.CTCDialogue:
                    IllustratorTypewriter.DrawCtcDialogue(__instance, StuxnetCore.dialogueText, StuxnetCore.dialogueEndActions);
                    StuxnetCore.dialogueIsActive = true;

                    goto default;
                default:
                    return true;
            }
        }

        public static void DrawTitle(OS os)
        {
            SpriteFont titleFont = GuiData.titlefont;
            SpriteFont subTitleFont = GuiData.font;

            string ChapterTitle = StuxnetCore.chapterTitle.ToUpper();
            string ChapterSubTitle = StuxnetCore.chapterSubTitle;

            int titleOffset = -50;
            int subTitleOffset = 50;

            Rectangle userBounds = os.fullscreen;

            RenderedRectangle.doRectangle(userBounds.X, userBounds.Y, userBounds.Width, userBounds.Height,
                Color.Black * 0.5f);

            // Draw chapter title
            Vector2 titleVector = titleFont.MeasureString(ChapterTitle);
            Vector2 titlePosition = new Vector2(
                (float)(userBounds.X + userBounds.Width / 2) - titleVector.X / 2f,
                userBounds.Center.Y + titleOffset
                );

            GuiData.spriteBatch.DrawString(titleFont, ChapterTitle, titlePosition, Color.White);

            // Draw chapter subtitle
            Vector2 subTitleVector = subTitleFont.MeasureString(ChapterSubTitle);
            Vector2 subPosition = new Vector2(
                (float)(userBounds.X + userBounds.Width / 2) - subTitleVector.X / 2f,
                userBounds.Center.Y + subTitleOffset
                );

            GuiData.spriteBatch.DrawString(subTitleFont, ChapterSubTitle, subPosition, Color.White);
        }
    }

    public class IllustratorTypewriter
    {
        private static char[] displayedChars;
        private static float timeTracker = 0f;

        private static int currentLine = 0;
        private static float totalLineHeight = 0f;
        private static readonly List<TextLine> textLines;

        private const string ctcText = "Click to continue...";

        private static readonly SpriteFont dialogueFont = GuiData.smallfont;
        private static readonly SpriteFont ctcFont = GuiData.detailfont;

        public static void DrawCtcDialogue(OS os, string textToWrite, string endActionsPath)
        {
            ParseText(textToWrite);

            TextLine currentTextLine = textLines[currentLine];

            displayedChars = textToWrite.ToCharArray();

            GameTime gameTime = os.lastGameTime;
            Rectangle gameScreen = os.fullscreen;
            Rectangle userBounds = os.fullscreen;

            RenderedRectangle.doRectangle(userBounds.X, userBounds.Y, userBounds.Width, userBounds.Height,
                Color.Black * 0.5f);

            int textLength = textToWrite.Length;

            if(timeTracker < textLength)
            {
                timeTracker += (float)gameTime.ElapsedGameTime.TotalSeconds * (10f * StuxnetCore.dialogueSpeed);
            }

            int charRange = (int)Math.Floor(timeTracker);

            char[] displayChars = displayedChars.Take(charRange).ToArray();
            string displayText = new string(displayChars);

            // Get measurements for the full text
            Vector2 dialogueVector = dialogueFont.MeasureString(textToWrite);
            Vector2 dialoguePosition = new Vector2(
                (gameScreen.X + gameScreen.Width / 2) - dialogueVector.X / 2f,
                gameScreen.Center.Y - (dialogueVector.Y / 2));

            // Actually show the text
            GuiData.spriteBatch.DrawString(dialogueFont, displayText, dialoguePosition, Color.White);

            // Show CTC text
            if(currentLine > textLines.Count)
            {
                Vector2 ctcVec = ctcFont.MeasureString(ctcText);
                Vector2 ctcPos = new Vector2(
                    (gameScreen.X + gameScreen.Width / 2) - ctcVec.X / 2f,
                    gameScreen.Height - 25);

                GuiData.spriteBatch.DrawString(ctcFont, ctcText, ctcPos, Color.White * 0.7f);

                MouseState mouse = Mouse.GetState();

                if(mouse.LeftButton == ButtonState.Pressed)
                {
                    StuxnetCore.dialogueIsActive = false;
                    displayedChars = null;


                    currentLine = 0;

                    if(endActionsPath.IsNullOrWhiteSpace()) {
                        StuxnetCore.illustState = States.None;

                        os.DisableTopBarButtons = false;

                        if (StuxnetCore.colorsCache.ContainsKey("topBarTextColor"))
                        {
                            os.topBarTextColor = StuxnetCore.colorsCache["topBarTextColor"];
                            os.topBarColor = StuxnetCore.colorsCache["topBarColor"];
                        }

                        os.display.visible = true;
                        os.netMap.visible = true;
                        os.ram.visible = true;
                        os.terminal.visible = true;

                        return;
                    }

                    RunnableConditionalActions.LoadIntoOS(endActionsPath, os);
                }
            }
        }

        private static void ParseText(string text)
        {
            textLines.Clear();

            string[] textSplit = text.Split('\n');

            foreach(string textLine in textSplit)
            {
                if(textLine.StartsWith("%"))
                {
                    TextLine line = new TextLine()
                    {
                        font = GuiData.font,
                        text = textLine.Substring(1),
                        length = textLine.Substring(1).Length,
                        lineOffset = GuiData.font.MeasureString(textLine.Substring(1)).Y
                    };

                    totalLineHeight += line.lineOffset + 5;

                    textLines.Add(line);
                } else
                {
                    TextLine line = new TextLine()
                    {
                        font = GuiData.smallfont,
                        text = textLine,
                        length = textLine.Length,
                        lineOffset = GuiData.smallfont.MeasureString(textLine).Y
                    };

                    totalLineHeight += line.lineOffset + 5;

                    textLines.Add(line);
                }
            }
        }

        private class TextLine
        {
            public SpriteFont font;
            public string text;
            public int length;
            public float lineOffset;
        }
    }
}
