using BepInEx;
using Hacknet;
using Hacknet.Extensions;
using Hacknet.Gui;
using Hacknet.Screens;
using Hacknet.UIUtils;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace Stuxnet_HN.Gamemode
{
    [HarmonyPatch]
    public class GamemodePatches
    {
        private static SavefileLoginScreen SavescreenInstance;

        public const int CONFIRM_BUTTON_ID = 16392804;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SavefileLoginScreen), "Draw")]
        public static bool DrawGamemodeMenuIfNeeded(SavefileLoginScreen __instance)
        {
            if (GamemodeMenu.VisibleEntries.Count == 0) return true;

            SavescreenInstance ??= __instance;

            if(GamemodeMenu.State != GamemodeMenu.GamemodeMenuState.Disabled)
            {
                return false;
            }

            // We create a non-visible button using the same ID as the confirmation button to
            // track when said confirmation button is pressed, because that's how Hacknet's
            // GUI system works. Usually I hate the same ID thing... but it works in my favor here?
            bool invisibleTracker = Button.doButton(CONFIRM_BUTTON_ID, -100, -100, 10, 10, "()", Color.Transparent);
            bool hitEnter = __instance.CanReturnEnter && Utils.keyPressed(GuiData.lastInput, Keys.Enter, null);

            if (__instance.HasOverlayScreen || __instance.PreventAdvancing || !__instance.IsNewAccountMode) return true;
            if (!invisibleTracker && !hitEnter) return true;
            if (__instance.Answers.Count < 3) return true;

            if(GamemodeMenu.CanOpenMenu)
            {
                GamemodeMenu.OpenMenu();
                return false;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ExtensionsMenuScreen), "Draw")]
        public static bool DrawGamemodeMenuOverExtensions(Rectangle dest)
        {
            if (GamemodeMenu.State == GamemodeMenu.GamemodeMenuState.Disabled) return true;

            GamemodeMenu.DrawGamemodeMenu(dest);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ExtensionLoader), "CheckAndAssignCoreServer")]
        public static bool ReplacePlayerCompIfNecessary(Computer c, OS os)
        {
            if (GamemodeMenu.SelectedEntry == null) return true;
            if (GamemodeMenu.SelectedEntry.PlayerComputerID.IsNullOrWhiteSpace() ||
                GamemodeMenu.SelectedEntry.PlayerComputerID.ToLower() == "playercomp") return true;
            string targetID = GamemodeMenu.SelectedEntry.PlayerComputerID;
            if (c.idName.ToLower() != "playercomp" &&
                c.idName != targetID) return true;

            if(c.idName.ToLower() == "playercomp" &&
                targetID != c.idName)
            {
                os.netMap.nodes.Remove(c);
                return false;
            }

            if(c.idName == targetID)
            {
                os.netMap.nodes.Remove(os.thisComputer);
                os.thisComputer = c;
                c.adminIP = c.ip;
                os.netMap.nodes.Remove(c);
                os.netMap.nodes.Insert(0, c);
                if(!os.netMap.visibleNodes.Contains(0))
                {
                    os.netMap.visibleNodes.Add(0);
                }
            }

            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MainMenu), "CreateNewAccountForExtensionAndStart")]
        public static void ReplaceNewExtensionAccountIfNecessary()
        {
            ExtensionInfo extensionInfo = ExtensionLoader.ActiveExtensionInfo;
            var currentEntry = GamemodeMenu.SelectedEntry;
            if (currentEntry == null) return;

            if(!currentEntry.StartingSongPath.IsNullOrWhiteSpace())
            {
                extensionInfo.IntroStartupSong = currentEntry.StartingSongPath;
            }

            if(!currentEntry.StartingActionsPath.IsNullOrWhiteSpace())
            {
                extensionInfo.StartingActionsPath = currentEntry.StartingActionsPath;
            }

            if(!currentEntry.StartingMissionPath.IsNullOrWhiteSpace())
            {
                extensionInfo.StartingMissionPath = currentEntry.StartingMissionPath;
            }

            if(!currentEntry.StartingThemePath.IsNullOrWhiteSpace())
            {
                extensionInfo.Theme = currentEntry.StartingThemePath;
            }

            if(currentEntry.DisableSavesByDefault)
            {
                extensionInfo.AllowSave = currentEntry.DisableSavesByDefault;
            }

            ExtensionLoader.ActiveExtensionInfo = extensionInfo;
        }

        public static void StartNewGame()
        {
            GamemodeMenu.CloseMenu();

            string username = SavescreenInstance.Answers[0];
            string password = SavescreenInstance.Answers[1];

            SavescreenInstance.InPasswordMode = false;
            SavescreenInstance.StartNewGameForUsernameAndPass(username, password);
            SavescreenInstance = null;
        }
    }
}
