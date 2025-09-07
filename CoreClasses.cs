using Microsoft.Xna.Framework;
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
