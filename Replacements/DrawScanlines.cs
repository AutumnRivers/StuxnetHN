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
            if (!PostProcessor.scanlinesEnabled)
            {
                return false;
            }

            Texture2D scanLines = __instance.scanLines;
            Color scanlinesColor = __instance.scanlinesColor;

            Vector2 position = new Vector2(0f, 0f);
            while (position.X < (float)__instance.ScreenManager.GraphicsDevice.Viewport.Width)
            {
                while (position.Y < (float)__instance.ScreenManager.GraphicsDevice.Viewport.Height)
                {
                    if(StuxnetCore.useScanLinesFix)
                    {
                        GuiData.spriteBatch.Draw(scanLines, position, scanlinesColor * (scanlinesColor.A / 255f));
                    } else
                    {
                        GuiData.spriteBatch.Draw(scanLines, position, scanlinesColor);
                    }
                    position.Y += scanLines.Height;
                }

                position.Y = 0f;
                position.X += scanLines.Width;
            }

            return false;
        }
    }
}
