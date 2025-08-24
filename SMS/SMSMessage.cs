using System;
using System.Collections.Generic;
using BepInEx;
using Hacknet;
using Microsoft.Xna.Framework.Audio;
using Pathfinder.GUI;
using Pathfinder.Util;

namespace Stuxnet_HN.SMS
{
    public class SMSMessage
    {
        public string Author { get; set; }
        public string ChannelName { get; set; }
        public string Content { get; set; }
        public List<SMSAttachment> Attachments { get; set; } = new();
        public bool HasBeenRead { get; set; } = false;
        public string OnReadActionsFilepath { get; set; } = null;

        public bool IsPlayer => Author == ComputerLoader.filter("#PLAYERNAME#");

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
        public string AccountUsername;

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
