using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

using System;
using Hacknet;

namespace Stuxnet_HN.Extensions
{
    public static class TextureHelper
    {
        public static Vector2 GetSizeAspect(this Texture2D originalTexture, float newWidth, float newHeight)
        {
            // Calculate scaling factors
            float scaleX = (float)newWidth / originalTexture.Width;
            float scaleY = (float)newHeight / originalTexture.Height;

            // Choose the smaller scale factor to maintain aspect ratio
            float scale = Math.Min(scaleX, scaleY);

            // Calculate new dimensions
            float scaledWidth = originalTexture.Width * scale;
            float scaledHeight = originalTexture.Height * scale;

            return new Vector2(scaledWidth, scaledHeight);
        }

        public static Vector2 GetSizeAspect(this Rectangle originalTexture, float newWidth, float newHeight)
        {
            // Calculate scaling factors
            float scaleX = (float)newWidth / originalTexture.Width;
            float scaleY = (float)newHeight / originalTexture.Height;

            // Choose the smaller scale factor to maintain aspect ratio
            float scale = Math.Min(scaleX, scaleY);

            // Calculate new dimensions
            float scaledWidth = originalTexture.Width * scale;
            float scaledHeight = originalTexture.Height * scale;

            return new Vector2(scaledWidth, scaledHeight);
        }

        public static Vector2 LerpSizeAspect(this Rectangle rect, Vector2 targetSize, Vector2 originSize, float tweenAmount)
        {
            Vector2 target = Vector2.Lerp(originSize, targetSize, tweenAmount);

            return rect.GetSizeAspect(target.X, target.Y);
        }
    }
}
