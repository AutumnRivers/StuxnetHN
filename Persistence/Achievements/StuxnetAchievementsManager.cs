using Hacknet;
using Hacknet.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Stuxnet_HN.Persistence.Achievements
{
    public static class StuxnetAchievementsManager
    {
        public static List<StuxnetAchievement> ValidAchievements = new();
        public static List<StuxnetAchievement> CollectedAchievements = new();

        public static void Initialize()
        {
            ValidAchievements.Clear();
            CollectedAchievements.Clear();

            if (StuxnetCore.Configuration.Achievements.Count <= 0) return;

            foreach(var achv in StuxnetCore.Configuration.Achievements)
            {
                var validAchv = new StuxnetAchievement(achv.Name, achv.Description, achv.IconPath);
                if(ValidAchievements.Any(a => a.Name == validAchv.Name))
                {
                    StuxnetCore.Logger.LogError("Achievement names must be unique! " +
                        string.Format("An achievement with the name '{0}' already exists. Skipping.",
                        validAchv.Name));
                    continue;
                }
                validAchv.IsSecret = achv.Secret;
                validAchv.IsHidden = achv.Hidden;
                ValidAchievements.Add(validAchv);
            }
        }

        public static List<string> GetCollectedAchievementNames()
        {
            if (CollectedAchievements.Count <= 0) return new List<string>();
            List<string> achvNames = new();
            foreach (var achv in CollectedAchievements)
            {
                achvNames.Add(achv.Name);
            }
            return achvNames;
        }

        public static void CollectAchievement(string achievementName)
        {
            var achv = GetAchievement(achievementName);
            if(achv != null && !HasCollectedAchievement(achievementName))
            {
                if(OS.DEBUG_COMMANDS && StuxnetCore.Configuration.ShowDebugText)
                {
                    StuxnetCore.Logger.LogDebug(string.Format("Collected achievement: {0}", achievementName));
                }
                CollectedAchievements.Add(achv);
                if(CollectedAchievements.Count == ValidAchievements.Count)
                {
                    PersistenceManager.AddFlag("collected_all_achievements");
                }
            } else if(OS.DEBUG_COMMANDS && StuxnetCore.Configuration.ShowDebugText)
            {
                StuxnetCore.Logger.LogWarning(
                    string.Format("Unable to collect achievement: {0}\nIt's either already been collected," +
                    " or it doesn't exist.", achievementName));
            }
        }

        public static StuxnetAchievement GetAchievement(string achievementName)
        {
            return ValidAchievements.FirstOrDefault(a => a.Name == achievementName);
        }

        public static bool HasCollectedAchievement(string achievementName)
        {
            return CollectedAchievements.Any(a => a.Name == achievementName);
        }

        public static void Reset()
        {
            CollectedAchievements.Clear();
            PersistenceManager.TakeFlag("collected_all_achievements");
            PersistenceManager.SavePersistentData();
            StuxnetCore.Logger.LogDebug("Reset achievements.");
        }
    }

    public class StuxnetAchievement
    {
        public string Name;
        public string Description;
        public string IconPath;
        public bool IsSecret = false;
        public bool IsHidden = false;

        public bool HasStartedLoading = false;

        private Texture2D _icon;
        public Texture2D Icon
        {
            get { return _icon; }
        }

        private bool FullyLoaded = false;

        public StuxnetAchievement(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public StuxnetAchievement(string name, string description, string imageFilepath)
        {
            Name = name;
            Description = description;
            IconPath = imageFilepath;
        }

        public override string ToString()
        {
            return Name;
        }

        public void Load()
        {
            if (FullyLoaded || HasStartedLoading) return;
            HasStartedLoading = true;
            string fullFilePath = Utils.GetFileLoadPrefix() + IconPath;
            if (!File.Exists(fullFilePath))
            {
                _icon = ExtensionLoader.ActiveExtensionInfo.LogoImage;
                return;
            }

            FileStream imageStream = File.OpenRead(fullFilePath);
            _icon = Texture2D.FromStream(GuiData.spriteBatch.GraphicsDevice, imageStream);
            imageStream.Close();

            FullyLoaded = true;
        }

        public void Unload()
        {
            FullyLoaded = false;
            HasStartedLoading = false;
            _icon = null;
        }

        public void DrawIcon(Rectangle dest, float opacity = 1.0f)
        {
            if (!FullyLoaded) return;
            GuiData.spriteBatch.Draw(Icon, dest, Color.White * opacity);
        }

        public void DrawIcon(Vector2 position, int height, float opacity = 1.0f)
        {
            Rectangle rect = new()
            {
                X = (int)position.X,
                Y = (int)position.Y,
                Height = height,
                Width = height
            };
            DrawIcon(rect, opacity);
        }
    }
}
