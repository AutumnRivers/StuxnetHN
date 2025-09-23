using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using BepInEx;
using Hacknet;
using Pathfinder.Action;
using Pathfinder.Meta.Load;
using Pathfinder.Util;
using Pathfinder.Util.XML;
using Stuxnet_HN.Extensions;

namespace Stuxnet_HN.SMS
{
    /*
     * <SendSMSMessage Author="" ChannelName="" OnReadActions="" MessageID="" MessageDelay="0.0">
     * <Message>Test</Message>
     * </SendSMSMessage>
     */
    [Action("SendSMSMessage")]
    public class SASendSMSMessage : DelayablePathfinderAction
    {
        [XMLStorage]
        public string Author;

        [XMLStorage]
        public string ChannelName = string.Empty;

        [XMLStorage]
        public string OnReadActions = string.Empty;

        [XMLStorage]
        public string MessageID = string.Empty;

        [XMLStorage]
        public float MessageDelay = 0.0f;

        public ElementInfo MainElement;

        public override void Trigger(OS os)
        {
            if(Author == "#PLAYERNAME#" && ChannelName.IsNullOrWhiteSpace())
            {
                throw new FormatException("SendSMSMessage actions that have the player as an author " +
                    "MUST have a valid ChannelName attribute.");
            }
            if(Author == "#PLAYERNAME#")
            {
                Author = ComputerLoader.filter(Author);
            }

            if(ChannelName.IsNullOrWhiteSpace())
            {
                ChannelName = Author;
            }

            SMSMessage message = new()
            {
                Author = Author,
                OnReadActionsFilepath = OnReadActions,
                ChannelName = ChannelName
            };

            foreach(var child in MainElement.Children)
            {
                string nodeID;
                string displayName;

                switch(child.Name)
                {
                    case "Message":
                        message.Content = GetMessageContent(child);
                        break;
                    case "NodeAttachment":
                        displayName = string.Empty;
                        if(child.Attributes.ContainsKey("DisplayName"))
                        {
                            displayName = child.Attributes["DisplayName"];
                        }
                        nodeID = child.ReadRequiredAttribute("NodeID");
                        NodeAttachment nodeAttachment = new(displayName, nodeID);
                        message.AddAttachment(nodeAttachment);
                        break;
                    case "NodeAccount":
                        displayName = string.Empty;
                        if (child.Attributes.ContainsKey("DisplayName"))
                        {
                            displayName = child.Attributes["DisplayName"];
                        }
                        nodeID = child.ReadRequiredAttribute("NodeID");
                        string nodeAccount = child.ReadRequiredAttribute("AccountUsername");
                        NodeAttachment nodeLoginAttachment = new(displayName, nodeID, nodeAccount);
                        message.AddAttachment(nodeLoginAttachment);
                        break;
                    case "UserNote":
                        displayName = child.ReadRequiredAttribute("DisplayName");
                        string noteContent = child.Content;
                        UserNoteAttachment noteAttachment = new(displayName, noteContent);
                        message.AddAttachment(noteAttachment);
                        break;
                    default:
                        StuxnetCore.Logger.LogWarning(string.Format(
                            "Unrecognized child element in SendSMSMessage '{0}'", child.Name));
                        break;
                }
            }

            if(message.Content == null)
            {
                throw new FormatException("SendSMSAction needs a Message child element");
            }

            if(MessageDelay <= 0.0f)
            {
                SMSSystem.SendMessage(message);
            } else
            {
                SMSSystem.QueueMessage(message, MessageDelay, MessageID);
            }
        }

        private string GetMessageContent(ElementInfo contentElem)
        {
            if(contentElem.Content.IsNullOrWhiteSpace())
            {
                throw new FormatException("SendSMSMessage child element 'Message' needs content");
            }

            return contentElem.Content;
        }

        public override void LoadFromXml(ElementInfo info)
        {
            base.LoadFromXml(info);
            MainElement = info;
        }

        public override XElement GetSaveElement()
        {
            var saveElem = base.GetSaveElement();
            foreach(var child in MainElement.Children)
            {
                saveElem.Add(child.ConvertToXElement());
            }
            return saveElem;
        }
    }

    [Action("CancelSMSMessage")]
    public class SACancelSMSMessage : PathfinderAction
    {
        [XMLStorage]
        public string MessageID;

        public override void Trigger(object os_obj)
        {
            if(MessageID.IsNullOrWhiteSpace())
            {
                throw new FormatException("MessageID attribute cannot be blank in CancelSMSMessage!");
            }

            SMSSystem.CancelQueuedMessage(MessageID);
        }
    }

    [Action("SMSShowChoices")]
    public class SASMSShowChoices : DelayablePathfinderAction
    {
        [XMLStorage]
        public string ChannelName;

        [XMLStorage]
        public float Timer = 0.0f;

        public List<ElementInfo> ChildElements;

        public override void Trigger(OS os)
        {
            if(ChildElements.Count <= 0)
            {
                throw new FormatException("SMSShowChoices needs at least one valid choice element!");
            }

            if(SMSSystem.ActiveChoices.Count > 0)
            {
                throw new Exception("You cannot show SMS choices when there are already active choices!");
            }

            List<SMSChoice> choices = new();

            foreach(var child in ChildElements)
            {
                if(child.Name != "Choice")
                {
                    StuxnetCore.Logger.LogWarning(string.Format(
                        "Unrecognized child element '{0}' in SMSShowChoice action. Skipping...",
                        child.Name));
                }

                string content = child.Content;
                if(content.IsNullOrWhiteSpace())
                {
                    throw new FormatException("SMSShowChoice child elements must have non-empty content!");
                }

                string actions = child.ReadRequiredAttribute("OnChosenActions");

                if (choices.Count >= 3) break;
                choices.Add(new SMSChoice(content, actions, ChannelName));
            }

            if(Timer > 0.0f)
            {
                SMSSystem.ChoiceTimer = Timer;
            }

            SMSSystem.ActiveChoices = choices;
        }

        public override void LoadFromXml(ElementInfo info)
        {
            base.LoadFromXml(info);
            ChildElements = info.Children;
        }

        public override XElement GetSaveElement()
        {
            var elem = base.GetSaveElement();
            foreach(var child in ChildElements)
            {
                elem.Add(child.ConvertToXElement());
            }
            return elem;
        }
    }

    [Action("SMSHideChoices")]
    public class SAHideChoices : DelayablePathfinderAction
    {
        [XMLStorage]
        public string OnSuccessActions;

        public override void Trigger(OS os)
        {
            if(SMSSystem.ActiveChoices.Any())
            {
                RunnableConditionalActions.LoadIntoOS(OnSuccessActions, os);
                SMSSystem.ActiveChoices.Clear();
            }
        }
    }

    [Action("ForceOpenSMSChannel")]
    public class SAForceOpenSMSChannel : DelayablePathfinderAction
    {
        [XMLStorage]
        public string ChannelName;

        public override void Trigger(OS os)
        {
            SMSModule.GlobalInstance.ActiveChannel = ChannelName;
            SMSModule.GlobalInstance.State = SMSModule.SMSModuleState.ViewMessageHistory;
            SMSModule.Activate();
        }
    }

    [Action("SMSBlockUser")]
    public class SABlockUser : DelayablePathfinderAction
    {
        [XMLStorage]
        public string UserBeingBlocked;

        [XMLStorage]
        public string BlockingUser = "#PLAYERNAME#";

        public override void Trigger(OS os)
        {
            if(UserBeingBlocked == BlockingUser)
            {
                throw new FormatException("SMSBlockUser : Users cannot block themselves!");
            }

            if (BlockingUser != "#PLAYERNAME#" && UserBeingBlocked != "#PLAYERNAME#")
            {
                StuxnetCore.Logger.LogWarning("SMSBlockUser : The player not being involved in the " +
                    "blocking won't do anything. Skipping...");
                return;
            }

            SMSSystemMessage blockedMessage;

            if(UserBeingBlocked == "#PLAYERNAME#")
            {
                blockedMessage = new()
                {
                    Author = "System",
                    Content = SMSSystemMessage.GetSystemMessage("blockplayer"),
                    ChannelName = BlockingUser
                };
            } else
            {
                blockedMessage = new()
                {
                    Author = "System",
                    Content = SMSSystemMessage.GetSystemMessage("blockedbyplayer"),
                    ChannelName = BlockingUser
                };
            }

            if(blockedMessage.ChannelName == "#PLAYERNAME#")
            {
                blockedMessage.ChannelName = UserBeingBlocked;
            }

            SMSSystem.SendMessage(blockedMessage);
        }
    }

    [Action("SMSAddUser")]
    public class AddUser : DelayablePathfinderAction
    {
        [XMLStorage]
        public string UserBeingAdded;

        [XMLStorage]
        public string UserAdding;

        [XMLStorage]
        public string ChannelName;

        public override void Trigger(OS os)
        {
            if (UserBeingAdded == UserAdding) return;

            SMSSystemMessage systemMessage;
            if(UserBeingAdded == "#PLAYERNAME#" && UserAdding == ChannelName)
            {
                systemMessage = new()
                {
                    Author = SMSSystemMessage.SYSTEM_AUTHOR,
                    Content = SMSSystemMessage.GetSystemMessage("addfriend", UserAdding),
                    ChannelName = ChannelName
                };
            } else
            {
                systemMessage = new()
                {
                    Author = SMSSystemMessage.SYSTEM_AUTHOR,
                    Content = SMSSystemMessage.GetSystemMessage("addtogc", UserAdding, UserBeingAdded),
                    ChannelName = ChannelName
                };
            }

            SMSSystem.SendMessage(systemMessage);
        }
    }

    [Action("SMSGoOffline")]
    public class SAGoOffline : DelayablePathfinderAction
    {
        [XMLStorage]
        public string User;

        [XMLStorage]
        public string ChannelName = string.Empty;

        public override void Trigger(OS os)
        {
            if(ChannelName.IsNullOrWhiteSpace())
            {
                ChannelName = User;
            }

            SMSSystemMessage systemMessage = new()
            {
                Author = SMSSystemMessage.SYSTEM_AUTHOR,
                Content = SMSSystemMessage.GetSystemMessage("gooffline", User),
                ChannelName = ChannelName
            };

            SMSSystem.SendMessage(systemMessage);
        }
    }

    [Action("SMSFailSendMessage")]
    public class SAFailMessage : DelayablePathfinderAction
    {
        [XMLStorage]
        public string ChannelName;

        public override void Trigger(OS os)
        {
            SMSSystemMessage systemMessage = new()
            {
                Author = SMSSystemMessage.SYSTEM_AUTHOR,
                Content = SMSSystemMessage.GetSystemMessage("failsend"),
                ChannelName = ChannelName
            };

            SMSSystem.SendMessage(systemMessage);
        }
    }

    [Action("ForceCloseSMS")]
    public class SAForceCloseSMS : DelayablePathfinderAction
    {
        public override void Trigger(OS os)
        {
            SMSModule.Deactivate();
        }
    }

    [Action("ForbidSMS")]
    public class SAForbidSMS : DelayablePathfinderAction
    {
        [XMLStorage]
        public bool AllowSMS = false;

        public override void Trigger(OS os)
        {
            SMSSystem.Disabled = !AllowSMS;
        }
    }
}
