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
        public static void StartNewExtensionSaveReplacement(ExtensionInfo info, object os_obj)
        {
            LocaleActivator.ActivateLocale(info.Language, Game1.getSingleton().Content);
            OS os = (OS)os_obj;
            GamemodeEntry currentEntry = GamemodeMenu.SelectedEntry;
            People.ReInitPeopleForExtension();
            if (Directory.Exists(info.FolderPath + "/Nodes"))
            {
                Utils.ActOnAllFilesRevursivley(info.FolderPath + "/Nodes", delegate (string filename)
                {
                    if (filename.EndsWith(".xml"))
                    {
                        Computer parsedComputer;
                        if (OS.TestingPassOnly)
                        {
                            try
                            {
                                parsedComputer = Computer.loadFromFile(filename);
                                if (parsedComputer != null)
                                {
                                    AssignPlayerComputer(parsedComputer, os);
                                }

                                return;
                            }
                            catch (Exception ex)
                            {
                                string format = "COMPUTER LOAD ERROR:\nError loading computer \"{0}\"";
                                Exception ex2 = ex;
                                format = string.Format(format, filename);
                                while (ex2 != null)
                                {
                                    string text3 = $"\r\nError: {ex2.GetType().Name} - {ex2.Message}";
                                    format += text3;
                                    ex2 = ex2.InnerException;
                                }

                                FormatException ex3 = new FormatException(format, ex);
                                throw ex3;
                            }
                        }

                        parsedComputer = Computer.loadFromFile(filename);
                        if (parsedComputer != null)
                        {
                            AssignPlayerComputer(parsedComputer, os);
                        }
                    }
                });
            }

            ComputerLoader.postAllLoadedActions?.Invoke();

            Computer mailServerComp = Programs.getComputer(os, "jmail");
            if (mailServerComp == null)
            {
                mailServerComp = new("JMail Email Server", NetworkMap.generateRandomIP(), new Vector2(0.8f, 0.2f), 6, 1, os)
                {
                    idName = "jmail"
                };
                mailServerComp.daemons.Add(new MailServer(mailServerComp, "JMail", os));
                MailServer.shouldGenerateJunk = false;
                mailServerComp.users.Add(new UserDetail(os.defaultUser.name, "mailpassword", 2));
                mailServerComp.initDaemons();
                os.netMap.mailServer = mailServerComp;
                os.netMap.nodes.Add(mailServerComp);
            }

            for (int i = 0; i < info.StartingVisibleNodes.Length; i++)
            {
                Computer nodeToDiscover = Programs.getComputer(os, info.StartingVisibleNodes[i]);
                if (nodeToDiscover != null)
                {
                    os.netMap.discoverNode(nodeToDiscover);
                }
            }

            for (int i = 0; i < info.FactionDescriptorPaths.Count; i++)
            {
                string text = info.FolderPath + "/" + info.FactionDescriptorPaths[i];
                using FileStream input = File.OpenRead(text);
                try
                {
                    XmlReader xmlRdr = XmlReader.Create(input);
                    Faction faction = Faction.loadFromSave(xmlRdr);
                    os.allFactions.factions.Add(faction.idName, faction);
                }
                catch (Exception innerException)
                {
                    throw new FormatException("Error loading Faction: " + text, innerException);
                }
            }

            OSTheme extTheme = currentEntry.GetStartingTheme();
            if(extTheme == OSTheme.HacknetBlue)
            {
                ThemeManager.setThemeOnComputer(os.thisComputer, info.Theme);
                ThemeManager.switchTheme(os, info.Theme);
            }
            else if(extTheme != OSTheme.Custom)
            {
                ThemeManager.setThemeOnComputer(os.thisComputer, extTheme);
                ThemeManager.switchTheme(os, extTheme);
            } else
            {
                string themePath = info.FolderPath + "/" + currentEntry.StartingThemePath;
                ThemeManager.setThemeOnComputer(os.thisComputer, themePath);
                ThemeManager.switchTheme(os, themePath);
            }

            if(!currentEntry.StartingSongPath.IsNullOrWhiteSpace())
            {
                info.IntroStartupSong = currentEntry.StartingSongPath;
            }
            ExtensionLoader.LoadExtensionStartTrackAsCurrentSong(info);

            string startingActions = info.StartingActionsPath;
            if(!currentEntry.StartingActionsPath.IsNullOrWhiteSpace() &&
                currentEntry.StartingActionsPath.ToLower() != "none")
            {
                startingActions = currentEntry.StartingActionsPath;
            }
            if(!startingActions.IsNullOrWhiteSpace())
            {
                startingActions = info.FolderPath + "/" + startingActions;
                os.delayer.Post(ActionDelayer.NextTick(), () =>
                {
                    RunnableConditionalActions.LoadIntoOS(startingActions, os);
                });
            }

            if (info.StartingMissionPath != null && !info.StartsWithTutorial && !info.HasIntroStartup)
            {
                LoadCustomStartingMission(os);
                if (!os.Flags.HasFlag("ExtensionFirstBootComplete"))
                {
                    os.Flags.AddFlag("ExtensionFirstBootComplete");
                }
            }
        }

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
