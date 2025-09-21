using System;
using BepInEx;
using Hacknet;
using Hacknet.Gui;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Pathfinder.GUI;
using Stuxnet_HN.Localization;

using static Stuxnet_HN.Extensions.GuiHelpers;

namespace Stuxnet_HN.Quests
{
    [HarmonyPatch]
    public class QuestPanelIllustrator
    {
        public static bool Opened { get; set; } = false;
        public static float TweenAmount { get; set; } = 0.0f;

        public static readonly string QuestText = Localizer.GetLocalized("Quests");

        public static bool Enabled => !StuxnetCore.Configuration.Quests.DisableQuestsSystem;

        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(OS), "drawScanlines")]
        public static void DrawQuestPanelButton()
        {
            if (!Enabled) return;

            if(Opened || TweenAmount > 0.0f)
            {
                DrawQuestPanel();
                return;
            }

            var textVec = GuiData.smallfont.MeasureString(QuestText);
            Rectangle button = new()
            {
                X = (int)((OS.currentInstance.topBar.Width / 2) - (textVec.X / 2) - 10),
                Y = 0,
                Width = (int)textVec.X + 20,
                Height = OS.TOP_BAR_HEIGHT
            };
            float buttonOpacity = 0.15f;
            if(MouseIsHoveringIn(button))
            {
                buttonOpacity *= 2;
            }

            GuiData.spriteBatch.Draw(Utils.white, button, Color.Black * buttonOpacity);
            TextItem.doSmallLabel(new(button.X + 10,
                (int)((OS.TOP_BAR_HEIGHT / 2) - (textVec.Y / 2))),
                QuestText, OS.currentInstance.topBarTextColor);

            if(MouseWasClickedIn(button))
            {
                Opened = true;
            }
        }

        public static int ExitButtonID = PFButton.GetNextID();
        public static readonly int ScrollPanelID = PFButton.GetNextID();

        public static int LastHeight = 0;
        private static int LastScroll = 0;

        private static bool _begun = false;

        public static readonly string MainMissionText = Localizer.GetLocalized("Main Mission");
        public static readonly string SQText = Localizer.GetLocalized("Sidequests");
        public static readonly string CompleteSQText = Localizer.GetLocalized("Complete Sidequest");
        public static readonly string ICSQText = Localizer.GetLocalized("Incomplete Sidequest");


        public static void DrawQuestPanel()
        {
            if (!Enabled) return;

            var os = OS.currentInstance;
            int width = (int)(os.topBar.Width * 0.75f);
            int height = (int)MathHelper.Max(os.display.bounds.Height, os.terminal.bounds.Height) - 50;

            if(Opened && TweenAmount < 1.0f)
            {
                TweenAmount += (float)os.lastGameTime.ElapsedGameTime.TotalSeconds * 0.65f;
            } else if(!Opened && TweenAmount > 0.0f)
            {
                TweenAmount -= (float)os.lastGameTime.ElapsedGameTime.TotalSeconds * 0.65f;
            }

            if(TweenAmount > 1.0f)
            {
                TweenAmount = 1.0f;
            }

            int panelY = (int)MathHelper.Lerp(-1 - height, -1, TweenAmount);

            Rectangle panelRect = new()
            {
                X = (os.topBar.Width / 2) - (width / 2),
                Y = panelY,
                Width = width,
                Height = height
            };

            if(LastHeight > panelRect.Height && !_begun)
            {
                ScrollablePanel.beginPanel(ScrollPanelID, panelRect, new(0, LastScroll));
                _begun = true;
            }

            DrawRectangle(panelRect, Color.Black * 0.9f);
            DrawOutline(panelRect, os.highlightColor, 1);

            if(Opened)
            {
                bool exit = Button.doButton(ExitButtonID,
                    panelRect.X + 10, panelRect.Y + panelRect.Height - 25,
                    panelRect.Width / 5, 15, "Exit...", Color.Red);
                if (exit)
                {
                    Opened = false;
                }
            }

            int yOffset = 10;
            TextItem.doLabel(new Vector2(panelRect.X + 10, panelRect.Y + yOffset),
                QuestText, Color.White);
            yOffset += GuiData.font.GetTextHeight(QuestText) + 10;

            DrawLine(new(panelRect.X, panelRect.Y + yOffset), panelRect.Width - 6, 3);
            yOffset += 8;

            GuiData.font.DrawScaledText(MainMissionText, new(panelRect.X + 10, panelRect.Y + yOffset),
                Color.White, 0.8f);
            yOffset += GuiData.font.GetTextHeight(MainMissionText, 0.8f) + 5;

            int missionHeight = (int)((panelRect.Width - 20) * MISSION_PANEL_HEIGHT_SCALE);
            DrawMission(QuestManager.MainMission, panelRect.Width - 20,
                new(panelRect.X + 10, panelRect.Y + yOffset));
            yOffset += missionHeight + 20;

            GuiData.font.DrawScaledText(SQText, new(panelRect.X + 10, panelRect.Y + yOffset),
                Color.White, 0.7f);
            yOffset += GuiData.font.GetTextHeight(SQText, 0.7f) + 5;

            for(int q = 0; q < QuestManager.Quests.Count; q++)
            {
                var quest = QuestManager.Quests[q];
                yOffset += DrawQuest(quest, new(panelRect.X + 10, panelRect.Y + yOffset), panelRect.Width - 20);
            }

            if(LastHeight > panelRect.Height)
            {
                LastScroll = (int)ScrollablePanel.endPanel(ScrollPanelID, new Vector2(0, LastScroll),
                    panelRect, Math.Max(panelRect.Height, LastHeight - panelRect.Height)).Y;
                _begun = false;
            }

            LastHeight = yOffset;
        }

        public int CompleteMainMissionButtonID = PFButton.GetNextID();

        public const float MISSION_PANEL_HEIGHT_SCALE = 0.15f;
        public const int DESCRIPTION_CHAR_LIMIT = 256;

        public static void DrawMission(ActiveMission mission, int width, Vector2 position)
        {
            Rectangle backingRect = new()
            {
                X = (int)position.X,
                Y = (int)position.Y,
                Width = width,
                Height = (int)(width * MISSION_PANEL_HEIGHT_SCALE)
            };
            DrawRectangle(backingRect, Color.Black * 0.1f);
            DrawOutline(backingRect, Color.White, 1);

            if (mission == null)
            {
                DrawCenteredText(backingRect, "No Active Mission", GuiData.font);
                return;
            }

            string title;
            string description;

            if (mission.postingTitle.IsNullOrWhiteSpace())
            {
                title = mission.email.subject;
                description = mission.email.body.Truncate(DESCRIPTION_CHAR_LIMIT, splitNewlines: true);
            } else
            {
                title = mission.postingTitle;
                description = mission.postingBody.Truncate(DESCRIPTION_CHAR_LIMIT, splitNewlines: true);
            }

            description = Utils.SuperSmartTwimForWidth(description,
                width - 10, GuiData.smallfont);

            TextItem.doLabel(new Vector2(position.X + 5, position.Y + 10),
                title, Color.White);
            int yOffset = GuiData.font.GetTextHeight(title) + 15;

            TextItem.doSmallLabel(new(position.X + 5, position.Y + yOffset),
                description, OS.currentInstance.lightGray);
            yOffset += GuiData.smallfont.GetTextHeight(description);
        }

        public static int DrawQuest(StuxnetQuest quest, Vector2 position, int maxWidth)
        {
            if (quest == null) return 0;

            int yOffset = 0;

            GuiData.font.DrawScaledText(quest.Title, new(position.X + 8, position.Y + yOffset + 8), Color.White,
                0.75f);
            if(OS.DEBUG_COMMANDS && StuxnetCore.Configuration.ShowDebugText)
            {
                int textWidth = (int)(GuiData.font.MeasureString(quest.Title).X * 0.75f);
                string idText = string.Format("id: {0}", quest.ID);
                string xMisText = string.Format("is XMOD: {0}", quest.IsXMission.ToString());
                int idHeight = GuiData.tinyfont.GetTextHeight(idText);
                TextItem.doTinyLabel(new(position.X + textWidth + 15, position.Y + yOffset + 8),
                    idText, Color.White * 0.5f);
                TextItem.doTinyLabel(new(position.X + textWidth + 15,
                    position.Y + yOffset + 8 + idHeight + 2), xMisText, Color.White * 0.5f);
            }
            yOffset += GuiData.font.GetTextHeight(quest.Title, 0.75f);

            string description = quest.Description;
            description = description.Truncate(128, splitNewlines: true);
            description = Utils.SuperSmartTwimForWidth(description, maxWidth - 8, GuiData.smallfont);
            TextItem.doSmallLabel(new(position.X + 8, position.Y + yOffset), description,
                OS.currentInstance.lightGray);
            yOffset += GuiData.smallfont.GetTextHeight(description);

            Rectangle updownGradientRect = new()
            {
                X = (int)position.X,
                Y = (int)position.Y,
                Width = 3,
                Height = yOffset
            };
            Rectangle leftrightGradientRect = new()
            {
                X = (int)position.X,
                Y = (int)position.Y,
                Width = maxWidth / 2,
                Height = 3
            };
            GuiData.spriteBatch.Draw(Utils.gradient, updownGradientRect, null, Color.White,
                0, Vector2.Zero, Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipVertically,
                1);
            GuiData.spriteBatch.Draw(Utils.gradientLeftRight, leftrightGradientRect, null,
                Color.White, 0, Vector2.Zero, Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipHorizontally,
                1);

            if(quest.ShowFailureTicker)
            {
                int buttonWidth = maxWidth / 8;
                TextItem.doSmallLabel(new(position.X + 8 + buttonWidth + 5,
                    position.X + yOffset + 10),
                    ICSQText, Color.Red);
            }
            yOffset += 20;

            bool attemptComplete = Button.doButton(quest.CompletionButtonID,
                (int)position.X + 8, (int)position.Y + yOffset + 10, maxWidth / 8, 20,
                CompleteSQText, OS.currentInstance.unlockedColor);
            if (attemptComplete)
            {
                bool success = QuestManager.AttemptCompleteQuest(quest);
                if (!success)
                {
                    quest.ShowFailureTicker = true;
                    OS.currentInstance.delayer.Post(ActionDelayer.Wait(5.0),
                        () =>
                        {
                            quest.ShowFailureTicker = false;
                        });
                }
            }

            return yOffset + 10;
        }
    }
}
