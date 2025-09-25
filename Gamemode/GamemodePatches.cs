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

        private static ExtensionInfo OldExtensionInfo;

        public const int CONFIRM_BUTTON_ID = 16392804;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SavefileLoginScreen), "Draw")]
        public static bool DrawGamemodeMenuIfNeeded(SavefileLoginScreen __instance)
        {
            if (GamemodeMenu.VisibleEntries.Count == 0) return true;

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
        public static bool DrawGamemodeMenuOverExtensions(ExtensionsMenuScreen __instance, Rectangle dest)
        {
            if (GamemodeMenu.State == GamemodeMenu.GamemodeMenuState.Disabled) return true;

            OldExtensionInfo ??= ExtensionLoader.ActiveExtensionInfo;
            SavescreenInstance ??= __instance.SaveScreen;

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
                c.idName = "playerComp";
                os.netMap.nodes.Remove(c);
                os.netMap.nodes.Insert(0, c);
                if(!os.netMap.visibleNodes.Contains(0))
                {
                    os.netMap.visibleNodes.Add(0);
                }
            }

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(IntroTextModule), "Update")]
        public static void ReplaceIntroText(IntroTextModule __instance)
        {
            if (GamemodeMenu.SelectedEntry == null) return;
            var currentEntry = GamemodeMenu.SelectedEntry;

            if (currentEntry.IntroTextOverride.IsNullOrWhiteSpace()) return;
            if(currentEntry.IntroTextOverride.ToLower() == "none")
            {
                __instance.text = new string[0];
                return;
            }

            string[] splitText = currentEntry.IntroTextOverride.Split('\n');
            __instance.text = splitText;
        }



        internal static void ChangeExtensionInfo()
        {
            ExtensionInfo extensionInfo = ExtensionLoader.ActiveExtensionInfo;
            var currentEntry = GamemodeMenu.SelectedEntry;
            if (currentEntry == null) return;

            if (!currentEntry.StartingSongPath.IsNullOrWhiteSpace())
            {
                extensionInfo.IntroStartupSong = currentEntry.StartingSongPath;
            }

            if (!currentEntry.StartingActionsPath.IsNullOrWhiteSpace())
            {
                extensionInfo.StartingActionsPath = currentEntry.StartingActionsPath.ToLower() != "none"
                    ? currentEntry.StartingActionsPath : null;
            }

            if (!currentEntry.StartingMissionPath.IsNullOrWhiteSpace())
            {
                extensionInfo.StartingMissionPath = currentEntry.StartingMissionPath.ToLower() != "none"
                    ? currentEntry.StartingMissionPath : null;
            }

            if (!currentEntry.StartingThemePath.IsNullOrWhiteSpace())
            {
                extensionInfo.Theme = currentEntry.StartingThemePath;
            }

            extensionInfo.AllowSave = !currentEntry.DisableSavesByDefault;
            StuxnetCore.defaultSave = !currentEntry.DisableSavesByDefault;

            if(currentEntry.StartingVisibleNodes.Count > 0)
            {
                if (currentEntry.StartingVisibleNodes[0].ToLower() == "none")
                {
                    extensionInfo.StartingVisibleNodes = new string[1] { "playerComp" };
                } else
                {
                    if(!currentEntry.StartingVisibleNodes.Contains("playerComp"))
                    {
                        currentEntry.StartingVisibleNodes.Add("playerComp");
                    }
                    extensionInfo.StartingVisibleNodes = currentEntry.StartingVisibleNodes.ToArray();
                }
            }

            extensionInfo.HasIntroStartup = currentEntry.HasIntroStartup;

            ExtensionLoader.ActiveExtensionInfo = extensionInfo;
            ActuallyStartNewGame();
        }

        public static void StartNewGame()
        {
            ChangeExtensionInfo();
            GamemodeMenu.CloseMenu();
        }

        private static void ActuallyStartNewGame()
        {
            string username = SavescreenInstance.Answers[0];
            string password = SavescreenInstance.Answers[1];

            SavescreenInstance.InPasswordMode = false;
            SavescreenInstance.PreventAdvancing = true;
            TextBox.MaskingText = false;

            SavescreenInstance.StartNewGameForUsernameAndPass(username, password);

            SavescreenInstance = null;
        }
    }
}
