using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Pathfinder.Action;
using Hacknet;
using Pathfinder.Util;

namespace StuxnetHN.Audio.Actions
{
    [Pathfinder.Meta.Load.Action("PlayCustomSong")]
    public class PlaySongAction : PathfinderAction
    {
        [XMLStorage]
        public string SongFile;

        [XMLStorage]
        public bool Immediately = false;

        public override void Trigger(object os_obj)
        {
            string command = "playCustomSong";
            if (Immediately) command += "Immediatley";
            command += ":" + SongFile;

            MissionFunctions.runCommand(0, command);
        }
    }

    [Pathfinder.Meta.Load.Action("StopMusic")]
    public class StopMusicAction : PathfinderAction
    {
        [XMLStorage]
        public bool FadeOut = true;

        public override void Trigger(object os_obj)
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
