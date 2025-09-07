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
        public class ShowChapterTitle : DelayablePathfinderAction
        {
            [XMLStorage]
            public string ChapterTitle;

            [XMLStorage]
            public string ChapterSubTitle;

            [XMLStorage]
            public bool HideTopBar = true;

            [XMLStorage]
            public string BackingOpacity;

            public override void Trigger(OS os)
            {
                ChapterData chapterData = new();

                if (!BackingOpacity.IsNullOrWhiteSpace())
                {
                    chapterData.BackingOpacity = float.Parse(BackingOpacity);
                }

                if (HideTopBar && !TopBarColorsCache.HasCache)
                {
                    os.DisableTopBarButtons = true;
                    os.DisableEmailIcon = true;

                    TopBarColorsCache.TopBarTextColor = os.topBarTextColor;
                    TopBarColorsCache.TopBarColor = os.topBarColor;

                    os.topBarTextColor = Color.Transparent;
                    os.topBarColor = Color.Transparent;
                }

                os.display.visible = false;
                os.netMap.visible = false;
                os.ram.visible = false;
                os.terminal.visible = false;

                chapterData.Title = ChapterTitle;
                chapterData.Subtitle = ChapterSubTitle;
                StuxnetCore.ChapterData = chapterData;
                StuxnetCore.illustState = States.IllustratorStates.DrawTitle;
            }
        }

        public class HideChapterTitle : DelayablePathfinderAction
        {
            public override void Trigger(OS os)
            {
                os.DisableTopBarButtons = false;

                if(!StuxnetCore.disableAlerts)
                {
                    os.DisableEmailIcon = false;
                }

                if(TopBarColorsCache.HasCache)
                {
                    os.topBarTextColor = TopBarColorsCache.TopBarTextColor;
                    os.topBarColor = TopBarColorsCache.TopBarColor;
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
