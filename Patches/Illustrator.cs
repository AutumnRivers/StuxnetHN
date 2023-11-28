using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet;
using Hacknet.Gui;

using HarmonyLib;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using States = Stuxnet_HN.Static.States.IllustratorStates;

namespace Stuxnet_HN.Patches
{
    [HarmonyPatch]
    public class Illustrator
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(OS), "drawScanlines")]
        public static bool Prefix(OS __instance)
        {
            switch(StuxnetCore.illustState)
            {
                case States.DrawTitle:
                    DrawTitle(__instance);

                    goto default;
                default:
                    return true;
            }
        }

        public static void DrawTitle(OS os)
        {
            SpriteFont titleFont = GuiData.titlefont;
            SpriteFont subTitleFont = GuiData.font;

            string ChapterTitle = StuxnetCore.chapterTitle.ToUpper();
            string ChapterSubTitle = StuxnetCore.chapterSubTitle;

            int titleOffset = -50;
            int subTitleOffset = 50;

            Rectangle userBounds = os.fullscreen;

            RenderedRectangle.doRectangle(userBounds.X, userBounds.Y, userBounds.Width, userBounds.Height,
                Color.Black * 0.5f);

            // Draw chapter title
            Vector2 titleVector = titleFont.MeasureString(ChapterTitle);
            Vector2 titlePosition = new Vector2(
                (float)(userBounds.X + userBounds.Width / 2) - titleVector.X / 2f,
                userBounds.Center.Y + titleOffset
                );

            GuiData.spriteBatch.DrawString(titleFont, ChapterTitle, titlePosition, Color.White);

            // Draw chapter subtitle
            Vector2 subTitleVector = subTitleFont.MeasureString(ChapterSubTitle);
            Vector2 subPosition = new Vector2(
                (float)(userBounds.X + userBounds.Width / 2) - subTitleVector.X / 2f,
                userBounds.Center.Y + subTitleOffset
                );

            GuiData.spriteBatch.DrawString(subTitleFont, ChapterSubTitle, subPosition, Color.White);
        }
    }
}
