using System;
using System.IO;
using System.Collections.Generic;

using Hacknet;
using Hacknet.Gui;
using Hacknet.Extensions;

using Pathfinder.Daemon;
using Pathfinder.Util;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using BepInEx;

using Stuxnet_HN.Localization;

using Stuxnet_HN.Configuration;

namespace Stuxnet_HN.Daemons
{
    public class CodeRedemptionDaemon : BaseDaemon
    {
        public event Action<string> CodeRedeemed;

        public CodeRedemptionDaemon(Computer computer, string serviceName, OS os) : base(computer, serviceName, os) { }

        public override string Identifier => "Code Redemption";

        [XMLStorage]
        public string CodePrefix;

        [XMLStorage]
        public string Message = "Enter your code here...";

        private bool getPlayerInput = false;
        private string userInput = "";
        private string cachedUserInput = "";

        string[] separator = new string[1];
        string[] array = new string[]{};

        int outlineThickness = 2;

        string splashText = "Hacknet? More like... LAMEnet... got em...";

        string resultText = "";
        Color resultColor = Color.White;

        Texture2D extensionLogo;

        public override void initFiles()
        {
            base.initFiles();

            extensionLogo = ExtensionLoader.ActiveExtensionInfo.LogoImage;

            Random random = new Random();

            splashText = StuxnetCore.postMsg[random.Next(0, StuxnetCore.postMsg.Length)];
        }

        public override void draw(Rectangle bounds, SpriteBatch sb)
        {
            base.draw(bounds, sb);

            string displayUserInput = CodePrefix + userInput.Truncate(15);
            string extName = ExtensionLoader.ActiveExtensionInfo.Name;

            // Title
            Vector2 titleVector = GuiData.font.MeasureString(LocaleTerms.Loc($"{extName} Code Redemption"));
            Vector2 titlePosition = new Vector2((float)(bounds.X + bounds.Width / 2) - titleVector.X / 2f, bounds.Y + 80);
            GuiData.spriteBatch.DrawString(GuiData.font, LocaleTerms.Loc($"{extName} Code Redemption"), titlePosition, Color.OrangeRed);

            // Server Message
            Vector2 msgVector = GuiData.smallfont.MeasureString(LocaleTerms.Loc(Message));
            Vector2 msgPosition = new Vector2((float)(bounds.X + bounds.Width / 2) - msgVector.X / 2f, bounds.Y + 130);
            GuiData.spriteBatch.DrawString(GuiData.smallfont, LocaleTerms.Loc(Message), msgPosition, Color.White);

            // Draw Logo
            Rectangle logoRect = new Rectangle()
            {
                X = bounds.X + (bounds.Width / 4),
                Y = bounds.Y + 175,
                Width = bounds.Width / 2,
                Height = bounds.Width / 2
            };

            GuiData.spriteBatch.Draw(extensionLogo, logoRect, Color.White * 0.3f);

            separator = new string[1] { "#$#$#$$#$&$#$#$#$#" };
            array = os.getStringCache.Split(separator, StringSplitOptions.None);

            Rectangle textBoxRect = new Rectangle()
            {
                Width = bounds.Width - 200,
                Height = 55,
                X = bounds.X + 100,
                Y = bounds.Center.Y - 30
            };

            RenderedRectangle.doRectangle(textBoxRect.X, textBoxRect.Y, textBoxRect.Width, textBoxRect.Height, Color.Black);
            RenderedRectangle.doRectangleOutline(textBoxRect.X, textBoxRect.Y, textBoxRect.Width, textBoxRect.Height,
                outlineThickness, Color.Gray);

            TextItem.doFontLabel(new Vector2(textBoxRect.X + 8, textBoxRect.Y + 7), displayUserInput, GuiData.font, Color.White);

            bool enterCodeButton = Button.doButton(191919,
                bounds.Center.X - 100,
                textBoxRect.Y + textBoxRect.Height + 35,
                200, 35, "Enter Code", Color.Orange);

            bool submitCodeButton = Button.doButton(191920,
                bounds.Center.X - 100,
                textBoxRect.Y + textBoxRect.Height + 80,
                200, 25, "Submit Code", Color.Green);

            // Splash Text
            Vector2 splshVector = GuiData.font.MeasureString(LocaleTerms.Loc(splashText));
            Vector2 splshPosition = new((float)(bounds.X + bounds.Width / 2) - splshVector.X / 2f, textBoxRect.Y + textBoxRect.Height + 130);
            GuiData.spriteBatch.DrawString(GuiData.font, LocaleTerms.Loc(splashText), splshPosition, Color.WhiteSmoke);

            // Result Text
            Vector2 resVector = GuiData.smallfont.MeasureString(LocaleTerms.Loc(resultText));
            Vector2 resPosition = new((float)(bounds.X + bounds.Width / 2) - resVector.X / 2f, textBoxRect.Y + textBoxRect.Height + 170);
            GuiData.spriteBatch.DrawString(GuiData.smallfont, LocaleTerms.Loc(resultText), resPosition, resultColor);

            // Logic
            if (enterCodeButton) { getPlayerInput = true; }

            if (submitCodeButton) {
                if(getPlayerInput) {
                    ShowNewSplash();
                    outlineThickness = 2;
                    cachedUserInput = userInput;
                    userInput = "";
                }

                if(CheckCode(cachedUserInput))
                {
                    resultText = Localizer.GetLocalized("Code Successful!");
                    resultColor = Color.Green;
                } else
                {
                    resultText = Localizer.GetLocalized("Code Failed...");
                    resultColor = Color.Red;
                }

                getPlayerInput = false;
                os.terminal.executeLine();
            }

            if(getPlayerInput)
            {
                if (!os.terminal.prompt.StartsWith("Code")) {
                    os.execute("getString Code");
                    outlineThickness = 4;
                }

                if (array.Length > 1)
                {
                    userInput = array[1];
                    if (userInput.Equals(""))
                    {
                        userInput = os.terminal.currentLine;
                    }
                }
            }
        }

        private bool CheckCode(string code)
        {
            Console.WriteLine(StuxnetCore.logPrefix + $"Checking against code {code}...");

            if (StuxnetCore.redeemedCodes.Contains(code)) { return false; }
            Dictionary<string, CodeEntry> codes = StuxnetConfig.GlobalConfig.Codes;

            if(!codes.ContainsKey(code)) { return false; }

            Console.WriteLine(StuxnetCore.logPrefix + $"codes.json has code {code}");

            CodeEntry validCode = codes[code];

            StuxnetCore.redeemedCodes.Add(code);
            CodeRedeemed.Invoke(code);

            // Add files
            if(validCode.files != null)
            {
                Folder userBin = Programs.getFolderAtPath("bin", os, os.thisComputer.files.root);
                Folder userHome = Programs.getFolderAtPath("home", os, os.thisComputer.files.root);

                foreach(var file in validCode.files)
                {
                    string filename = file.Key;
                    string filedata = file.Value;

                    FileEntry fileToAdd = new FileEntry(ComputerLoader.filter(filedata), filename);

                    if(filename.EndsWith(".exe"))
                    {
                        userBin.files.Add(fileToAdd);
                    } else
                    {
                        userHome.files.Add(fileToAdd);
                    }
                }
            }

            // Add radio codes
            if(validCode.radio != null)
            {
                foreach(string songID in validCode.radio)
                {
                    StuxnetCore.unlockedRadio.Add(songID);
                }
            }

            // Add themes
            if(validCode.themes != null)
            {
                Folder userSys = Programs.getFolderAtPath("sys", os, os.thisComputer.files.root);

                foreach(var theme in validCode.themes)
                {
                    string name = theme.Key + "-x-server.sys";
                    string filepath = theme.Value;

                    FileEntry customThemeFile = CreateCustomThemeFile(name, filepath);

                    userSys.files.Add(customThemeFile);
                }
            }
            
            // Add PF themes (Shaders, animated, etc.)
            // TODO

            // Send associated email (if any)
            if(!validCode.email.IsNullOrWhiteSpace())
            {
                string emailFile = ExtensionLoader.ActiveExtensionInfo.FolderPath + "/" + validCode.email;

                if(!File.Exists(emailFile)) { return true; }

                ActiveMission emailToSend = (ActiveMission)ComputerLoader.readMission(emailFile);

                emailToSend.sendEmail(os);
            }

            // Load conditional actions (if any)
            if(!validCode.action.IsNullOrWhiteSpace())
            {
                RunnableConditionalActions.LoadIntoOS("/" + validCode.action, os);
            }

            return true;
        }

        private FileEntry CreateCustomThemeFile(string name, string filepath)
        {
            string data = ThemeManager.getThemeDataStringForCustomTheme("/" + filepath);

            if(data.IsNullOrWhiteSpace()) { return new FileEntry("!!! ERROR LOADING THEME !!!", name + "-error"); }

            data = ComputerLoader.filter(data);

            return new FileEntry(data, name);
        }

        private void ShowNewSplash()
        {
            Random random = new Random();

            splashText = StuxnetCore.postMsg[random.Next(0, StuxnetCore.postMsg.Length)];
        }
    }
}
