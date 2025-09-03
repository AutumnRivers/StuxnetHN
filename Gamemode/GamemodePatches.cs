using Hacknet;
using Hacknet.Extensions;
using Hacknet.Gui;
using Hacknet.Screens;
using Hacknet.UIUtils;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stuxnet_HN.Gamemode
{
    [HarmonyPatch]
    public class GamemodePatches
    {
        private static SavefileLoginScreen SavescreenInstance;

        public const int CONFIRM_BUTTON_ID = 16392804;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SavefileLoginScreen), "Draw")]
        public static bool DrawGamemodeMenuIfNeeded(SavefileLoginScreen __instance, Rectangle dest)
        {
            if (GamemodeMenu.Entries.Count == 0) return true;

            if(SavescreenInstance == null)
            {
                SavescreenInstance = __instance;
            }

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

            GamemodeMenu.DrawGamemodeMenu(dest);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ExtensionLoader), "LoadNewExtensionSession")]
        public static bool ReplaceNewExtensionSessionIfNecessary(ExtensionInfo info, object os_obj)
        {
            if (GamemodeMenu.SelectedEntry == null) return true;

            GamemodeReplacements.StartNewExtensionSaveReplacement(info, os_obj);
            return false;
        }

        public static void StartNewGame()
        {
            GamemodeMenu.CloseMenu();

            string username = SavescreenInstance.Answers[0];
            string password = SavescreenInstance.Answers[1];

            SavescreenInstance.StartNewGameForUsernameAndPass(username, password);
            SavescreenInstance = null;
        }
    }
}
