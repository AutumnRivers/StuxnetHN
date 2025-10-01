using System;
using System.Collections.Generic;
using Hacknet;
using Hacknet.Extensions;
using Hacknet.Gui;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static Stuxnet_HN.Extensions.GuiHelpers;

namespace Stuxnet_HN.Persistence.Achievements
{
    [HarmonyPatch]
    public class AchievementPatches
    {
        private static Queue<StuxnetAchievement> QueuedAchievements = new();
        private static StuxnetAchievement CurrentAchievement;
        private static float AnimationProgress = 0.0f;
        private static float AnimationLifetime = 0.0f;

        private const float ANIMATION_DURATION = 1.5f;
        private const float POPUP_LIFE = 5.0f;

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(OS),"drawModules")]
        public static void DrawAchievementPopup(OS __instance)
        {
            if (QueuedAchievements.Count <= 0 && CurrentAchievement == null) return;
            if (CurrentAchievement == null)
            {
                LoadAndDiscardNextAchievement();
                return;
            }

            var gt = (float)__instance.lastGameTime.ElapsedGameTime.TotalSeconds;
            var fullscreen = __instance.fullscreen;

            int popupHeight = fullscreen.Height / 6;

            var fontHeight = GuiData.font.GetTextHeight("Achievement Unlocked!");

            int yOffset = (int)MathHelper.Lerp(fullscreen.Height + fontHeight + 5, fullscreen.Height - popupHeight, AnimationProgress);

            if((AnimationLifetime - ANIMATION_DURATION) >= POPUP_LIFE)
            {
                if(AnimationProgress <= 0.0f)
                {
                    LoadAndDiscardNextAchievement();
                    return;
                }
                AnimationProgress -= gt / ANIMATION_DURATION;
            } else if(AnimationProgress < 1.0f)
            {
                AnimationProgress += gt / ANIMATION_DURATION;
            }

            AnimationLifetime += gt;
            DrawPopup(fullscreen, yOffset);
        }

        private static void DrawPopup(Rectangle fullscreen, int yOffset)
        {
            int popupWidth = fullscreen.Width / 5;
            int popupHeight = (int)(fullscreen.Height / 5.5f);
            Rectangle popupBackground = new(fullscreen.X, yOffset, popupWidth, popupHeight);

            Texture2D achvImage;

            if(string.IsNullOrWhiteSpace(CurrentAchievement.IconPath))
            {
                achvImage = ExtensionLoader.ActiveExtensionInfo.LogoImage;
            } else
            {
                if(CurrentAchievement.Icon == null)
                {
                    if(!CurrentAchievement.HasStartedLoading) CurrentAchievement.Load();
                    achvImage = ExtensionLoader.ActiveExtensionInfo.LogoImage;
                } else
                {
                    achvImage = CurrentAchievement.Icon;
                }
            }

            Rectangle iconBounds = new(0, popupBackground.Y + 15, popupBackground.Height - 30, popupBackground.Height - 30);
            iconBounds.X = fullscreen.X + (popupBackground.Width / 2) - (iconBounds.Width / 2);
            GuiData.spriteBatch.Draw(achvImage, iconBounds, Color.White * 0.3f);

            DrawRectangle(popupBackground, Color.Black * 0.5f);
            DrawOutline(popupBackground, OS.currentInstance.defaultHighlightColor, 2);

            var fontHeight = GuiData.font.GetTextHeight("Achievement Unlocked!");
            Rectangle titleBounds = new(fullscreen.X, yOffset - fontHeight + 12,
                popupBackground.Width, fontHeight);

            TextItem.doCenteredFontLabel(titleBounds, "Achievement Unlocked!", GuiData.font,
                OS.currentInstance.defaultHighlightColor);

            int textYOffset = yOffset + 5;
            Rectangle topBounds = new(fullscreen.X + 5, textYOffset, popupBackground.Width - 10,
                (int)(popupBackground.Height / 0.35f));
            TextItem.doFontLabelToSize(topBounds, CurrentAchievement.Name, GuiData.font, Color.White, true, true);

            textYOffset += (int)Math.Min(GuiData.font.GetTextHeight(CurrentAchievement.Name),
                popupBackground.Height / 0.35f);
            string description = Utils.SuperSmartTwimForWidth(CurrentAchievement.Description,
                popupBackground.Width - 10, GuiData.smallfont);
            Rectangle botBounds = new(fullscreen.X + 5, textYOffset + 5, popupBackground.Width - 10,
                (int)(popupBackground.Height / 0.6f));
            TextItem.doFontLabelToSize(botBounds, description, GuiData.smallfont,
                OS.currentInstance.lightGray, true, true);
        }

        private static void LoadAndDiscardNextAchievement()
        {
            AnimationLifetime = 0.0f;
            AnimationProgress = 0.0f;
            if (QueuedAchievements.Count <= 0)
            {
                CurrentAchievement = null;
                return;
            }
            if(CurrentAchievement != null)
            {
                CurrentAchievement.Unload();
            }
            OS.currentInstance.mailicon.newMailSound.Play();
            CurrentAchievement = QueuedAchievements.Dequeue();
        }

        internal static void QueueAchievement(StuxnetAchievement achievement)
        {
            if (QueuedAchievements.Contains(achievement)) return;
            QueuedAchievements.Enqueue(achievement);
        }

        internal static void QueueAchievement(string achievementName)
        {
            var achv = StuxnetAchievementsManager.GetAchievement(achievementName);
            if (achv == null) return;
            QueueAchievement(achv);
        }
    }
}
