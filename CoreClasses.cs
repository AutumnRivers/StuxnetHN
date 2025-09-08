using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Stuxnet_HN.Patches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stuxnet_HN
{
    public class VisualNovelTextData
    {
        public string Text;
        public bool IsCtc = false;
        public float CompleteDelay = 0f;
        public float Speed = 1f;
        public string EndActions;
        public Color Color = Color.White;
        public float BackingOpacity = 0.6f;
    }

    public class ChapterData
    {
        public string Title;
        public string Subtitle;
        public Color Color;
        public float BackingOpacity;
    }

    public static class StuxnetCache
    {
        public static List<AnimatedTheme> AnimatedThemes { get; private set; } = new();
        public static List<Texture2D> Images { get; private set; } = new();

        public static bool TryGetCachedTheme(string filepath, out AnimatedTheme theme)
        {
            theme = null;
            if (!AnimatedThemes.Any(theme => theme.Filepath == filepath)) return false;

            theme = AnimatedThemes.Find(theme => theme.Filepath == filepath);
            return true;
        }

        public static void CacheTheme(AnimatedTheme theme)
        {
            if(TryGetCachedTheme(theme.Filepath, out var existingTheme))
            {
                int index = AnimatedThemes.IndexOf(existingTheme);
                AnimatedThemes[index] = theme;
            } else
            {
                AnimatedThemes.Add(theme);
            }
        }
    }

    public static class TopBarColorsCache
    {
        public static Color TopBarTextColor;
        public static Color TopBarColor;

        public static bool HasCache
        {
            get
            {
                return TopBarTextColor != default && TopBarColor != default;
            }
        }

        public static void ClearCache()
        {
            TopBarColor = default;
            TopBarTextColor = default;
        }
    }
}
