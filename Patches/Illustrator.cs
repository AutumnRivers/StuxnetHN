using BepInEx;
using Hacknet;
using Hacknet.Gui;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using States = Stuxnet_HN.Static.States.IllustratorStates;
using Stuxnet_HN.Actions.Dialogue;
using Stuxnet_HN.Localization;
using Stuxnet_HN.Actions;
using static Stuxnet_HN.Extensions.GuiHelpers;
using Pathfinder.Util.XML;
using Pathfinder.Replacements;

namespace Stuxnet_HN.Patches
{
    [HarmonyPatch]
    public class Illustrator
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(OS), "drawModules")]
        public static bool FullscreenCreditsIllustrator(OS __instance)
        {
            if (FullscreenCredits.IsActive)
            {
                var fullscreen = __instance.fullscreen;

                DrawRectangle(fullscreen, __instance.moduleColorBacking);

                FullscreenCredits.DrawFullscreenCredits();
                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(OS), "drawScanlines")]
        public static void Prefix(OS __instance)
        {
            if(StuxnetCore.CutsceneIsActive)
            {
                StuxnetCore.CurrentlyLoadedCutscene.Draw();
            }

            switch (StuxnetCore.illustState)
            {
                case States.DrawTitle:
                    DrawTitle(__instance);
                    break;
                case States.CTCDialogue:
                case States.AutoDialogue:
                    IllustratorTypewriter.DrawVNDialogue(__instance, StuxnetCore.CurrentVNTextData);
                    break;
            }
        }

        public static void DrawTitle(OS os)
        {
            SpriteFont titleFont = GuiData.titlefont;
            SpriteFont subTitleFont = GuiData.font;

            string ChapterTitle = StuxnetCore.ChapterData.Title;
            string ChapterSubTitle = StuxnetCore.ChapterData.Subtitle;

            int titleOffset = -50;
            int subTitleOffset = 50;

            Rectangle userBounds = os.fullscreen;

            RenderedRectangle.doRectangle(userBounds.X, userBounds.Y, userBounds.Width, userBounds.Height,
                Color.Black * StuxnetCore.ChapterData.BackingOpacity);

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
        private static readonly List<TextLine> textLines = new();

        private static readonly SpriteFont ctcFont = GuiData.detailfont;

        public static void DrawVNDialogue(OS os, VisualNovelTextData vnTextData)
        {
            string ctcText = string.Format("{0}...", Localizer.GetLocalized("Click anywhere to continue"));
            ParseText(vnTextData.Text);

            TextLine currentTextLine = textLines[currentLine];
            SpriteFont dialogueFont = currentTextLine.font;

            displayedChars = currentTextLine.text.ToCharArray();

            GameTime gameTime = os.lastGameTime;
            Rectangle gameScreen = os.fullscreen;
            Rectangle userBounds = os.fullscreen;

            float centerOffset = totalLineHeight / 2f;
            float lineOffset = 0f;

            RenderedRectangle.doRectangle(userBounds.X, userBounds.Y, userBounds.Width, userBounds.Height,
                Color.Black * vnTextData.BackingOpacity);

            for (int i = 0; i < currentLine; i++)
            {
                TextLine targetLine = textLines[i];

                Vector2 lineVector = targetLine.font.MeasureString(targetLine.text);
                Vector2 linePosition = new(
                    (gameScreen.X + gameScreen.Width / 2) - lineVector.X / 2f,
                    (gameScreen.Center.Y - centerOffset) + lineOffset);

                GuiData.spriteBatch.DrawString(targetLine.font, targetLine.text, linePosition, vnTextData.Color);

                lineOffset += targetLine.lineOffset;
            }

            int textLength = currentTextLine.length;

            if(timeTracker < textLength)
            {
                timeTracker += (float)gameTime.ElapsedGameTime.TotalSeconds * (10f * vnTextData.Speed);
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
            GuiData.spriteBatch.DrawString(dialogueFont, displayText, dialoguePosition, vnTextData.Color);

            if(timeTracker >= textLength && currentLine < (textLines.Count - 1))
            {
                timeTracker = 0f;
                currentLine++;
            }

            // Show CTC text
            if(currentLine >= (textLines.Count - 1) && timeTracker >= textLength)
            {
                if(!vnTextData.IsCtc)
                {
                    float completeDelay = vnTextData.CompleteDelay;

                    completeDelayTracker += (float)gameTime.ElapsedGameTime.TotalSeconds;

                    if(completeDelayTracker >= completeDelay)
                    {
                        ResetTypewriter(os, vnTextData.EndActions);
                    }

                    return;
                }

                Vector2 ctcVec = ctcFont.MeasureString(ctcText);
                Vector2 ctcPos = new(
                    (gameScreen.X + gameScreen.Width / 2) - ctcVec.X / 2f,
                    gameScreen.Height - 25);

                GuiData.spriteBatch.DrawString(ctcFont, ctcText, ctcPos, Color.White * 0.7f);

                MouseState mouse = Mouse.GetState();

                if(mouse.LeftButton == ButtonState.Pressed)
                {
                    ResetTypewriter(os, vnTextData.EndActions);
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

                if(TopBarColorsCache.HasCache)
                {
                    os.topBarTextColor = TopBarColorsCache.TopBarTextColor;
                    os.topBarColor = TopBarColorsCache.TopBarColor;
                    TopBarColorsCache.ClearCache();
                }

                os.display.visible = true;
                os.netMap.visible = true;
                os.ram.visible = true;
                os.terminal.visible = true;

                return;
            }

            bool nextActionIsDialogue = false;
            using (FileStream input = File.OpenRead(Utils.GetFileLoadPrefix() + endActionsPath))
            {
                if(OS.DEBUG_COMMANDS && StuxnetCore.Configuration.ShowDebugText)
                {
                    StuxnetCore.Logger.LogDebug("Loading end dialogue action at " +
                        Utils.GetFileLoadPrefix() + endActionsPath);
                }

                XmlReader rdr = XmlReader.Create(input);
                EventExecutor xmlExecutor = new(rdr);
                ElementInfo rootElement = null;

                xmlExecutor.RegisterExecutor("*", (exec, info) =>
                {
                    rootElement = info;
                }, ParseOption.ParseInterior);
                xmlExecutor.Parse();

                RunnableConditionalActions runnableConditionalActions = ActionsLoader.LoadActionSets(rootElement);

                foreach (var actionSet in runnableConditionalActions.Actions)
                {
                    if (actionSet.Actions.Any(a => a is VisualNovelTextActions.CTCDialogueAction ||
                    a is VisualNovelTextActions.AutoDialogueAction ||
                    a is ChapterTitleActions.ShowChapterTitle))
                    {
                        nextActionIsDialogue = true;
                        break;
                    }
                }
                rdr.Close();
                input.Close();
            }

            if(!nextActionIsDialogue)
            {
                StuxnetCore.illustState = States.None;

                os.DisableTopBarButtons = false;

                if (TopBarColorsCache.HasCache)
                {
                    os.topBarTextColor = TopBarColorsCache.TopBarTextColor;
                    os.topBarColor = TopBarColorsCache.TopBarColor;
                    TopBarColorsCache.ClearCache();
                }

                os.display.visible = true;
                os.netMap.visible = true;
                os.ram.visible = true;
                os.terminal.visible = true;
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
