using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet;

using HarmonyLib;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Stuxnet_HN.Replacements
{
    [HarmonyPatch]
    public class DrawScanlines
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(OS), "drawScanlines")]
        public static bool Prefix(OS __instance)
        {
            Texture2D scanLines = __instance.scanLines;
            Color scanlinesColor = __instance.scanlinesColor;

            if (!PostProcessor.scanlinesEnabled)
            {
                return true;
            }

            Vector2 position = new Vector2(0f, 0f);
            while (position.X < (float)__instance.ScreenManager.GraphicsDevice.Viewport.Width)
            {
                while (position.Y < (float)__instance.ScreenManager.GraphicsDevice.Viewport.Height)
                {
                    GuiData.spriteBatch.Draw(scanLines, position, scanlinesColor * (scanlinesColor.A / 255f));
                    position.Y += scanLines.Height;
                }

                position.Y = 0f;
                position.X += scanLines.Width;
            }

            return false;
        }
    }
}
