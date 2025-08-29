using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Pathfinder.Meta.Load;
using Pathfinder.Event.Saving;
using Hacknet;
using HarmonyLib;
using System.Xml.Linq;
using Pathfinder.Replacements;
using Pathfinder.Util.XML;

namespace Stuxnet_HN.SMS
{
    public static class SMSSaveConstants
    {
        public const string BASE_SMS_SAVE_ELEMENT = "HacknetSave.StuxnetSMS.";
        public const string SMS_MESSAGES_ELEMENT = BASE_SMS_SAVE_ELEMENT + "ActiveMessages";
        public const string QSMS_MESSAGES_ELEMENT = BASE_SMS_SAVE_ELEMENT + "QueuedMessages";
        public const string CHOICES_ELEMENT = BASE_SMS_SAVE_ELEMENT + "ActiveChoices";
    }

    public class SMSSavePatch
    {
        [Event()]
        public static void SaveSMSDataToSaveFile(SaveEvent saveEvent)
        {
            var save = saveEvent.Save.FirstNode;
            XElement smsSaveElem = new("StuxnetSMS");

            XElement activeMessages = new("ActiveMessages");
            foreach(var message in SMSSystem.ActiveMessages)
            {
                if (message == null) continue;
                activeMessages.Add(message.GetSaveElement());
            }
            smsSaveElem.Add(activeMessages);

            var queuedMessages = new XElement("QueuedMessages");
            foreach(var queued in SMSSystem.QueuedMessages)
            {
                if (queued == null) continue;
                queuedMessages.Add(queued.GetSaveElement());
            }
            smsSaveElem.Add(queuedMessages);

            var activeChoices = new XElement("ActiveChoices");
            foreach(var choice in SMSSystem.ActiveChoices)
            {
                if (choice == null) continue;
                activeChoices.Add(choice);
            }
            smsSaveElem.Add(activeChoices);

            save.AddAfterSelf(smsSaveElem);
        }
    }

    [SaveExecutor(SMSSaveConstants.SMS_MESSAGES_ELEMENT)]
    public class LoadActiveMessages : SaveLoader.SaveExecutor
    {
        public override void Execute(EventExecutor exec, ElementInfo info)
        {
            foreach(var child in info.Children)
            {
                var xChild = child.ConvertToXElement();
                SMSMessage message = SMSMessage.Deserialize(xChild);
                SMSSystem.ActiveMessages.Add(message);
            }
        }
    }

    [SaveExecutor(SMSSaveConstants.QSMS_MESSAGES_ELEMENT)]
    public class LoadQueuedMessages : SaveLoader.SaveExecutor
    {
        public override void Execute(EventExecutor exec, ElementInfo info)
        {
            foreach(var child in info.Children)
            {
                var xChild = child.ConvertToXElement();
                QueuedSMSMessage queued = QueuedSMSMessage.Deserialize(xChild);
                SMSSystem.QueuedMessages.Add(queued);
            }
        }
    }

    [SaveExecutor(SMSSaveConstants.CHOICES_ELEMENT)]
    public class LoadActiveChoices : SaveLoader.SaveExecutor
    {
        public override void Execute(EventExecutor exec, ElementInfo info)
        {
            foreach(var choice in info.Children)
            {
                var xChoice = choice.ConvertToXElement();
                SMSChoice smsChoice = SMSChoice.Deserialize(xChoice);
                SMSSystem.ActiveChoices.Add(smsChoice);
            }
        }
    }
}
