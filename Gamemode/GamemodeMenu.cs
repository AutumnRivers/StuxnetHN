using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using Hacknet;
using Hacknet.Extensions;
using Hacknet.Gui;
using Microsoft.Xna.Framework;
using Pathfinder.GUI;
using Stuxnet_HN.Localization;
using Stuxnet_HN.Persistence;
using static Stuxnet_HN.Extensions.GuiHelpers;

namespace Stuxnet_HN.Gamemode
{
    public static class GamemodeMenu
    {
        public static List<GamemodeEntry> Entries { get; set; } = new();
        public static GamemodeEntry SelectedEntry { get; set; }
        public static GamemodeMenuState State { get; set; } = GamemodeMenuState.Disabled;

        public static List<GamemodeEntry> VisibleEntries
        {
            get
            {
                return Entries.Where(e => e.CanBeSeen).ToList();
            }
        }

        private static string LastError;

        public enum GamemodeMenuState
        {
            Disabled,
            ListEntries,
            ConfirmEntry,
            Error
        }

        public static void Initialize()
        {
            if (StuxnetCore.Configuration.Gamemode.Gamemodes.Count == 0) return;

            foreach(var configEntry in StuxnetCore.Configuration.Gamemode.Gamemodes)
            {
                Entries.Add(GamemodeEntry.FromConfig(configEntry));
            }
        }

        public static void DrawGamemodeMenu(Rectangle bounds)
        {
            if (State == GamemodeMenuState.Disabled) return;

            switch(State)
            {
                case GamemodeMenuState.ListEntries:
                default:
                    DrawEntries(bounds);
                    break;
                case GamemodeMenuState.ConfirmEntry:
                    DrawEntryConfirmation(bounds);
                    break;
                case GamemodeMenuState.Error:
                    DrawErrorScreen(bounds);
                    break;
            }
        }

        private static int EntriesPanelID = -1;
        private static int SelectPathButtonID = -1;

        public const int ENTRY_PANEL_HEIGHT = 140;
        public const int ENTRY_PANEL_MARGIN = 50;
        public const int ENTRIES_BOT_MARGIN = 15;

        private static float EntryPanelScroll = 0f;

        public static void DrawEntries(Rectangle bounds)
        {
            if(EntriesPanelID == -1)
            {
                EntriesPanelID = PFButton.GetNextID();
            }

            string selectText = Localizer.GetLocalized("Select Your Starting Path");
            if(!StuxnetCore.Configuration.Gamemode.SelectPathText.IsNullOrWhiteSpace())
            {
                selectText = StuxnetCore.Configuration.Gamemode.SelectPathText;
            }
            selectText = selectText.Truncate(36);
            GuiData.font.DrawScaledText(selectText,
                new(bounds.X, bounds.Y + ENTRY_PANEL_MARGIN),
                Color.White, 1.37f);
            int yOffset = GuiData.font.GetTextHeight(selectText, 1.37f) + ENTRY_PANEL_MARGIN + bounds.Y;
            int listWidth = bounds.Width - 50;

            Rectangle listBounds = new()
            {
                X = bounds.X,
                Y = yOffset + 20,
                Width = listWidth,
                Height = bounds.Height - yOffset - ENTRY_PANEL_MARGIN
            };
            Rectangle descBounds = listBounds;
            descBounds.Width = bounds.Width - 25;
            descBounds.X += listWidth + 25;

            DrawRectangle(descBounds, Color.Black * 0.3f);
            DrawOutline(descBounds, Utils.SlightlyDarkGray, 1);

            string fullDescription = ExtensionLoader.ActiveExtensionInfo.Description;
            if (PotentialPath != null)
            {
                fullDescription = PotentialPath.Description;
            }
            fullDescription = fullDescription.Truncate(1024);
            fullDescription = Utils.SuperSmartTwimForWidth(fullDescription, descBounds.Width - 20, GuiData.smallfont);

            TextItem.doSmallLabel(new(descBounds.X + 10, descBounds.Y + 10), fullDescription, Color.White);

            List<GamemodeEntry> VisibleEntries = Entries.ToList();
            VisibleEntries.RemoveAll(e => !e.CanBeSeen);

            int finalHeight = (VisibleEntries.Count * ENTRY_PANEL_HEIGHT) +
                ((VisibleEntries.Count - 1) * ENTRIES_BOT_MARGIN);
            bool needsScroll = finalHeight > listBounds.Height;
            int xOffset = bounds.X;
            yOffset = listBounds.Y;
            if (needsScroll)
            {
                Rectangle drawbounds = listBounds;
                drawbounds.Height = finalHeight;
                drawbounds.Width += drawbounds.Width / 10;
                ScrollablePanel.beginPanel(EntriesPanelID, drawbounds, new(0, EntryPanelScroll));

                yOffset = 0;
                xOffset = 0;
            }

            foreach(var entry in Entries)
            {
                if(!entry.RequiredFlagForVisibility.IsNullOrWhiteSpace())
                {
                    if (!PersistenceManager.HasGlobalFlag(entry.RequiredFlagForVisibility)) continue;
                }

                try
                {
                    DrawEntry(entry, new(xOffset, yOffset), listBounds.Width);
                    yOffset += ENTRY_PANEL_HEIGHT + ENTRIES_BOT_MARGIN;
                } catch(Exception e)
                {
                    CatchError(e);
                }
            }

            if (needsScroll)
            {
                float maxScroll = Math.Max(finalHeight, listBounds.Height - finalHeight);
                var scroll = ScrollablePanel.endPanel(EntriesPanelID, new(0, EntryPanelScroll),
                    listBounds, maxScroll);
                EntryPanelScroll = scroll.Y;
            }

            if(LastEntryID != -1 && !GuiData.isMouseLeftDown())
            {
                LastEntryID = -1;
            }

            bool selectPath = Button.doButton(ConfirmButtonID,
                bounds.X + bounds.Width - 50 - (bounds.Width / 4),
                bounds.Y + bounds.Height - 150,
                bounds.Width / 4, 50, "Select Path",
                PotentialPath == null ? Utils.SlightlyDarkGray : Color.DarkGreen);
            if(selectPath && PotentialPath != null)
            {
                State = GamemodeMenuState.ConfirmEntry;
            }

            bool goBack = Button.doButton(GoBackButtonID,
                bounds.X,
                bounds.Y + bounds.Height - 150,
                bounds.Width / 4, 50, "Go Back", Color.DarkRed);
            if(goBack)
            {
                CloseMenu();
            }
        }

        private static GamemodeEntry PotentialPath;
        private static int LastEntryID = -1;

        public static void DrawEntry(GamemodeEntry entry, Vector2 pos, int width)
        {
            bool disabled = !entry.CanSelect;

            Rectangle entryBounds = new()
            {
                X = (int)pos.X,
                Y = (int)pos.Y,
                Width = width,
                Height = ENTRY_PANEL_HEIGHT
            };

            Color backingColor = Color.Black * 0.2f;
            if (entry == PotentialPath)
            {
                backingColor = Color.Black * 0.5f;
            }
            if(MouseIsHoveringIn(entryBounds) && GuiData.isMouseLeftDown() &&
                (entry.GuiID == LastEntryID || LastEntryID == -1) && !disabled)
            {
                backingColor = Color.Black * 0.35f;
                if (LastEntryID == -1) LastEntryID = entry.GuiID;
            }
            if(LastEntryID == entry.GuiID && GuiData.mouseWasPressed())
            {
                PotentialPath = entry;
            }

            if(disabled)
            {
                backingColor = Utils.SlightlyDarkGray * 0.2f;
            }

            DrawRectangle(entryBounds, backingColor);
            DrawOutline(entryBounds, Utils.SlightlyDarkGray, 1);

            int yOffset = 0;
            string title = entry.Title;
            if (disabled) title += " (DISABLED)";
            GuiData.font.DrawScaledText(title, new(pos.X + 5, pos.Y + 5), Color.White, 0.75f);
            yOffset += GuiData.font.GetTextHeight(entry.Title, 0.75f);

            string description = Utils.SuperSmartTwimForWidth(entry.ShortDescription, width, GuiData.smallfont);
            TextItem.doSmallLabel(new(pos.X + 5, pos.Y + yOffset + 5), description, Utils.SlightlyDarkGray);
        }

        private static int ConfirmButtonID = -1;
        private static int GoBackButtonID = -1;

        public static void DrawEntryConfirmation(Rectangle bounds)
        {
            if(PotentialPath == null)
            {
                State = GamemodeMenuState.ListEntries;
                StuxnetCore.Logger.LogError(
                    "Gamemode Menu loaded entry confirmation, but no entry was selected!"
                    );
            }
            if(!PotentialPath.CanBeSeen || !PotentialPath.CanSelect)
            {
                State = GamemodeMenuState.ListEntries;
                PotentialPath = null;

                StuxnetCore.Logger.LogError("Invalid entry in entry confirmation screen.");
            }

            string topTextTemplate = Localizer.GetLocalized("You have selected {0}.");
            string topText = string.Format(topTextTemplate, PotentialPath.Title);

            var topTextSize = GuiData.font.MeasureString(topText) * 2;
            int xOffset = bounds.Center.X - ((int)topTextSize.X / 2);
            int yOffset = 0;

            GuiData.font.DrawScaledText(topText,
                new(xOffset, bounds.Y + ENTRY_PANEL_MARGIN),
                Color.White, 2);

            string bottomText = Localizer.GetLocalized("Are you sure?");
            var bottomTextSize = GuiData.font.MeasureString(bottomText);
            yOffset += (int)topTextSize.Y + 25;
            xOffset = bounds.Center.X - ((int)bottomTextSize.X / 2);

            TextItem.doLabel(new(xOffset, bounds.Y + yOffset),
                bottomText, Color.White);
            yOffset += GuiData.font.GetTextHeight(bottomText) + 50;

            bool goBack = Button.doButton(GoBackButtonID,
                bounds.Center.X - 5 - (bounds.Width / 4),
                bounds.Y + yOffset,
                bounds.Width / 4, 50,
                "Go Back", Color.DarkRed);
            if(goBack)
            {
                State = GamemodeMenuState.ListEntries;
                return;
            }

            bool confirm = Button.doButton(ConfirmButtonID,
                bounds.Center.X + 5,
                bounds.Y + yOffset,
                bounds.Width / 4, 50,
                "Confirm", Color.DarkGreen);
            if(confirm)
            {
                try
                {
                    SelectedEntry = PotentialPath;
                    PotentialPath = null;
                    GamemodePatches.StartNewGame();
                } catch(Exception e)
                {
                    CatchError(e);
                }
                return;
            }
        }

        public static void DrawErrorScreen(Rectangle bounds)
        {
            string errorText = "An error occurred with Stuxnet.Gamemode.\n";
            errorText += "For a more detailed error, check your BepInEx logs.\n\n";
            errorText += Utils.SuperSmartTwimForWidth(LastError, bounds.Width - 20, GuiData.font);
            TextItem.doLabel(new(bounds.X + 10, bounds.Y + ENTRY_PANEL_MARGIN), errorText, Color.Red);

            bool goBack = Button.doButton(GoBackButtonID,
                bounds.X + 10,
                bounds.Y + bounds.Height - 110,
                bounds.Width / 6,
                50, "Go Back", Color.DarkRed);
            if(goBack)
            {
                State = GamemodeMenuState.ListEntries;
            }
        }

        public static bool CanOpenMenu
        {
            get
            {
                if (StuxnetCore.Configuration.Gamemode.RequirePersistentFlag.IsNullOrWhiteSpace()) return true;

                return PersistenceManager.HasGlobalFlag(StuxnetCore.Configuration.Gamemode.RequirePersistentFlag);
            }
        }

        public static void OpenMenu()
        {
            EntriesPanelID = ReturnAndReassignID(EntriesPanelID);
            SelectPathButtonID = ReturnAndReassignID(SelectPathButtonID);
            ConfirmButtonID = ReturnAndReassignID(ConfirmButtonID);
            GoBackButtonID = ReturnAndReassignID(GoBackButtonID);

            State = GamemodeMenuState.ListEntries;
        }

        public static int ReturnAndReassignID(int currentID)
        {
            if(currentID > -1)
            {
                PFButton.ReturnID(currentID);
            }
            return PFButton.GetNextID();
        }

        public static void CloseMenu()
        {
            if (State == GamemodeMenuState.Disabled) return;

            EntriesPanelID = ReturnAndResetID(EntriesPanelID);
            SelectPathButtonID = ReturnAndResetID(SelectPathButtonID);
            ConfirmButtonID = ReturnAndResetID(ConfirmButtonID);
            GoBackButtonID = ReturnAndResetID(GoBackButtonID);

            State = GamemodeMenuState.Disabled;
        }

        public static int ReturnAndResetID(int currentID)
        {
            PFButton.ReturnID(currentID);
            return -1;
        }

        private static void CatchError(Exception e)
        {
            LastError = e.ToString();
            StuxnetCore.Logger.LogError(LastError);
            State = GamemodeMenuState.Error;
        }
    }
}
