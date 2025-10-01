using Pathfinder.GUI;
using System.Collections.Generic;
using Hacknet;
using HarmonyLib;
using Hacknet.Screens;
using Hacknet.Gui;
using Microsoft.Xna.Framework;
using Stuxnet_HN.Persistence.Achievements;

namespace Stuxnet_HN.Patches.SettingsMenu
{
    [HarmonyPatch]
    public class StuxnetMenuButtonsPatch
    {
        internal static readonly Dictionary<string, int> SettingsButtonIDs = new()
        {
            { "OpenSettings", PFButton.GetNextID() },
            { "CloseSettings", PFButton.GetNextID() },
            { "OpenAchievements", PFButton.GetNextID() },
            { "CloseAchievements", PFButton.GetNextID() }
        };

        public const int BUTTON_MARGIN = 5;
        public const int BUTTON_WIDTH = 450;
        public const int SMALL_BUTTON_HEIGHT = 25;
        public const int BIG_BUTTON_HEIGHT = SMALL_BUTTON_HEIGHT * 2;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ExtensionsMenuScreen), "DrawExtensionInfoDetail")]
        public static void DrawStuxnetMainMenuButtons(Vector2 drawpos)
        {
            Vector2 buttonPos = drawpos;
            buttonPos.Y += BUTTON_MARGIN + SMALL_BUTTON_HEIGHT;

            if (Button.doButton(SettingsButtonIDs["OpenSettings"],
                (int)buttonPos.X, (int)buttonPos.Y, BUTTON_WIDTH, 25, "Stuxnet Settings...",
                MainMenu.buttonColor))
            {
                StuxnetSettingsMenu.Active = true;
            }

            buttonPos.Y += BUTTON_MARGIN + SMALL_BUTTON_HEIGHT;

            bool openAchvs = Button.doButton(SettingsButtonIDs["OpenAchievements"],
                (int)buttonPos.X, (int)buttonPos.Y, BUTTON_WIDTH, SMALL_BUTTON_HEIGHT,
                "Achievements", MainMenu.buttonColor);

            if(openAchvs)
            {
                AchievementsScreen.Open();
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ExtensionsMenuScreen), "Draw")]
        public static bool DrawStuxnetSettingsMenu(Rectangle dest)
        {
            if (!StuxnetSettingsMenu.Active && !AchievementsScreen.Active) return true;

            if(StuxnetSettingsMenu.Active)
            {
                StuxnetSettingsMenu.Draw(dest);
            } else if(AchievementsScreen.Active)
            {
                AchievementsScreen.Draw(dest);
            } else
            {
                return true;
            }

            return false;
        }

        internal static int GetSettingsButtonID(string buttonName)
        {
            if (SettingsButtonIDs.ContainsKey(buttonName)) return SettingsButtonIDs[buttonName];
            var id = PFButton.GetNextID();
            SettingsButtonIDs.Add(buttonName, id);
            return id;
        }
    }
}
