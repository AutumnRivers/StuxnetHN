using System.Collections.Generic;
using System.IO;
using Hacknet;
using Microsoft.Xna.Framework.Graphics;
using Pathfinder.Event.Loading;
using Pathfinder.Meta.Load;

namespace Stuxnet_HN.Patches
{
    public class ComputerIconsPatch
    {
        private static Dictionary<string, string> IconsToAdd = new()
        {
            { "sec0", "Sprites/CompLogos/Sec0Computer" },
            { "sec1", "Sprites/CompLogos/Sec1Computer" },
            { "computer", "Sprites/CompLogos/Computer" },
            { "oldServer", "Sprites/CompLogos/OldServer" },
            { "sec2", "Sprites/CompLogos/Sec2Computer" }
        };

        private static Dictionary<string, string> CustomIconsToAdd => StuxnetCore.Configuration.CustomCompIcons;

        [Event()]
        public static void AddSecurityIconsAsValidIcons(OSLoadedEvent loadedEvent)
        {
            OS os = loadedEvent.Os;
            DisplayModule displayModule = os.display;

            foreach(var iconToAdd in IconsToAdd)
            {
                if (displayModule.compAltIcons.ContainsKey(iconToAdd.Key)) continue;

                var icon = TextureBank.load(iconToAdd.Value, os.content);
                displayModule.compAltIcons.Add(iconToAdd.Key, icon);
            }

            AddCustomIcons(os);
        }

        public const int SOFT_FILE_SIZE_LIMIT = 1048576; // 1MB in bytes
        public const int HARD_FILE_SIZE_LIMIT = SOFT_FILE_SIZE_LIMIT * 10;
        public const int IMAGE_SIZE_LIMIT = 256;

        private static void AddCustomIcons(OS os)
        {
            if (CustomIconsToAdd.Count <= 0) return;
            var display = os.display;
            foreach(var iconToAdd in CustomIconsToAdd)
            {
                string imagePath = Utils.GetFileLoadPrefix() + iconToAdd.Value;

                if(!File.Exists(imagePath)) {
                    StuxnetCore.Logger.LogError(
                        string.Format("SKIPPED: Custom icon file at path {0} does not exist!", imagePath)
                        );
                    continue;
                }

                if (display.compAltIcons.ContainsKey(iconToAdd.Key)) continue;

                if(OS.DEBUG_COMMANDS && StuxnetCore.Configuration.ShowDebugText)
                {
                    StuxnetCore.Logger.LogDebug(
                        string.Format("Adding custom comp icon with ID of {0} and path of {1}",
                        iconToAdd.Key, imagePath)
                        );
                }

                var fileSize = new FileInfo(imagePath).Length;

                if(fileSize > HARD_FILE_SIZE_LIMIT)
                {
                    StuxnetCore.Logger.LogError(
                        string.Format("SKIPPED: Custom comp icons are not allowed to exceed 10MB in size. (ID:{0})",
                        iconToAdd.Key)
                        );
                    continue;
                } else if(fileSize > SOFT_FILE_SIZE_LIMIT)
                {
                    StuxnetCore.Logger.LogWarning(
                        string.Format("WARNING: Custom comp icons should not exceed 1MB. (ID:{0})",
                        iconToAdd.Key)
                        );
                }

                FileStream imageStream = File.OpenRead(imagePath);
                Texture2D image = Texture2D.FromStream(GuiData.spriteBatch.GraphicsDevice, imageStream);
                if(image.Width > IMAGE_SIZE_LIMIT || image.Height > IMAGE_SIZE_LIMIT)
                {
                    StuxnetCore.Logger.LogWarning(
                        string.Format("WARNING: Custom comp icons should not be bigger than {1}x{1} pixels. (ID:{0})",
                        iconToAdd.Key, IMAGE_SIZE_LIMIT)
                        );
                }
                display.compAltIcons.Add(iconToAdd.Key, image);
                imageStream.Close();
            }
        }
    }
}
