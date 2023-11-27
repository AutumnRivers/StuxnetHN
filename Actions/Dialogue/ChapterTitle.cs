using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet;

using Pathfinder.Action;
using Pathfinder.Util;

using Microsoft.Xna.Framework;

using Stuxnet_HN.Static;

namespace Stuxnet_HN.Actions.Dialogue
{
    public class ChapterTitleActions
    {
        public class ShowChapterTitle : DelayablePathfinderAction
        {
            [XMLStorage]
            public string ChapterTitle;

            [XMLStorage]
            public string ChapterSubTitle;

            [XMLStorage]
            public string HideTopBar = "true";

            public override void Trigger(OS os)
            {
                if(HideTopBar.ToLower() == "true")
                {
                    os.DisableTopBarButtons = true;
                    os.DisableEmailIcon = true;
                }

                os.display.visible = false;
                os.netMap.visible = false;
                os.ram.visible = false;
                os.terminal.visible = false;

                StuxnetCore.chapterTitle = ChapterTitle;
                StuxnetCore.chapterSubTitle = ChapterSubTitle;
                StuxnetCore.illustState = States.IllustratorStates.DrawTitle;
            }
        }

        public class HideChapterTitle : DelayablePathfinderAction
        {
            public override void Trigger(OS os)
            {
                os.DisableTopBarButtons = false;
                os.DisableEmailIcon = false;

                os.display.visible = true;
                os.netMap.visible = true;
                os.ram.visible = true;
                os.terminal.visible = true;

                if (StuxnetCore.illustState == States.IllustratorStates.DrawTitle)
                {
                    StuxnetCore.illustState = States.IllustratorStates.None;
                }
            }
        }
    }
}
