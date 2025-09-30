using Hacknet;
using Hacknet.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Stuxnet_HN.Persistence.Achievements
{
    public static class AchievementsManager
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

        public static void CollectAchievement(string achievementName)
        {
            var achv = GetAchievement(achievementName);
            if(achv != null && !HasCollectedAchievement(achievementName))
            {
                CollectedAchievements.Add(achv);
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
    }

    public class StuxnetAchievement
    {
        public string Name;
        public string Description;
        public string IconPath;
        public bool IsSecret = false;
        public bool IsHidden = false;

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

        public void Load()
        {
            if (FullyLoaded) return;
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
            _icon.Dispose();
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
