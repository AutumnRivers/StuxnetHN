using System;
using System.Linq;

using Pathfinder.Daemon;
using Pathfinder.Util;

using Hacknet;
using Hacknet.Gui;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using BepInEx;
using Stuxnet_HN.Localization;

namespace Stuxnet_HN.Daemons
{
    public class VaultDaemon : BaseDaemon
    {
        public VaultDaemon(Computer computer, string serviceName, OS opSystem) : base(computer, serviceName, opSystem) { }

        public override string Identifier => "Vault Daemon";

        private static readonly Random random = new Random();

        [XMLStorage]
        public string Name = "TsukiVault";

        [XMLStorage]
        public string KeyName;

        [XMLStorage]
        public string SecretCode = RandomString(7);

        [XMLStorage]
        public string MaximumKeys = "5";

        [XMLStorage]
        public string Message = "This server protected with TsukiVault technology!";

        public override void initFiles()
        {
            base.initFiles();

            if(int.Parse(MaximumKeys) > 10)
            {
                MaximumKeys = "10";
            } else if(int.Parse(MaximumKeys) < 1)
            {
                MaximumKeys = "1";
            }

            if (KeyName.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException("Vault Daemon Key Name cannot be null or whitespace.\n" +
                    "Comp ID: " + comp.idName);
            }

            if(!StuxnetCore.receivedKeys.ContainsKey(KeyName))
            {
                StuxnetCore.receivedKeys.Add(KeyName, 0);
            }
        }

        public override void draw(Rectangle bounds, SpriteBatch sb)
        {
            // Set the admin password to the secret code
            comp.adminPass = SecretCode;

            int boundaryX = bounds.X;
            int boundaryY = bounds.Y;

            int maxKeys = int.Parse(MaximumKeys);

            // Title of vault
            GuiData.spriteBatch.DrawString(GuiData.font, Name, new Vector2(boundaryX + 30, boundaryY + 30),
                Color.WhiteSmoke, 0.0f, Vector2.Zero, 2.1f, SpriteEffects.None, 0.1f);

            // Fancy divider
            RenderedRectangle.doRectangle(boundaryX, boundaryY + 125, bounds.Width, 30, os.defaultHighlightColor);

            // Get current amount of keys
            if(!StuxnetCore.receivedKeys.ContainsKey(KeyName))
            {
                StuxnetCore.receivedKeys.Add(KeyName, 0);
            }
            int currentKeys = StuxnetCore.receivedKeys[KeyName];

            // Base/interval offsets for the for loop
            int baseOffset = 180;
            int offsetInterval = 60;

            // Save the buttons for later
            bool grantedButton = false;
            bool deniedButton = false;

            // Draw a button for each key
            for (int i = 0; i <= maxKeys + 1; i++)
            {
                if(i < maxKeys && currentKeys > i)
                {
                    Button.doButton(11 * (i + 1), boundaryX + 20, boundaryY + (baseOffset + (offsetInterval * i)), 400, 40, 
                        Localizer.GetLocalized("UNLOCKED"), os.brightUnlockedColor);
                } else if(i < maxKeys)
                {
                    Button.doButton(11 * (i + 1), boundaryX + 20, boundaryY + (baseOffset + (offsetInterval * i)), 400, 40, 
                        Localizer.GetLocalized("LOCKED"), os.brightLockedColor);
                } else if(i == maxKeys && currentKeys >= maxKeys)
                {
                    grantedButton = Button.doButton(211, boundaryX + 20, boundaryY + (baseOffset + (offsetInterval * i)),
                        400, 30, string.Format("{0} :: {1}", Localizer.GetLocalized("ACCESS GRANTED"), SecretCode), Color.Green);
                } else if(i == maxKeys && currentKeys < maxKeys)
                {
                    deniedButton = Button.doButton(211, boundaryX + 20, boundaryY + (baseOffset + (offsetInterval * i)),
                        400, 30, Localizer.GetLocalized("ACCESS DENIED"), os.lockedColor);
                }
            }

            if (grantedButton)
            {
                comp.giveAdmin(os.thisComputer.ip);
                OS.currentInstance.runCommand("ls");
            }; // Give the player admin when they hit the button

            if (deniedButton)
            { // Warn the user they still need all the keys
                os.warningFlash();
                string accessDenied = Localizer.GetLocalized("Access to the vault is denied.");
                string allKeys = Localizer.GetLocalized("All {0} keys are required.");
                os.write(string.Format("\n{0}\n{1}\n", accessDenied, string.Format(allKeys, MaximumKeys)));
            };

            // *notices your code comment* owo
            TextItem.doFontLabel(new Vector2(boundaryX + 20, (bounds.Height + bounds.Y) - 35),
                Message, GuiData.smallfont, os.terminalTextColor);
        }

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"; // Alphanumeric
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

    }
}
