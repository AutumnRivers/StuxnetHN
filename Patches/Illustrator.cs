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
            if(StuxnetCore.originalScanlines == null)
            {
                StuxnetCore.originalScanlines = __instance.scanLines;
            }

            switch(StuxnetCore.illustState)
            {
                case States.DrawTitle:
                    DrawTitle(__instance);

                    goto default;
                case States.CTCDialogue:
                    IllustratorTypewriter.DrawCtcDialogue(__instance, StuxnetCore.dialogueText, StuxnetCore.dialogueEndActions);

                    goto default;
                case States.AutoDialogue:
                    IllustratorTypewriter.DrawCtcDialogue(__instance, StuxnetCore.dialogueText, StuxnetCore.dialogueEndActions);

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
                Color.Black * StuxnetCore.backingOpacity);

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
        private static float completeDelayTracker = 0f;

        private static int currentLine = 0;
        private static float totalLineHeight = 0f;
        private static readonly List<TextLine> textLines = new List<TextLine>();

        private const string ctcText = "Click to continue...";

        private static readonly SpriteFont ctcFont = GuiData.detailfont;

        public static void DrawCtcDialogue(OS os, string textToWrite, string endActionsPath)
        {
            ParseText(textToWrite);

            TextLine currentTextLine = textLines[currentLine];
            SpriteFont dialogueFont = currentTextLine.font;

            displayedChars = currentTextLine.text.ToCharArray();

            GameTime gameTime = os.lastGameTime;
            Rectangle gameScreen = os.fullscreen;
            Rectangle userBounds = os.fullscreen;

            float centerOffset = totalLineHeight / 2f;
            float lineOffset = 0f;

            RenderedRectangle.doRectangle(userBounds.X, userBounds.Y, userBounds.Width, userBounds.Height,
                Color.Black * StuxnetCore.backingOpacity);

            for (int i = 0; i < currentLine; i++)
            {
                TextLine targetLine = textLines[i];

                Vector2 lineVector = targetLine.font.MeasureString(targetLine.text);
                Vector2 linePosition = new Vector2(
                    (gameScreen.X + gameScreen.Width / 2) - lineVector.X / 2f,
                    (gameScreen.Center.Y - centerOffset) + lineOffset);

                GuiData.spriteBatch.DrawString(targetLine.font, targetLine.text, linePosition, StuxnetCore.dialogueColor);

                lineOffset += targetLine.lineOffset;
            }

            int textLength = currentTextLine.length;

            if(timeTracker < textLength)
            {
                timeTracker += (float)gameTime.ElapsedGameTime.TotalSeconds * (10f * StuxnetCore.dialogueSpeed);
            }

            int charRange = (int)Math.Floor(timeTracker);

            char[] displayChars = displayedChars.Take(charRange).ToArray();
            string displayText = new string(displayChars);

            // Get measurements for the full text
            Vector2 dialogueVector = dialogueFont.MeasureString(currentTextLine.text);
            Vector2 dialoguePosition = new Vector2(
                (gameScreen.X + gameScreen.Width / 2) - dialogueVector.X / 2f,
                (gameScreen.Center.Y - centerOffset) + lineOffset);

            // Actually show the text
            GuiData.spriteBatch.DrawString(dialogueFont, displayText, dialoguePosition, StuxnetCore.dialogueColor);

            if(timeTracker >= textLength && currentLine < (textLines.Count - 1))
            {
                timeTracker = 0f;
                currentLine++;
            }

            // Show CTC text
            if(currentLine >= (textLines.Count - 1) && timeTracker >= textLength)
            {
                if(!StuxnetCore.dialogueIsCtc)
                {
                    float completeDelay = StuxnetCore.dialogueCompleteDelay;

                    completeDelayTracker += (float)gameTime.ElapsedGameTime.TotalSeconds;

                    if(completeDelayTracker >= completeDelay)
                    {
                        ResetTypewriter(os, endActionsPath);
                    }

                    return;
                }

                Vector2 ctcVec = ctcFont.MeasureString(ctcText);
                Vector2 ctcPos = new Vector2(
                    (gameScreen.X + gameScreen.Width / 2) - ctcVec.X / 2f,
                    gameScreen.Height - 25);

                GuiData.spriteBatch.DrawString(ctcFont, ctcText, ctcPos, Color.White * 0.7f);

                MouseState mouse = Mouse.GetState();

                if(mouse.LeftButton == ButtonState.Pressed)
                {
                    ResetTypewriter(os, endActionsPath);
                }
            }
        }

        private static void ResetTypewriter(OS os, string endActionsPath)
        {
            displayedChars = null;

            currentLine = 0;
            timeTracker = 0f;
            completeDelayTracker = 0f;

            totalLineHeight = 0;
            textLines.Clear();

            StuxnetCore.dialogueIsActive = false;

            if (endActionsPath.IsNullOrWhiteSpace())
            {
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

        private static void ParseText(string text)
        {
            textLines.Clear();
            totalLineHeight = 0;

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

                    totalLineHeight += line.lineOffset + 3;

                    textLines.Add(line);
                } else
                {
                    string finalText = textLine;

                    if(textLine.IsNullOrWhiteSpace())
                    {
                        finalText = " ";
                    }

                    TextLine line = new TextLine()
                    {
                        font = GuiData.smallfont,
                        text = finalText,
                        length = finalText.Length,
                        lineOffset = GuiData.smallfont.MeasureString(finalText).Y
                    };

                    totalLineHeight += line.lineOffset + 3;

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
