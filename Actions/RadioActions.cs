using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet;

using Pathfinder.Action;
using Pathfinder.Util;

namespace Stuxnet_HN.Actions
{
    public class RadioActions
    {
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

        public class PreventRadioAccess : DelayablePathfinderAction
        {
            public override void Trigger(OS os)
            {
                StuxnetCore.allowRadio = false;
            }
        }

        public class AllowRadioAccess : DelayablePathfinderAction
        {
            public override void Trigger(OS os)
            {
                StuxnetCore.allowRadio = true;
            }
        }
    }
}
