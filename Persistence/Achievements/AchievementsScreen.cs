using System.Collections.Generic;
using System.Linq;
using Hacknet;
using Hacknet.Gui;
using Microsoft.Xna.Framework;
using Pathfinder.GUI;
using Stuxnet_HN.Extensions;


namespace Stuxnet_HN.Persistence.Achievements
{
    public static class AchievementsScreen
    {
        public static bool Active { get; internal set; } = false;

        public static List<StuxnetAchievement> Achievements
        {
            get { return AchievementsManager.ValidAchievements; }
        }

        public static List<StuxnetAchievement> VisibleAchievements { get; private set; } = new();

        public static List<StuxnetAchievement> CollectedAchievements
        {
            get { return AchievementsManager.CollectedAchievements; }
        }

        public static int TopMargin
        {
            get { return GuiData.titlefont.GetTextHeight("Hacknet", 0.5f); }
        }

        private static int ScrollPanelID = PFButton.GetNextID();
        private static Vector2 ScrollPanelValue = Vector2.Zero;

        public static void Open()
        {
            VisibleAchievements = Achievements.Where(a => !a.IsHidden || AchievementsManager.HasCollectedAchievement(a.Name))
                .ToList();
            LoadAllAchievements();
            Active = true;
        }

        public static void Close()
        {
            VisibleAchievements.Clear();
            UnloadAllAchievements();
            Active = false;
        }

        private static float LastYOffset = 0.0f;

        public static void Draw(Rectangle drawbounds)
        {
            LastYOffset = 0.0f;

            drawbounds.Height -= 30;

            int height = (int)(drawbounds.Height * 0.3f);
            int fullHeight = (height + 5) * VisibleAchievements.Count;

            Rectangle scrollbounds = drawbounds;
            scrollbounds.Height = fullHeight + 1;
            ScrollablePanel.beginPanel(ScrollPanelID, scrollbounds, ScrollPanelValue);

            foreach(var achv in VisibleAchievements)
            {
                DrawAchievementEntry(achv, new(0, LastYOffset), drawbounds.Width, height);
            }

            ScrollPanelValue = ScrollablePanel.endPanel(ScrollPanelID,
                ScrollPanelValue, drawbounds, scrollbounds.Height, true);

            DrawExitButton(drawbounds);
        }

        public static void DrawAchievementEntry(StuxnetAchievement achievement, Vector2 position, int width, int height)
        {
            if(achievement.IsHidden && !AchievementsManager.HasCollectedAchievement(achievement.Name))
            {
                return;
            }

            RenderedRectangle.doRectangle(
                (int)position.X, (int)position.Y,
                width, height, Color.Black * 0.2f);
            RenderedRectangle.doRectangleOutline(
                (int)position.X, (int)position.Y,
                width, height, 1, Color.LightGray);

            int xOffset = height + 5;
            bool collected = AchievementsManager.HasCollectedAchievement(achievement.Name);
            float opacity = collected ? 1.0f : 0.5f;
            achievement.DrawIcon(position, height, opacity);

            Rectangle topTextBounds = new()
            {
                X = xOffset,
                Y = (int)position.Y,
                Width = width - height - 5,
                Height = (int)(height * 0.75f)
            };
            TextItem.doFontLabelToSize(topTextBounds, achievement.Name, GuiData.font,
                Color.White * opacity, true);

            string description = achievement.Description.Truncate(64, "...", true);
            if(achievement.IsSecret && !AchievementsManager.HasCollectedAchievement(achievement.Name))
            {
                description = "???";
            }

            Rectangle bottomTextBounds = new()
            {
                X = xOffset,
                Y = (int)(position.Y + (height * 0.75f) + 5),
                Width = width - height - 5,
                Height = (int)(height * 0.245f)
            };
            TextItem.doFontLabelToSize(bottomTextBounds, description,
                GuiData.smallfont, Color.White * opacity, true);

            LastYOffset += height + 5;
        }

        private static int ExitButtonID = PFButton.GetNextID();

        private static void DrawExitButton(Rectangle drawbounds)
        {
            bool exit = Button.doButton(ExitButtonID, drawbounds.X,
                drawbounds.Y + drawbounds.Height - 25 - 10,
                450, 25, "Exit...", Color.Red);
            if(exit)
            {
                Close();
            }
        }

        private static void LoadAllAchievements()
        {
            foreach(var achv in Achievements)
            {
                achv.Load();
            }
        }

        private static void UnloadAllAchievements()
        {
            foreach(var achv in Achievements)
            {
                achv.Unload();
            }
        }
    }
}
