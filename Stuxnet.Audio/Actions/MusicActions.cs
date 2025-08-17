using Pathfinder.Action;
using Hacknet;
using Pathfinder.Util;

namespace StuxnetHN.Audio.Actions
{
    [Pathfinder.Meta.Load.Action("PlayCustomSong")]
    public class PlaySongAction : DelayablePathfinderAction
    {
        [XMLStorage]
        public string SongFile;

        [XMLStorage]
        public bool Immediately = false;

        public override void Trigger(OS os)
        {
            string command = "playCustomSong";
            if (Immediately) command += "Immediatley";
            command += ":" + SongFile;

            MissionFunctions.runCommand(0, command);
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
}
