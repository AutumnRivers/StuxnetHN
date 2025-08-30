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
using BepInEx;
using Pathfinder.Meta.Load;

namespace Stuxnet_HN.Actions.Dialogue
{
    public class ChapterTitleActions
    {
        [Action("ShowChapterTitle")]
        public class ShowChapterTitle : DelayablePathfinderAction
        {
            [XMLStorage]
            public string ChapterTitle;

            [XMLStorage]
            public string ChapterSubTitle;

            [XMLStorage]
            public string HideTopBar = "true";

            [XMLStorage]
            public string BackingOpacity;

            public override void Trigger(OS os)
            {
                if (!BackingOpacity.IsNullOrWhiteSpace())
                {
                    StuxnetCore.backingOpacity = float.Parse(BackingOpacity);
                }

                if (HideTopBar.ToLower() == "true")
                {
                    os.DisableTopBarButtons = true;
                    os.DisableEmailIcon = true;

                    StuxnetCore.colorsCache["topBarTextColor"] = os.topBarTextColor;
                    StuxnetCore.colorsCache["topBarColor"] = os.topBarColor;

                    os.topBarTextColor = Color.Transparent;
                    os.topBarColor = Color.Transparent;
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

        [Action("HideChapterTitle")]
        public class HideChapterTitle : DelayablePathfinderAction
        {
            public override void Trigger(OS os)
            {
                os.DisableTopBarButtons = false;

                if(!StuxnetCore.disableAlerts)
                {
                    os.DisableEmailIcon = false;
                }

                if(StuxnetCore.colorsCache.ContainsKey("topBarTextColor"))
                {
                    os.topBarTextColor = StuxnetCore.colorsCache["topBarTextColor"];
                    os.topBarColor = StuxnetCore.colorsCache["topBarColor"];
                }

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
