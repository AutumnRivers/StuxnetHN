using System.Linq;

using Hacknet;

using Pathfinder.Action;
using Pathfinder.Meta.Load;
using Pathfinder.Util;

namespace Stuxnet_HN.Actions
{
    public class RadioActions
    {
        [Action("AddSongToRadio")]
        public class AddSong : PathfinderAction
        {
            [XMLStorage]
            public string SongID;

            public override void Trigger(object os_obj)
            {
                string[] songIDs = new string[]{};

                if(SongID.Contains(",")) { songIDs = SongID.Split(','); }

                if(songIDs.Any())
                {
                    foreach(string song in songIDs)
                    {
                        StuxnetCore.unlockedRadio.Add(song);
                    }
                } else
                {
                    StuxnetCore.unlockedRadio.Add(SongID);
                }
            }
        }

        [Action("RemoveSongFromRadio")]
        public class RemoveSong : PathfinderAction
        {
            [XMLStorage]
            public string SongID;

            public override void Trigger(object os_obj)
            {
                if(!StuxnetCore.unlockedRadio.Contains(SongID)) { return; }

                StuxnetCore.unlockedRadio.Remove(SongID);
            }
        }

        [Action("PreventRadioAccess")]
        public class PreventRadioAccess : DelayablePathfinderAction
        {
            public override void Trigger(OS os)
            {
                StuxnetCore.allowRadio = false;
            }
        }

        [Action("AllowRadioAccess")]
        public class AllowRadioAccess : DelayablePathfinderAction
        {
            public override void Trigger(OS os)
            {
                StuxnetCore.allowRadio = true;
            }
        }
    }
}
