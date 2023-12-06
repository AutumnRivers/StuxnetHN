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

namespace Stuxnet_HN.Actions.Dialogue
{
    public class VisualNovelText
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
            public string TextSpeed = "1";

            [XMLStorage]
            public string HideTopBar = "true";

            public override void Trigger(object os_obj)
            {
                if(StuxnetCore.dialogueIsActive)
                {
                    return;
                }

                OS os = (OS)os_obj;

                if (HideTopBar.ToLower() == "true")
                {
                    os.DisableTopBarButtons = true;
                    os.DisableEmailIcon = true;

                    StuxnetCore.colorsCache["topBarTextColor"] = os.topBarTextColor;
                    StuxnetCore.colorsCache["topBarColor"] = os.topBarColor;

                    os.topBarTextColor = Color.Transparent;
                    os.topBarColor = Color.Transparent;
                }

                float dialogueSpeed = float.Parse(TextSpeed);

                StuxnetCore.dialogueText = Text;
                StuxnetCore.dialogueSpeed = dialogueSpeed;
                StuxnetCore.dialogueEndActions = EndDialogueActions;
                StuxnetCore.dialogueColor = new Color().FromString(TextColor);

                StuxnetCore.dialogueIsCtc = true;
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
            public string TextSpeed = "1";

            [XMLStorage]
            public string ContinueDelay = "0";

            [XMLStorage]
            public string EndDialogueActions;

            [XMLStorage]
            public string HideTopBar = "true";

            public override void Trigger(object os_obj)
            {
                if (StuxnetCore.dialogueIsActive)
                {
                    return;
                }

                OS os = (OS)os_obj;

                if (HideTopBar.ToLower() == "true")
                {
                    os.DisableTopBarButtons = true;
                    os.DisableEmailIcon = true;

                    StuxnetCore.colorsCache["topBarTextColor"] = os.topBarTextColor;
                    StuxnetCore.colorsCache["topBarColor"] = os.topBarColor;

                    os.topBarTextColor = Color.Transparent;
                    os.topBarColor = Color.Transparent;
                }

                float continueDelay = float.Parse(ContinueDelay);

                StuxnetCore.dialogueIsCtc = false;
                StuxnetCore.dialogueCompleteDelay = continueDelay;
                StuxnetCore.dialogueIsActive = true;

                float dialogueSpeed = float.Parse(TextSpeed);

                StuxnetCore.dialogueText = Text;
                StuxnetCore.dialogueSpeed = dialogueSpeed;
                StuxnetCore.dialogueEndActions = EndDialogueActions;
                StuxnetCore.dialogueColor = new Color().FromString(TextColor);

                StuxnetCore.illustState = Static.States.IllustratorStates.AutoDialogue;

                os.display.visible = false;
                os.netMap.visible = false;
                os.ram.visible = false;
                os.terminal.visible = false;
            }
        }
    }
}
