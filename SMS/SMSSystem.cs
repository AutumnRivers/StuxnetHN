using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stuxnet_HN.SMS
{
    public static class SMSSystem
    {
        public static List<SMSMessage> ActiveMessages = new();
        public static List<QueuedSMSMessage> QueuedMessages = new();
        public static Dictionary<string, Color> AuthorColors = new()
        {
            { "player", Color.Transparent } // Transparent = Theme Highlight Color
        };
    }
}
