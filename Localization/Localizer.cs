using System.IO;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Hacknet;
using BepInEx;

namespace Stuxnet_HN.Localization
{
    internal static class Localizer
    {
        public const string LOCAL_FOLDER_PATH = "./StuxnetLocalization/";
        public const string TEMPLATE_FILENAME = "template.json";
        public static List<StuxnetLocalization> ValidLanguages = new();
        public static StuxnetLocalization ActiveLanguage;

        public static async void Initialize()
        {
            if (!Directory.Exists(LOCAL_FOLDER_PATH)) return;
            if (!Directory.GetFiles(LOCAL_FOLDER_PATH).Any()) return;

            foreach(var filename in Directory.EnumerateFiles(LOCAL_FOLDER_PATH))
            {
                string langCode = filename.Split('.')[0];
                if (ValidLanguages.Any(vl => vl.LanguageCode == langCode) ||
                    filename == TEMPLATE_FILENAME ||
                    !filename.EndsWith(".json")) continue;

                StreamReader fileReader = new(LOCAL_FOLDER_PATH);
                string fileContent = await fileReader.ReadToEndAsync();
                fileReader.Close();

                var localizedTerms = JsonConvert.DeserializeObject<Dictionary<string,string>>(fileContent);
                var localization = new StuxnetLocalization()
                {
                    LanguageCode = langCode,
                    LocalizedTerms = localizedTerms
                };
                ValidLanguages.Add(localization);
                if(OS.DEBUG_COMMANDS)
                {
                    StuxnetCore.Logger.LogDebug(string.Format("Registered localization with language code of {0}",
                        localization.LanguageCode));
                }
                if(Settings.ActiveLocale == localization.LanguageCode)
                {
                    ActiveLanguage = localization;
                }
            }
        }

        public static string GetLocalized(string term)
        {
            if (ActiveLanguage == null) return term;

            return ActiveLanguage.GetLocalizedTerm(term);
        }
    }

    internal class StuxnetLocalization
    {
        public string LanguageCode { get; set; } = "en-us";
        public Dictionary<string, string> LocalizedTerms { get; set; } = new();

        public string GetLocalizedTerm(string englishTerm)
        {
            if(!LocalizedTerms.ContainsKey(englishTerm))
            {
                return englishTerm;
            } else if (LocalizedTerms[englishTerm].IsNullOrWhiteSpace())
            {
                return englishTerm;
            }

            return LocalizedTerms[englishTerm];
        }
    }
}
