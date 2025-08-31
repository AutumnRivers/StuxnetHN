using Hacknet;
using Hacknet.Gui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathfinder.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stuxnet_HN.Extensions
{
    public static class GuiHelpers
    {
        public static void DrawRectangle(Rectangle target, Color color)
        {
            RenderedRectangle.doRectangle(target.X, target.Y, target.Width, target.Height, color);
        }

        public static void DrawOutline(Rectangle target, Color color, int thickness)
        {
            RenderedRectangle.doRectangleOutline(target.X, target.Y, target.Width, target.Height,
                thickness, color);
        }

        public static void DrawLine(Vector2 startingPoint, int width, int thickness)
        {
            DrawLine(startingPoint, width, thickness, Color.White);
        }

        public static void DrawLine(Vector2 startingPoint, int width, int thickness, Color color,
            bool ignoreCentering = false)
        {
            if(thickness > 2 && !ignoreCentering)
            {
                startingPoint.Y -= (float)Math.Floor(thickness / 2.0);
            }

            Rectangle line = new()
            {
                X = (int)startingPoint.X,
                Y = (int)startingPoint.Y,
                Width = width,
                Height = thickness
            };
            DrawRectangle(line, color);
        }

        public static void DrawScaledText(this SpriteFont font, string text, Vector2 position,
            Color color, float scale)
        {
            GuiData.spriteBatch.DrawString(font, text, position, color, 0, Vector2.Zero, scale,
                SpriteEffects.None, 1);
        }

        public static int GetTextHeight(this SpriteFont font, string text, float scale = 1.0f)
        {
            var vec = font.MeasureString(text);
            return (int)(vec.Y * scale);
        }

        public static bool MouseIsHoveringIn(Rectangle target)
        {
            var mousePos = GuiData.getMousePos();

            return mousePos.X >= target.X && mousePos.X <= target.X + target.Width &&
                mousePos.Y >= target.Y && mousePos.Y <= target.Y + target.Height;
        }

        public static bool MouseWasClickedIn(Rectangle target)
        {
            bool hovering = MouseIsHoveringIn(target);
            bool clicked = GuiData.mouseLeftUp();

            return hovering && clicked;
        }

        public static void DrawCenteredText(Rectangle bounds, string text, SpriteFont font)
        {
            Vector2 textVector = font.MeasureString(text);
            Vector2 textPosition = new Vector2(
                (float)(bounds.X + bounds.Width / 2) - textVector.X / 2f,
                (float)(bounds.Y + bounds.Height / 2) - textVector.Y / 2f);

            GuiData.spriteBatch.DrawString(font, text, textPosition, Color.White);
        }
    }
}
