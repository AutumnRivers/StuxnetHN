using Microsoft.Xna.Framework;
using Hacknet;
using Hacknet.Gui;
using System;
using System.Collections.Generic;
using Stuxnet_HN.Extensions;
using Hacknet.PlatformAPI.Storage;
using System.Collections.ObjectModel;

namespace Stuxnet_HN.Patches.SettingsMenu
{
    internal static class StuxnetSettingsMenu
    {
        private static int LastButtonOffset = 0;

        public static bool Active { get; internal set; }

        private static readonly List<StuxnetSettingsButton> _buttons = new()
        {
            new("ClearPersistence", "Clear Persistent Data", ClearPersistentData,
                "WARNING: This will also delete ALL of your save files for this extension.",
                Color.Red)
        };

        public static ReadOnlyCollection<StuxnetSettingsButton> Buttons
        {
            get { return _buttons.AsReadOnly(); }
        }

        private static string ResultText;

        public static void Draw(Rectangle bounds)
        {
            LastButtonOffset = 0;

            string titleText = "Stuxnet Settings";

            if(!string.IsNullOrWhiteSpace(ResultText))
            {
                titleText = ResultText;
            }

            LastButtonOffset += GuiData.titlefont.GetTextHeight("Hacknet", 0.5f);

            TextItem.doLabel(new(bounds.X, bounds.Y + LastButtonOffset),
                titleText, Color.White);
            LastButtonOffset += (int)(GuiData.font.GetTextHeight(titleText) * 1.25f);

            foreach(var btn in Buttons)
            {
                bool pressed = DrawButtonWithSubtitle(new(bounds.X, bounds.Y + LastButtonOffset),
                    btn.ButtonID, btn.ButtonText, btn.SubText, btn.Disabled ? Color.Gray : btn.Color);
                if (pressed && !btn.Disabled) btn.OnPressed();
            }

            bool exit = Button.doButton(StuxnetMenuButtonsPatch.SettingsButtonIDs["CloseSettings"],
                bounds.X + 10, bounds.Y + bounds.Height - StuxnetMenuButtonsPatch.SMALL_BUTTON_HEIGHT - 10,
                StuxnetMenuButtonsPatch.BUTTON_WIDTH, StuxnetMenuButtonsPatch.SMALL_BUTTON_HEIGHT,
                "Exit...", MainMenu.exitButtonColor);
            if(exit)
            {
                Active = false;
            }
        }

        public static bool DrawButtonWithSubtitle(Vector2 buttonPos, int buttonID, string buttonText, string subText, Color buttonColor)
        {
            bool buttonHit = Button.doButton(buttonID, (int)buttonPos.X, (int)buttonPos.Y,
                StuxnetMenuButtonsPatch.BUTTON_WIDTH, StuxnetMenuButtonsPatch.BIG_BUTTON_HEIGHT,
                buttonText, buttonColor);
            int offset = StuxnetMenuButtonsPatch.BIG_BUTTON_HEIGHT + StuxnetMenuButtonsPatch.BUTTON_MARGIN;
            LastButtonOffset += offset;

            string trimmedText = Utils.SuperSmartTwimForWidth(subText, StuxnetMenuButtonsPatch.BUTTON_WIDTH, GuiData.smallfont);
            TextItem.doSmallLabel(new(buttonPos.X, buttonPos.Y + offset), trimmedText, Color.White);
            LastButtonOffset += GuiData.smallfont.GetTextHeight(trimmedText) + (StuxnetMenuButtonsPatch.BUTTON_MARGIN * 2);

            return buttonHit;
        }

        private static void ClearPersistentData()
        {
            Persistence.PersistenceManager.Reset();
            Persistence.PersistenceManager.SavePersistentData();
            ClearSaveFiles();
        }

        private static void ClearSaveFiles()
        {
            bool success = true;
            foreach(var account in SaveFileManager.Accounts)
            {
                try
                {
                    SaveFileManager.DeleteUser(account.Username);
                } catch(Exception e)
                {
                    success = false;
                    StuxnetCore.Logger.LogError(
                        string.Format("Failed to delete savefile '{0}': {1}",
                        account.Username, e.ToString())
                        );
                    ResultText = "Error(s) Occurred.";
                    continue;
                }
            }
            if(success) { ResultText = "Success."; }
        }

        public class StuxnetSettingsButton
        {
            public int ButtonID;
            public string ButtonText;
            public string SubText;
            public Color Color;
            public Action OnPressed;
            public bool Disabled = false;

            public StuxnetSettingsButton(string name, string text, Action onPressed,
                string subText = null, Color buttonColor = default)
            {
                Color = buttonColor == default ? MainMenu.buttonColor : buttonColor;

                ButtonID = StuxnetMenuButtonsPatch.GetSettingsButtonID(name);
                ButtonText = text;
                SubText = subText;
                OnPressed = onPressed;
            }
        }
    }
}
