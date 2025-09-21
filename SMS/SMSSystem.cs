using Hacknet;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stuxnet_HN.Localization;
using BepInEx;

namespace Stuxnet_HN.SMS
{
    public static class SMSSystem
    {
        public static bool Disabled { get; set; } = false;

        public static bool Active { get; set; } = false;

        public static event Action<string> NewMessageReceived;

        public static List<SMSMessage> ActiveMessages = new();
        public static List<QueuedSMSMessage> QueuedMessages = new();
        public static List<SMSChoice> ActiveChoices = new();
        public static float ChoiceTimer = 0.0f;
        public static Dictionary<string, Color> AuthorColors = new()
        {

        };

        public static void Initialize()
        {
            foreach(var author in StuxnetCore.Configuration.SMS.AuthorColors)
            {
                string parsedAuthor = ComputerLoader.filter(author.Key);
                if (AuthorColors.ContainsKey(parsedAuthor)) continue;
                AuthorColors.Add(parsedAuthor, Utils.convertStringToColor(author.Value));
            }
            if(!AuthorColors.ContainsKey("#PLAYERNAME#"))
            {
                AuthorColors.Add("#PLAYERNAME#", Color.Transparent); // Transparent = theme highlight color
            }
        }

        public static void QueueMessage(SMSMessage message, float delay, string messageID = null)
        {
            if(delay <= 0.0f)
            {
                SendMessage(message);
                return;
            }

            QueuedSMSMessage queuedMessage = new(message, messageID);
            bool exists = false;
            if(messageID != null)
            {
                exists = QueuedMessages.Any(msg => msg.ID == messageID);
            }

            if(exists && !messageID.IsNullOrWhiteSpace())
            {
                StuxnetCore.Logger.LogWarning(string.Format(
                    "Tried to queue message with ID of {0}, but a queued message with that ID already exists! Skipping...",
                    messageID));
                return;
            }

            QueuedMessages.Add(queuedMessage);

            OS.currentInstance.delayer.Post(ActionDelayer.Wait(delay),
                () => { SendMessage(queuedMessage); });
        }

        public static void CancelQueuedMessage(string messageID)
        {
            bool exists = QueuedMessages.Any(msg => msg.ID == messageID);

            if(!exists)
            {
                StuxnetCore.Logger.LogWarning(string.Format(
                    "Tried to cancel queued message with an ID of {0}, but a message with that ID doesn't exist.",
                    messageID));
                return;
            }

            if(OS.DEBUG_COMMANDS)
            {
                StuxnetCore.Logger.LogDebug(string.Format("Cancelled SMS message with ID of {0}", messageID));
            }

            var message = QueuedMessages.First(msg => msg.ID == messageID);
            int msgIndex = QueuedMessages.IndexOf(message);

            QueuedMessages[msgIndex].Cancelled = true;
        }

        public static void SendMessage(QueuedSMSMessage queuedSMSMessage)
        {
            if(queuedSMSMessage.ID != null)
            {
                var queuedMsg = QueuedMessages.First(msg => msg.ID == queuedSMSMessage.ID);
                QueuedMessages.Remove(queuedMsg);
            } else
            {
                QueuedMessages.Remove(queuedSMSMessage);
            }

            if (queuedSMSMessage.Cancelled)
            {
                return;
            }

            var message = queuedSMSMessage.Message;
            SendMessage(message);
        }

        public static void SendMessage(SMSMessage message)
        {
            OS os = OS.currentInstance;

            if (message == null)
            {
                throw new ArgumentNullException("Failed to send SMS message: message cannot be null!");
            }
            if (string.IsNullOrWhiteSpace(message.Author)) message.Author = "UNKNOWN";

            if(!message.IsPlayer || !(message.ChannelName == SMSModule.GlobalInstance.ActiveChannel &&
                SMSModule.GlobalInstance.State == SMSModule.SMSModuleState.ViewMessageHistory))
            {
                if(!SMSModule.GlobalInstance.visible && message.Author != SMSSystemMessage.SYSTEM_AUTHOR && message.Author != "#PLAYERNAME#")
                {
                    string notif = "SMS ALERT: ";
                    notif += string.Format(Localizer.GetLocalized("You received a message from {0}"),
                        message.Author);
                    os.write("--- !");
                    os.write(notif);
                    os.beepSound.Play();
                }
            }

            ActiveMessages.Add(message);
            NewMessageReceived?.Invoke(message.ChannelName);
        }

        public static List<SMSMessage> GetActiveMessagesByAuthor(string author)
        {
            return ActiveMessages.FindAll(msg => msg.Author == author);
        }

        public static List<SMSMessage> GetActiveMessagesForChannel(string channelName)
        {
            return ActiveMessages.FindAll(msg => msg.ChannelName == channelName);
        }

        public static List<string> GetActiveUsersInChannel(string channelName)
        {
            var channelMessages = GetActiveMessagesForChannel(channelName);
            List<string> activeUsers = new()
            {
                OS.currentInstance.SaveGameUserName
            };

            foreach(var msg in channelMessages)
            {
                if(!activeUsers.Contains(msg.Author))
                {
                    activeUsers.Add(msg.Author);
                }
            }

            return activeUsers;
        }

        public static List<string> ActiveChannels
        {
            get
            {
                List<string> channels = new();
                foreach(var msg in ActiveMessages)
                {
                    if(!channels.Contains(msg.ChannelName))
                    {
                        channels.Add(msg.ChannelName);
                    }
                }
                return channels;
            }
        }
    }
}
