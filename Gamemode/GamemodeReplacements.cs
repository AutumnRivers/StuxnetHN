using BepInEx;
using Hacknet;
using Hacknet.Extensions;
using Hacknet.Localization;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Xml;

namespace Stuxnet_HN.Gamemode
{
    public class GamemodeReplacements
    {
        public static void AssignPlayerComputer(Computer computer, OS os)
        {
            if((computer.idName.ToLower() != "playercomp" &&
                computer.idName.ToLower() != GamemodeMenu.SelectedEntry.PlayerComputerID.ToLower()) ||
                GamemodeMenu.SelectedEntry.PlayerComputerID.IsNullOrWhiteSpace())
            {
                ExtensionLoader.CheckAndAssignCoreServer(computer, os);
                return;
            }

            if(computer.idName.ToLower() == "playercomp")
            {
                return;
            }

            computer.idName = "playerComp";
            ExtensionLoader.CheckAndAssignCoreServer(computer, os);
        }

        public static void LoadCustomStartingMission(OS os)
        {
            GamemodeEntry currentEntry = GamemodeMenu.SelectedEntry;

            if(currentEntry.StartingMissionPath.IsNullOrWhiteSpace() ||
                currentEntry.StartingMissionPath.ToLower() == "none")
            {
                ExtensionLoader.SendStartingEmailForActiveExtensionNextFrame(os);
                return;
            }

            os.delayer.Post(ActionDelayer.NextTick(), () =>
            {
                ActiveMission mission = currentEntry.GetStartingMission();
                os.currentMission = mission;
                mission.sendEmail(os);
                mission.ActivateSuppressedStartFunctionIfPresent();
                os.saveGame();
            });
        }
    }
}
