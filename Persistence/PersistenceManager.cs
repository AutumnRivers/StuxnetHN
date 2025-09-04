using Hacknet.PlatformAPI.Storage;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using Hacknet;

namespace Stuxnet_HN.Persistence
{
    public static class PersistenceManager
    {
        public static List<string> PersistentFlags = new();
        public static PersistentFileData LastLoadedData { get; private set; }

        public const string PERSISTENT_FILE_NAME = "stuxnet.persistence.json";

        public static async void Initialize()
        {
            if (!SaveFileManager.StorageMethods[0].FileExists(PERSISTENT_FILE_NAME))
            {
                string jsonContent = JsonConvert.SerializeObject(new PersistentFileData());
                SaveFileManager.StorageMethods[0].WriteFileData(PERSISTENT_FILE_NAME, jsonContent);

                if(StuxnetCore.Configuration.ShowDebugText && OS.DEBUG_COMMANDS)
                {
                    StuxnetCore.Logger.LogDebug("Successfully wrote to persistent savefile.");
                }
            } else
            {
                var saveData = SaveFileManager.StorageMethods[0].GetFileReadStream(PERSISTENT_FILE_NAME);
                StreamReader saveDataReader = new(saveData);
                string rawSaveData = await saveDataReader.ReadToEndAsync();
                LastLoadedData = JsonConvert.DeserializeObject<PersistentFileData>(rawSaveData);
                saveDataReader.Close();
                saveData.Close();

                LoadPersistentData(LastLoadedData);

                if (StuxnetCore.Configuration.ShowDebugText && OS.DEBUG_COMMANDS)
                {
                    StuxnetCore.Logger.LogDebug("Successfully read data from persistent savefile.");
                }
            }
        }

        public static void LoadPersistentData(PersistentFileData fileData)
        {
            PersistentFlags = fileData.Flags;
        }

        public static void SavePersistentData()
        {
            PersistentFileData persistentData = PersistentFileData.FromGlobalData();
            string rawPersistentData = JsonConvert.SerializeObject(persistentData);
            SaveFileManager.StorageMethods[0].WriteFileData(PERSISTENT_FILE_NAME, rawPersistentData);
            LastLoadedData = persistentData;

            if (StuxnetCore.Configuration.ShowDebugText && OS.DEBUG_COMMANDS)
            {
                StuxnetCore.Logger.LogDebug("Successfully wrote to persistent savefile.");
            }
        }

        public static void Reset()
        {
            PersistentFlags = new();
            LastLoadedData = null;
        }

        public static void AddFlag(string flag)
        {
            if(!PersistentFlags.Contains(flag))
            {
                PersistentFlags.Add(flag);
            }
        }

        public static void TakeFlag(string flag)
        {
            if(PersistentFlags.Contains(flag))
            {
                PersistentFlags.Remove(flag);
            }
        }

        public static void ResetFlags()
        {
            PersistentFlags.Clear();
        }

        public static bool HasGlobalFlag(string flagName)
        {
            return PersistentFlags.Contains(flagName);
        }

        public static bool HasGlobalFlags(params string[] flagNames)
        {
            bool hasFlags = true;
            foreach(var flag in flagNames)
            {
                if (PersistentFlags.Contains(flag)) continue;
                hasFlags = false;
                break;
            }
            return hasFlags;
        }
    }

    public class PersistentFileData
    {
        public List<string> Flags = new();

        public static PersistentFileData FromGlobalData()
        {
            PersistentFileData persistentFileData = new()
            {
                Flags = PersistenceManager.PersistentFlags
            };
            return persistentFileData;
        }
    }
}
