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
            Console.WriteLine(message.ChannelName);

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
}
