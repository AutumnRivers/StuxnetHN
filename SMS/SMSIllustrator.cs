using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hacknet;
using Hacknet.Gui;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Pathfinder.GUI;

namespace Stuxnet_HN.SMS
{
    [HarmonyPatch]
    public class SMSIllustrator
    {
        public static bool Active { get; set; } = false;

        public static Module[] ModuleCache { get; set; } = new Module[4];

        [HarmonyPrefix]
        [HarmonyPatch(typeof(OS),"drawModules")]
        public static void DrawSMS()
        {
            OS os = OS.currentInstance;

            if (Active && ModuleCache[0] == null)
            {
                for (int i = 0; i < 4; i++)
                {
                    var module = os.modules[i];
                    ModuleCache[i] = module;

                    if (module.name.ToLowerInvariant() != "ram")
                    {
                        module.visible = false;
                        os.modules[i] = module;
                    }
                    else
                    {
                        var ram = (RamModule)module;
                        ram.inputLocked = true;
                        ram.guiInputLockStatus = true;
                        os.modules[i] = ram;
                    }
                }
            }
            else if (!Active && ModuleCache[0] != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    os.modules[i] = ModuleCache[i];
                    ModuleCache[i] = null;
                }
            }
        }
    }

    public class SMSModule : Module
    {
        private static bool[] ModuleVisibleCache { get; set; } = new bool[4];
        private static bool _hasCached = false;

        public static SMSModule GlobalInstance;

        public int ExitButtonID;

        private static Rectangle MessageListBounds;

        public SMSModule(Rectangle bounds, OS os) : base(bounds, os)
        {
            this.bounds = bounds;
            this.os = os;
            spriteBatch = GuiData.spriteBatch;
            name = "Messenger";
            visible = false;
            ExitButtonID = PFButton.GetNextID();
            GlobalInstance = this;

            ModuleVisibleCache = new bool[]
            {
                true, true, true, true
            };

            Vector2 titleSize = GuiData.font.MeasureString("SMS Messenger");
            MessageListBounds = new()
            {
                X = bounds.X,
                Y = bounds.Y + (int)titleSize.Y + 28,
                Width = bounds.Width,
                Height = bounds.Height - ((int)titleSize.Y + 28)
            };
        }

        public static void Activate()
        {
            OS os = OS.currentInstance;

            if(!_hasCached)
            {
                for (int i = 0; i < 4; i++)
                {
                    var module = os.modules[i];
                    ModuleVisibleCache[i] = module.visible;

                    if (module is not RamModule)
                    {
                        module.visible = false;
                        os.modules[i] = module;
                    }
                    else
                    {
                        var ram = (RamModule)module;
                        ram.inputLocked = true;
                        ram.guiInputLockStatus = true;
                        os.modules[i] = ram;
                    }
                }

                _hasCached = true;
            }

            GlobalInstance.visible = true;
        }

        public static void Deactivate()
        {
            OS os = OS.currentInstance;

            if (_hasCached)
            {
                for (int i = 0; i < 4; i++)
                {
                    os.modules[i].visible = ModuleVisibleCache[i];
                }

                os.ram.inputLocked = false;
                os.ram.guiInputLockStatus = false;
                _hasCached = false;
            }

            GlobalInstance.visible = false;
        }

        private int lastMessageOffset = 0;

        public override void Draw(float t)
        {
            base.Draw(t);
            spriteBatch.Draw(Utils.white, bounds, os.displayModuleExtraLayerBackingColor);
            TextItem.doLabel(new Vector2(bounds.X + 10, bounds.Y + 10),
                "SMS Messenger", Color.White);
            Vector2 titleSize = GuiData.font.MeasureString("SMS Messenger");
            Rectangle gradientBox = new()
            {
                X = bounds.X,
                Y = (int)(bounds.Y + titleSize.Y + 15),
                Width = (int)(bounds.Width * 0.85f),
                Height = 3
            };
            spriteBatch.Draw(Utils.gradientLeftRight, gradientBox, null, os.highlightColor,
                0f, Vector2.Zero, Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipHorizontally, 1f);

            bool exit = Button.doButton(ExitButtonID,
                bounds.X + 10, bounds.Y + bounds.Height - 35,
                100, 25, "Exit...", os.brightLockedColor);
            if (exit) Deactivate();

            lastMessageOffset = 0;
            DrawUserEntry(-5);
        }

        private List<string> GetUsers()
        {
            List<string> users = new();
            foreach(var msg in SMSSystem.ActiveMessages)
            {
                if(!users.Contains(msg.Author))
                {
                    users.Add(msg.Author);
                }
            }
            return users;
        }

        private const float USER_TITLE_SCALE = 0.7f;

        private void DrawUserEntry(int index)
        {
            var activeUsers = GetUsers();
            //var user = activeUsers[index];

            Rectangle borderTop = new()
            {
                X = MessageListBounds.X,
                Y = MessageListBounds.Y + lastMessageOffset,
                Width = MessageListBounds.Width,
                Height = 1
            };
            spriteBatch.Draw(Utils.white, borderTop, os.lightGray);
            spriteBatch.DrawString(GuiData.font, "Example User",
                new Vector2(MessageListBounds.X + 13, MessageListBounds.Y + 10 + lastMessageOffset), Color.White,
                0f, Vector2.Zero, USER_TITLE_SCALE, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 1f);
            var userVector = GuiData.font.MeasureString("Example User") * USER_TITLE_SCALE;
            TextItem.doSmallLabel(new Vector2(MessageListBounds.X + 13,
                MessageListBounds.Y + userVector.Y + 5 + lastMessageOffset), "Lorem ipsum or whatever",
                os.lightGray);
            var previewVector = GuiData.smallfont.MeasureString("Lorem ipsum or whatever");
            lastMessageOffset += (int)(MessageListBounds.Y + userVector.Y + previewVector.Y + 15);

            if(activeUsers.Count - 1 >= index)
            {
                Rectangle borderBottom = new()
                {
                    X = MessageListBounds.X,
                    Y = lastMessageOffset,
                    Width = MessageListBounds.Width,
                    Height = 1
                };
                spriteBatch.Draw(Utils.white, borderBottom, os.lightGray);
            }
        }
    }
}
