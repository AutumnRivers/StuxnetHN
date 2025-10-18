using Pathfinder.Action;
using Hacknet;
using Pathfinder.Util;
using StuxnetHN.Audio.Replacements;
using StuxnetHN.Audio.Patches;

namespace StuxnetHN.Audio.Actions
{
    [Pathfinder.Meta.Load.Action("PlayCustomSong")]
    public class PlaySongAction : DelayablePathfinderAction
    {
        [XMLStorage]
        public string SongFile;

        [XMLStorage]
        public bool Immediately = false;

        [XMLStorage]
        public int BeginLoop = -1;

        [XMLStorage]
        public int EndLoop = -1;

        public override void Trigger(OS os)
        {
            string command = "playCustomSong";
            if (Immediately) command += "Immediatley";
            command += ":" + SongFile;

            MissionFunctions.runCommand(0, command);

            StuxnetMusicManager.LoopBegin = BeginLoop;
            StuxnetMusicManager.LoopEnd = EndLoop;

            StuxnetMusicManager.CurrentSongEntry = null;
            MusicManagerPatches.ActivatedFromAction = true;
        }
    }

    [Pathfinder.Meta.Load.Action("StopMusic")]
    public class StopMusicAction : DelayablePathfinderAction
    {
        [XMLStorage]
        public bool FadeOut = true;

        public override void Trigger(OS os)
        {
            if(FadeOut)
            {
                MusicManager.nextSong = null;
                MusicManager.state = 1;
            } else
            {
                MusicManager.stop();
            }
        }
    }

    [Pathfinder.Meta.Load.Action("PreloadSong")]
    public class SAPreloadSong : DelayablePathfinderAction
    {
        [XMLStorage]
        public string SongFile;

        public override void Trigger(OS os)
        {
            if (!Stuxnet_HN.StuxnetCore.Configuration.Audio.ReplaceMusicManager) return;

            StuxnetMusicManager.Player.PreloadAsync(SongFile);
        }
    }
}
