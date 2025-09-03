using System.Collections.Generic;
using Newtonsoft.Json;
using Hacknet.Extensions;
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
}