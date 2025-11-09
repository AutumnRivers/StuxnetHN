using BepInEx.Configuration;
using Pathfinder.Event;
using Pathfinder.Event.Options;
using Pathfinder.Options;
using StuxnetHN.Audio.Replacements;

namespace StuxnetHN.Audio.Options
{
    public class AudioOptions
    {
        public const string OPTION_TAG = "Stuxnet.Audio";
        internal static OptionSlider CacheSizeLimit = new("SMM Cache Limit", "Sets the maximum amount of song files " +
            "to keep in the cache, until older entries are removed (0 = disable cache entirely)")
        { 
            MinValue = 0,
            MaxValue = 5,
            Step = 1
        };
        public static void Initialize()
        {
            OptionsManager.AddOption(OPTION_TAG, CacheSizeLimit);
            EventManager<CustomOptionsSaveEvent>.AddHandler(OnOptionSave);
            InitConfig();
        }

        private static void InitConfig()
        {
            ConfigEntry<int> entry = StuxnetAudioCore.UserConfig.Bind(OPTION_TAG, "CacheLimit", 3, "SMM Cache File Limit");
            CacheSizeLimit.Value = entry.Value;
            OggMusicPlayer.CacheLimit = entry.Value;
        }

        private static void OnOptionSave(CustomOptionsSaveEvent _)
        {
            ConfigEntry<int> entry;
            StuxnetAudioCore.UserConfig.TryGetEntry(OPTION_TAG, "CacheLimit", out entry);
            entry.Value = (int)CacheSizeLimit.Value;
            StuxnetAudioCore.UserConfig.Save();
        }
    }
}
