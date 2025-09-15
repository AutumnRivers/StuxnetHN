using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using BepInEx;
using Hacknet;
using Microsoft.Xna.Framework.Audio;
using Pathfinder.GUI;
using Pathfinder.Util;
using Stuxnet_HN.Localization;

namespace Stuxnet_HN.SMS
{
    public class SMSMessage
    {
        public string Author { get; set; }
        public string ChannelName { get; set; }
        public string Content { get; set; }
        public List<SMSAttachment> Attachments { get; set; } = new();
        public bool HasBeenRead { get; set; } = false;
        public string OnReadActionsFilepath { get; set; } = string.Empty;
        public string Guid { get; set; } = string.Empty;

        public bool IsPlayer => Author == ComputerLoader.filter("#PLAYERNAME#");

        public SMSMessage()
        {
            Guid = GenerateGuid();
        }

        private static string GenerateGuid()
        {
            string guid = System.Guid.NewGuid().ToString();

            if (SMSSystem.ActiveMessages.Any(msg => msg.Guid == guid)) return GenerateGuid();

            return guid;
        }

        public void AddAttachment(SMSAttachment attachment)
        {
            if (Attachments.Count >= 3) return;
            Attachments.Add(attachment);
        }

        public void ReadMessage()
        {
            if(!OnReadActionsFilepath.IsNullOrWhiteSpace() && !HasBeenRead)
            {
                RunnableConditionalActions.LoadIntoOS(OnReadActionsFilepath, OS.currentInstance);
            }
            HasBeenRead = true;
        }

        public const string MESSAGE_SAVE_ELEMENT = "SMSMessage";

        public XElement GetSaveElement()
        {
            XElement messageElement = new(MESSAGE_SAVE_ELEMENT);
            XAttribute author = new("Author", Author);
            XAttribute channel = new("Channel", ChannelName);
            XAttribute read = new("Read", HasBeenRead);
            XAttribute actions = new("Actions", OnReadActionsFilepath);
            XAttribute guid = new("Guid", Guid);
            messageElement.Add(author, channel, read, actions, guid);

            XElement contentElement = new("MessageContent")
            {
                Value = Content
            };
            messageElement.Add(contentElement);

            XElement attachmentsElement = new("Attachments");
            foreach(var attach in Attachments)
            {
                XElement attachElem;
                XAttribute attachName = new("DisplayName", attach.DisplayName);
                
                if(attach is NodeAttachment nodeAttachment)
                {
                    attachElem = new("NodeAttachment");
                    XAttribute nodeID = new("NodeID", nodeAttachment.NodeID);
                    XAttribute acctUsername = new("Username", nodeAttachment.AccountUsername);
                    attachElem.Add(nodeID, acctUsername);
                } else if(attach is UserNoteAttachment noteAttach)
                {
                    attachElem = new("UserNoteAttachment")
                    {
                        Value = noteAttach.Content
                    };
                } else
                {
                    throw new Exception(
                        string.Format("Unrecognized SMS attachment type '{0}'",
                        attach.GetType())
                        );
                }

                attachElem.Add(attachName);
                attachmentsElement.Add(attachElem);
            }
            messageElement.Add(attachmentsElement);

            return messageElement;
        }

        public static SMSMessage Deserialize(XElement saveNode)
        {
            SMSMessage message = new()
            {
                Author = saveNode.Attribute("Author").Value,
                ChannelName = saveNode.Attribute("Channel").Value,
                HasBeenRead = bool.Parse(saveNode.Attribute("Read").Value),
                OnReadActionsFilepath = saveNode.Attribute("Actions").Value,
                Content = saveNode.Element("MessageContent").Value,
                Guid = saveNode.Attribute("Guid").Value
            };

            foreach(var attachment in saveNode.Element("Attachments").Elements())
            {
                switch(attachment.Name.ToString())
                {
                    case "NodeAttachment":
                        NodeAttachment nodeAttachment = new(
                            attachment.Attribute("DisplayName").Value,
                            attachment.Attribute("NodeID").Value);
                        var username = attachment.Attribute("Username").Value;
                        if(!username.IsNullOrWhiteSpace())
                        {
                            nodeAttachment.AccountUsername =
                                attachment.Attribute("Username").Value;
                        }
                        message.AddAttachment(nodeAttachment);
                        break;
                    case "UserNoteAttachment":
                        UserNoteAttachment noteAttachment = new(
                            attachment.Attribute("DisplayName").Value,
                            attachment.Value);
                        message.AddAttachment(noteAttachment);
                        break;
                }
            }

            if(message.Author == SMSSystemMessage.SYSTEM_AUTHOR)
            {
                message = (SMSSystemMessage)message;
            }

            return message;
        }
    }

    public class SMSSystemMessage : SMSMessage
    {
        public const string SYSTEM_AUTHOR = "SMSSystemAuthor_DoNotUse";

        public SMSSystemMessage() : base() { }

        public static string GetSystemMessage(string messageID, params string[] args)
        {
            string template = messageID switch
            {
                "gooffline" => "{0} is offline...",
                "addfriend" => "{0} added you",
                "addtogc" => "{0} added {1} to the group",
                "blockplayer" => "You are blocked by this user.",
                "blockedbyplayer" => "You have blocked this user.",
                _ => "Message Failed To Send",
            };
            for(int idx = 0; idx < args.Length; idx++)
            {
                var value = args[idx];
                if (string.IsNullOrWhiteSpace(value)) continue;
                args[idx] = ComputerLoader.filter(value);
            }
            return string.Format(Localizer.GetLocalized(template), args);
        }
    }

    public class QueuedSMSMessage
    {
        public SMSMessage Message { get; set; }
        public string ID { get; set; }
        public bool Cancelled { get; set; } = false;

        public QueuedSMSMessage(SMSMessage message, string id)
        {
            Message = message;
            ID = id;
        }

        public XElement GetSaveElement()
        {
            XElement qElement = new("QueuedMessage");
            XAttribute idAttr = new("ID", ID);
            XAttribute cancelAttr = new("Cancelled", Cancelled);
            qElement.Add(idAttr, cancelAttr);
            qElement.AddFirst(Message.GetSaveElement());
            return qElement;
        }

        public static QueuedSMSMessage Deserialize(XElement saveNode)
        {
            SMSMessage message = SMSMessage.Deserialize(saveNode.Element(SMSMessage.MESSAGE_SAVE_ELEMENT));
            string ID = saveNode.Attribute("ID").Value;
            bool cancelled = bool.Parse(saveNode.Attribute("Cancelled").Value);
            return new(message, ID)
            {
                Cancelled = cancelled
            };
        }
    }

    public class SMSChoice
    {
        public string Content { get; set; }
        public string OnClickedActions { get; set; }
        public string ChannelName { get; set; }

        public SMSChoice(string content, string actions, string channel)
        {
            Content = content;
            OnClickedActions = actions;
            ChannelName = channel;
        }

        public void Chosen()
        {
            RunnableConditionalActions.LoadIntoOS(OnClickedActions, OS.currentInstance);
        }

        public const string CHOICE_ELEMENT = "SMSChoice";

        public XElement GetSaveElement()
        {
            XElement choiceElem = new(CHOICE_ELEMENT);
            XAttribute actions = new("Actions", OnClickedActions);
            XAttribute channel = new("Channel", ChannelName);
            choiceElem.Add(actions, channel);
            choiceElem.Value = Content;

            return choiceElem;
        }

        public static SMSChoice Deserialize(XElement saveNode)
        {
            string actions = saveNode.Attribute("Actions").Value;
            string channel = saveNode.Attribute("Channel").Value;
            return new(saveNode.Value, actions, channel);
        }
    }

    public abstract class SMSAttachment
    {
        public string DisplayName { get; set; }
        public int ButtonID;

        public SMSAttachment(string displayName)
        {
            DisplayName = displayName;
            ButtonID = PFButton.GetNextID();
        }

        public abstract void OnButtonClicked();
    }

    public class NodeAttachment : SMSAttachment
    {
        public static SoundEffect BipSound;

        public string NodeID;
        public string AccountUsername = string.Empty;

        public Computer AssociatedComputer;

        public NodeAttachment(string displayName, string nodeID) : base(displayName)
        {
            NodeID = nodeID;
        }

        public NodeAttachment(string displayName, string nodeID, string username) : base(displayName)
        {
            NodeID = nodeID;
            AccountUsername = username;
        }

        public override void OnButtonClicked()
        {
            Computer comp;
            if (AssociatedComputer != null)
            {
                comp = AssociatedComputer;
                addToNetMap();
                return;
            }

            comp = ComputerLookup.FindById(NodeID);
            if(comp ==  null)
            {
                string error = string.Format("Node with ID of '{0}' could not be found", NodeID);
                throw new Exception(error);
            }
            AssociatedComputer = comp;
            addToNetMap();

            void addToNetMap()
            {
                NetworkMap netMap = OS.currentInstance.netMap;

                netMap.discoverNode(AssociatedComputer);
                BipSound.Play();

                if (!AccountUsername.IsNullOrWhiteSpace())
                {
                    for (int i = 0; i < comp.users.Count; i++)
                    {
                        var user = comp.users[i];
                        if (user.name != AccountUsername) continue;

                        user.known = true;
                        comp.users[i] = user;
                        break;
                    }
                }
            }
        }
    }

    public class UserNoteAttachment : SMSAttachment
    {
        public string Content { get; set; }

        public UserNoteAttachment(string displayName, string content) : base(displayName)
        {
            DisplayName = displayName;
            Content = content;
        }

        public override void OnButtonClicked()
        {
            NotesExe.AddNoteToOS(Content, OS.currentInstance);
        }
    }
}
