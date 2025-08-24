using System.Linq;
using Hacknet;
using Stuxnet_HN.SMS;
using Pathfinder.Meta.Load;
using Stuxnet_HN.Localization;

namespace Stuxnet_HN.Commands
{
    public class SMSCommands
    {
        public static void ActivateSMS(OS os, string[] args)
        {
            if(ThemeManager.currentTheme is OSTheme.TerminalOnlyBlack)
            {
                os.beepSound.Play();
                os.terminal.writeLine("--------------------");
                os.terminal.writeLine("--------------------");
                os.terminal.writeLine(":: ERROR ::");
                os.terminal.writeLine(":: UNABLE TO ACTIVATE MESSENGER ::");
                os.terminal.writeLine("--------------------");
                os.terminal.writeLine("--------------------");
                return;
            }

            SMSModule.Activate();
        }

        public static void CheckUnread(OS os, string[] args)
        {
            string notification = Localizer.GetLocalized("You're all caught up!") + " :)";
            int unreadCount = SMSSystem.ActiveMessages.Count(msg => !msg.HasBeenRead);
            if(unreadCount > 0)
            {
                notification = string.Format(Localizer.GetLocalized("You have {0} unread messages."), unreadCount);
            }
            os.terminal.writeLine(notification);
        }
    }
}
