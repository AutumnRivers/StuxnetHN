using BepInEx;

using Hacknet;
using Hacknet.PlatformAPI.Storage;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathfinder.Daemon;
using Pathfinder.Util;

namespace Stuxnet_HN.Daemons
{
    public class DebugDaemon : BaseDaemon
    {
        public DebugDaemon(Computer computer, string serviceName, OS os) : base(computer, serviceName, os) { }

        [XMLStorage]
        public string RequiredPassword;

        public override void navigatedTo()
        {
            base.navigatedTo();

            bool isDebug = OS.DEBUG_COMMANDS;

            string playerPassword = SaveFileManager.LastLoggedInUser.Password;

            if (!isDebug ||
                (!RequiredPassword.IsNullOrWhiteSpace() && RequiredPassword != playerPassword))
            {
                Programs.disconnect(new string[0], os);
                os.terminal.writeLine("Access Denied");
            }
        }

        public override void draw(Rectangle bounds, SpriteBatch sb)
        {
            base.draw(bounds, sb);

            os.display.command = "connect";
        }
    }
}
