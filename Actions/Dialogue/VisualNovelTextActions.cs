using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet;

using Pathfinder.Action;
using Pathfinder.Util;

using Microsoft.Xna.Framework;
using Stuxnet_HN.Extensions;
using BepInEx;
using Pathfinder.Meta.Load;

namespace Stuxnet_HN.Actions.Dialogue
{
    public class VisualNovelTextActions
    {
        public class CTCDialogueAction : PathfinderAction
        {
            [XMLStorage(IsContent = true)]
            public string Text;

            [XMLStorage]
            public string EndDialogueActions;

            [XMLStorage]
            public string TextColor = "255,255,255";

            [XMLStorage]
            public float TextSpeed = 1f;

            [XMLStorage]
            public bool HideTopBar = true;

            [XMLStorage]
            public float BackingOpacity = 0f;

            public override void Trigger(object os_obj)
            {
                if(StuxnetCore.dialogueIsActive)
                {
                    return;
                }

                VisualNovelTextData vnTextData = new()
                {
                    IsCtc = true
                };

                if(BackingOpacity > 0f)
                {
                    vnTextData.BackingOpacity = BackingOpacity;
                }

                OS os = (OS)os_obj;

                if (HideTopBar && !TopBarColorsCache.HasCache)
                {
                    os.DisableTopBarButtons = true;
                    os.DisableEmailIcon = true;

                    TopBarColorsCache.TopBarTextColor = os.topBarTextColor;
                    TopBarColorsCache.TopBarColor = os.topBarColor;

                    os.topBarTextColor = Color.Transparent;
                    os.topBarColor = Color.Transparent;
                }

                vnTextData.Text = Text;
                vnTextData.Speed = TextSpeed;
                vnTextData.EndActions = EndDialogueActions;
                vnTextData.Color = Utils.convertStringToColor(TextColor);

                StuxnetCore.CurrentVNTextData = vnTextData;

                StuxnetCore.dialogueIsActive = true;
                StuxnetCore.illustState = Static.States.IllustratorStates.CTCDialogue;

                os.display.visible = false;
                os.netMap.visible = false;
                os.ram.visible = false;
                os.terminal.visible = false;
            }
        }

        public class AutoDialogueAction : PathfinderAction
        {
            [XMLStorage(IsContent = true)]
            public string Text;

            [XMLStorage]
            public string TextColor = "255,255,255";

            [XMLStorage]
            public float TextSpeed = 1;

            [XMLStorage]
            public float ContinueDelay = 0f;

            [XMLStorage]
            public string EndDialogueActions;

            [XMLStorage]
            public bool HideTopBar = true;

            [XMLStorage]
            public float BackingOpacity = 0f;

            public override void Trigger(object os_obj)
            {
                if (StuxnetCore.dialogueIsActive)
                {
                    return;
                }

                VisualNovelTextData vnTextData = new()
                {
                    IsCtc = false
                };

                if (BackingOpacity > 0f)
                {
                    vnTextData.BackingOpacity = BackingOpacity;
                }

                OS os = (OS)os_obj;

                if (HideTopBar && !TopBarColorsCache.HasCache)
                {
                    os.DisableTopBarButtons = true;
                    os.DisableEmailIcon = true;

                    TopBarColorsCache.TopBarTextColor = os.topBarTextColor;
                    TopBarColorsCache.TopBarColor = os.topBarColor;

                    os.topBarTextColor = Color.Transparent;
                    os.topBarColor = Color.Transparent;
                }

                vnTextData.CompleteDelay = ContinueDelay;
                vnTextData.Text = Text;
                vnTextData.Speed = TextSpeed;
                vnTextData.EndActions = EndDialogueActions;
                vnTextData.Color = Utils.convertStringToColor(TextColor);

                StuxnetCore.CurrentVNTextData = vnTextData;

                StuxnetCore.dialogueIsActive = true;
                StuxnetCore.illustState = Static.States.IllustratorStates.AutoDialogue;

                os.display.visible = false;
                os.netMap.visible = false;
                os.ram.visible = false;
                os.terminal.visible = false;
            }
        }
    }
}
