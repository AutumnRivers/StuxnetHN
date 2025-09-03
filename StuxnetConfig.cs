using BepInEx;
using Hacknet;
using Hacknet.Extensions;
using Newtonsoft.Json;
using Pathfinder.GUI;
using Pathfinder.Util;
using Stuxnet_HN.Configuration;
using Stuxnet_HN.Persistence;
using System;
using System.Collections.Generic;
using System.IO;

namespace Stuxnet_HN.Configuration
{
    public class StuxnetConfig
    {
        public static StuxnetConfig GlobalConfig;

        public bool ShowDebugText = true;

        public StuxnetAudioConfig Audio;
        public StuxnetQuestsConfig Quests;
        public StuxnetSMSConfig SMS;
        public StuxnetGamemodeConfig Gamemode;
        public Dictionary<string, CodeEntry> Codes;
        public Dictionary<string, SequencerInfo> Sequencers;

        public const string STUXNET_CONFIG_FILENAME = "stuxnet_config.json";

        public static StuxnetConfig LoadFromJson()
        {
            string extensionFolder = ExtensionLoader.ActiveExtensionInfo.FolderPath + "/";
            if(!File.Exists(extensionFolder + STUXNET_CONFIG_FILENAME))
            {
                StuxnetCore.Logger.LogWarning("Stuxnet configuration file not found!");
                return null;
            }

            var rawConfig = File.ReadAllText(extensionFolder + STUXNET_CONFIG_FILENAME);
            GlobalConfig = JsonConvert.DeserializeObject<StuxnetConfig>(rawConfig);
            return GlobalConfig;
        }
    }

    public class StuxnetAudioConfig
    {
        public Dictionary<string, SongEntry> Songs;
    }

    public class StuxnetQuestsConfig
    {
        public bool ReplaceLoadMissionAction = true;
        public bool IgnoreXMODMissions = false;
    }

    public class StuxnetSMSConfig
    {
        public Dictionary<string, string> AuthorColors;
    }

    public class StuxnetGamemodeConfig
    {
        public List<GamemodeConfiguration> Gamemodes;
        public string SelectPathText = string.Empty;
        public string RequirePersistentFlag = string.Empty;
    }

    public class GamemodeConfiguration
    {
        public string Title;
        public string Description;
        public bool AllowSaves = true;
        public string StartingMission = "none";
        public string StartingActions = "none";
        public string StartingTheme = null;
        public string StartingSong = null;
        public string PlayerCompID = "playerComp";
        public List<string> StartingFlags = new();

        public string RequiredFlagForSelection = null;
        public string RequiredFlagForVisibility = null;
    }
}

namespace Stuxnet_HN
{
    public class CodeEntry
    {
        public Dictionary<string, string> files;
        public string[] radio;
        public Dictionary<string, string> themes;
        public Dictionary<string, string> stuxnetThemes;
        public string email;
        public string action;
    }

    public class SongEntry
    {
        public string artist;
        public string title;
        public string path;
        public bool initial = false;
        public string songId;
    }

    public class SequencerInfo
    {
        public string requiredFlag;
        public float spinUpTime;
        public string targetIDorIP;
        public string sequencerActions;
    }

    public class GamemodeEntry
    {
        public string Title;
        public string Description;
        public bool DisableSavesByDefault = false;

        public string StartingMissionPath;
        public string StartingActionsPath;
        public string StartingThemePath;
        public string StartingSongPath;
        public string PlayerComputerID;

        public string RequiredFlagForSelection;
        public string RequiredFlagForVisibility;

        public List<string> StartingFlags;

        internal int GuiID;

        public const int SHORT_DESC_CHAR_LIMIT = 128;

        public string ShortDescription => Description.Truncate(SHORT_DESC_CHAR_LIMIT);

        public bool CanSelect
        {
            get
            {
                if (RequiredFlagForSelection.IsNullOrWhiteSpace()) return true;
                return PersistenceManager.HasGlobalFlag(RequiredFlagForSelection);
            }
        }
        public bool CanBeSeen
        {
            get
            {
                if (RequiredFlagForVisibility.IsNullOrWhiteSpace()) return true;
                return PersistenceManager.HasGlobalFlag(RequiredFlagForVisibility);
            }
        }

        public GamemodeEntry()
        {
            GuiID = PFButton.GetNextID();
        }

        public void Destroy()
        {
            PFButton.ReturnID(GuiID);
            GuiID = -1;
        }

        public static GamemodeEntry FromConfig(GamemodeConfiguration configuration)
        {
            string playerID = string.Empty;
            if (configuration.PlayerCompID != "playerComp") playerID = configuration.PlayerCompID;
            return new GamemodeEntry()
            {
                Title = configuration.Title,
                Description = configuration.Description,
                DisableSavesByDefault = !configuration.AllowSaves,
                StartingMissionPath = configuration.StartingMission,
                StartingActionsPath = configuration.StartingActions,
                PlayerComputerID = playerID,
                StartingFlags = configuration.StartingFlags,
                StartingSongPath = configuration.StartingSong,
                StartingThemePath = configuration.StartingTheme,
                RequiredFlagForSelection = configuration.RequiredFlagForSelection,
                RequiredFlagForVisibility = configuration.RequiredFlagForVisibility
            };
        }

        public ActiveMission GetStartingMission()
        {
            if (StartingMissionPath.IsNullOrWhiteSpace() ||
                StartingMissionPath.ToLowerInvariant() == "none") return null;

            return (ActiveMission)ComputerLoader.readMission(StartingMissionPath);
        }

        public OSTheme GetStartingTheme()
        {
            if (StartingThemePath.IsNullOrWhiteSpace()) return OSTheme.HacknetBlue;
            ExtensionInfo info = ExtensionLoader.ActiveExtensionInfo;

            OSTheme theme = OSTheme.Custom;
            foreach (object value in Enum.GetValues(typeof(OSTheme)))
            {
                string text2 = value.ToString().ToLower();
                if (text2 == info.Theme)
                {
                    theme = (OSTheme)value;
                    break;
                }
            }

            return theme;
        }
    }
}