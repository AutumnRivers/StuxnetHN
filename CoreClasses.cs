using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Stuxnet_HN.Cutscenes;
using Stuxnet_HN.Patches;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public static List<StuxnetCutscene> Cutscenes { get; private set; } = new();

        public static bool TryGetCachedTheme(string filepath, out AnimatedTheme theme)
        {
            return TryGetCachedItem(AnimatedThemes, th => th.FilePath == filepath, out theme);
        }

        public static bool TryGetCachedCutscene(string filepath, out StuxnetCutscene cutscene)
        {
            return TryGetCachedItem(Cutscenes, cs => cs.FilePath == filepath, out cutscene);
        }

        public static bool TryGetCachedItem<T>(List<T> cacheCollection, Func<T, bool> predicate, out T item)
        {
            item = cacheCollection.FirstOrDefault(predicate);
            return item != null && !item.Equals(default(T));
        }

        public const int MAX_CACHE_SIZE = 10;

        public static void CacheTheme(AnimatedTheme theme)
        {
            CacheItem(AnimatedThemes, theme, th => th.FilePath == theme.FilePath);
        }

        public static void CacheCutscene(StuxnetCutscene cutscene)
        {
            CacheItem(Cutscenes, cutscene, cs => cs.FilePath == cutscene.FilePath);
        }

        public static void CacheItem<T>(List<T> cacheCollection, T newItem, Func<T, bool> predicate)
        {
            if (TryGetCachedItem(cacheCollection, predicate, out T existingItem))
            {
                int index = cacheCollection.IndexOf(existingItem);
                if (index != -1)
                {
                    cacheCollection[index] = newItem;
                }
            }
            else
            {
                if (cacheCollection.Count >= MAX_CACHE_SIZE)
                {
                    cacheCollection.RemoveAt(0);
                }
                cacheCollection.Add(newItem);
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
                return TopBarTextColor != default || TopBarColor != default;
            }
        }

        public static void ClearCache()
        {
            TopBarColor = default;
            TopBarTextColor = default;
        }
    }
}
