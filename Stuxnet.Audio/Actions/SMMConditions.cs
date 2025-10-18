using Pathfinder.Action;
using Pathfinder.Meta.Load;
using Pathfinder.Util;
using StuxnetHN.Audio.Replacements;

namespace StuxnetHN.Audio.Actions
{
    [Condition("OnSongInCache")]
    public class SCOnSongPreloaded : PathfinderCondition
    {
        [XMLStorage]
        public string SongFile;

        [XMLStorage]
        public bool CacheIfNotExists = false;

        public bool forceCached = false;

        public override bool Check(object os_obj)
        {
            bool exists = StuxnetMusicManager.Player.HasCachedSong(SongFile);
            if(!exists && CacheIfNotExists && !forceCached)
            {
                forceCached = true;
                StuxnetMusicManager.Player.PreloadAsync(SongFile);
            }
            return !Stuxnet_HN.StuxnetCore.Configuration.Audio.ReplaceMusicManager || StuxnetMusicManager.Player.HasCachedSong(SongFile);
        }
    }
}
