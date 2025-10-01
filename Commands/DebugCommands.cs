using Hacknet;
using Pathfinder.Command;
using Stuxnet_HN.Patches;
using Stuxnet_HN.Persistence.Achievements;

namespace Stuxnet_HN.Commands
{
    public class DebugCommands
    {
        public static void RegisterCommands()
        {
            if (!OS.DEBUG_COMMANDS) return;
            CommandManager.RegisterCommand("decrypttheme", DecryptThemeFile);
            CommandManager.RegisterCommand("getbasetheme", GetBaseThemeForAnimatedTheme);
            CommandManager.RegisterCommand("unlocktestachv", CollectTestAchievement);
        }

        public static void DecryptThemeFile(OS os, string[] args)
        {
            if(args.Length <= 1)
            {
                os.write("Not enough arguments");
                return;
            }

            if (!args[1].EndsWith(".sys"))
            {
                os.write("Argument should be a custom theme file");
                return;
            }

            var folder = Programs.getCurrentFolder(os);
            var file = folder.searchForFile(args[1]);

            if(file == null)
            {
                os.write("File doesn't exist");
                return;
            }

            if(!file.data.Contains(ThemeManager.CustomThemeIDSeperator))
            {
                os.write("Argument should be a CUSTOM theme file");
                return;
            }

            var data = AnimatedThemeFilePatch.GetThemeDataFromFileData(file.data);
            os.write(data[1]);
        }

        public static void GetBaseThemeForAnimatedTheme(OS os, string[] args)
        {
            if(AnimatedThemeIllustrator.CurrentTheme == null)
            {
                os.write("Invalid theme");
                return;
            }

            os.write(AnimatedThemeIllustrator.CurrentTheme.ThemePath);
        }

        public static void CollectTestAchievement(OS os, string[] args)
        {
            StuxnetAchievement testAchv = new("Hello, World!", "This is a test achievement!");
            AchievementPatches.QueueAchievement(testAchv);
            os.write("Test achievement should've been shown");
        }
    }
}
