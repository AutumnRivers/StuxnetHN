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
        public const int BOTTOM_PANEL_HEIGHT = 85;

        private static bool[] ModuleVisibleCache { get; set; } = new bool[4];
        private static bool _hasCached = false;

        public static SMSModule GlobalInstance;

        public int ExitButtonID;

        private static Rectangle MessageListBounds;
        private static Rectangle MessageHistoryBounds;

        public enum SMSModuleState
        {
            PreviewMessages,
            ViewMessageHistory
        }
        public SMSModuleState State = SMSModuleState.PreviewMessages;
        public string ActiveChannel = null;

        public SMSModule(Rectangle bounds, OS os) : base(bounds, os)
        {
            this.bounds = bounds;
            this.os = os;
            spriteBatch = GuiData.spriteBatch;
            name = "Messenger";
            visible = false;
            ExitButtonID = PFButton.GetNextID();
            GlobalInstance = this;

            SMSSystem.NewMessageReceived += delegate(string channel) {
                if(channel == ActiveChannel)
                {
                    MessageHistoryScrollPosition.X = -1; // Jumps to latest message
                }
            };

            ModuleVisibleCache = new bool[]
            {
                true, true, true, true
            };

            Vector2 titleSize = GuiData.font.MeasureString("SMS Messenger");
            MessageListBounds = new()
            {
                X = bounds.X + 1,
                Y = bounds.Y + (int)titleSize.Y + 28,
                Width = bounds.Width - 2,
                Height = bounds.Height - ((int)titleSize.Y + 28)
            };
            MessageHistoryBounds = MessageListBounds;
            MessageHistoryBounds.Height -= BOTTOM_PANEL_HEIGHT;
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

            lastMessageOffset = 0;

            DrawDisplay();
            if(State != SMSModuleState.ViewMessageHistory)
            {
                bool exit = Button.doButton(ExitButtonID,
                    bounds.X + 10, bounds.Y + bounds.Height - 35,
                    100, 25, "Exit...", os.brightLockedColor);
                if (exit) Deactivate();
            }
        }

        private void DrawDisplay()
        {
            var channels = SMSSystem.ActiveChannels;
            switch(State)
            {
                case SMSModuleState.PreviewMessages:
                default:
                    for(int i = 0; i < channels.Count; i++)
                    {
                        DrawChannelEntry(channels[i], i);
                    }
                    break;
                case SMSModuleState.ViewMessageHistory:
                    DrawMessageView();
                    break;
            }
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

        public List<string> ActiveUsers
        {
            get { return GetUsers(); }
        }
        public List<int> ChannelButtonIDs = new();

        private const float USER_TITLE_SCALE = 0.7f;

        private void DrawChannelEntry(string channel, int index)
        {
            var activeChannels = SMSSystem.ActiveChannels;
            if(ChannelButtonIDs.Count <= index)
            {
                ChannelButtonIDs.Add(PFButton.GetNextID());
            }

            SMSMessage lastMessage = SMSSystem.ActiveMessages.Last(msg => msg.ChannelName == channel);
            string lastMessageContent = lastMessage.Content;

            lastMessageContent = lastMessageContent.Truncate(27);
            lastMessageContent = lastMessage.Author + ": " + lastMessageContent;

            if (!lastMessage.HasBeenRead)
            {
                channel += " (!)";
            }

            bool viewChannel = Button.doButton(ChannelButtonIDs[index],
                MessageListBounds.X + MessageListBounds.Width - 50,
                MessageListBounds.Y + lastMessageOffset + 10,
                40, 40, "->", Color.Transparent);
            if (viewChannel)
            {
                ActiveChannel = lastMessage.ChannelName;
                State = SMSModuleState.ViewMessageHistory;
            }

            if(activeChannels.First() == channel)
            {
                Rectangle borderTop = new()
                {
                    X = MessageListBounds.X,
                    Y = MessageListBounds.Y + lastMessageOffset + 5,
                    Width = MessageListBounds.Width,
                    Height = 1
                };
                spriteBatch.Draw(Utils.white, borderTop, os.lightGray);
            }

            spriteBatch.DrawString(GuiData.font, channel,
                new Vector2(MessageListBounds.X + 13, MessageListBounds.Y + 10 + lastMessageOffset), Color.White,
                0f, Vector2.Zero, USER_TITLE_SCALE, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 1f);
            var userVector = GuiData.font.MeasureString(channel) * USER_TITLE_SCALE;
            TextItem.doSmallLabel(new Vector2(MessageListBounds.X + 13,
                MessageListBounds.Y + userVector.Y + 5 + lastMessageOffset), lastMessageContent,
                os.lightGray);
            var previewVector = GuiData.smallfont.MeasureString(lastMessageContent);
            lastMessageOffset += (int)(userVector.Y + previewVector.Y + 15);

            Rectangle borderBottom = new()
            {
                X = MessageListBounds.X,
                Y = MessageListBounds.Y + lastMessageOffset,
                Width = MessageListBounds.Width,
                Height = 1
            };
            spriteBatch.Draw(Utils.white, borderBottom, os.lightGray);
        }

        private Vector2 MessageHistoryScrollPosition = new(-1,-1);
        private const int MESSAGE_PADDING = 5;
        private readonly int PanelID = PFButton.GetNextID();
        private readonly int MessagesReturnID = PFButton.GetNextID();

        private void DrawMessageView()
        {
            var channel = ActiveChannel;
            var messages = SMSSystem.GetActiveMessagesForChannel(channel);

            Rectangle borderTop = new()
            {
                X = MessageHistoryBounds.X + 1,
                Y = MessageHistoryBounds.Y - 1,
                Width = MessageHistoryBounds.Width - 2,
                Height = 1
            };
            spriteBatch.Draw(Utils.white, borderTop, os.lightGray);

            int finalHeight = MeasureMessagesHeight(messages);

            bool goBackToMessages = Button.doButton(MessagesReturnID,
                Bounds.X + Bounds.Width - 110, Bounds.Y + 10,
                100, 35, "Messages", os.defaultHighlightColor);
            if(goBackToMessages)
            {
                State = SMSModuleState.PreviewMessages;
                ActiveChannel = null;
                MessageHistoryScrollPosition.X = -1;
            }

            PatternDrawer.draw(MessageHistoryBounds, 0.8f,
                os.moduleColorBacking, os.highlightColor * 0.2f,
                spriteBatch, PatternDrawer.thinStripe);

            if(MessageHistoryScrollPosition.X < 0)
            {
                MessageHistoryScrollPosition = new Vector2(0,
                    finalHeight - MessageHistoryBounds.Height);
            }

            bool needsScroll = finalHeight >= MessageHistoryBounds.Height;

            Rectangle panelDest = Bounds;
            panelDest.Height = finalHeight + 50;
            if (needsScroll)
            {
                ScrollablePanel.beginPanel(PanelID, panelDest, MessageHistoryScrollPosition);
            }

            lastMessageOffset = 0;
            foreach(var msg in messages)
            {
                DrawMessage(msg);
            }

            if(needsScroll)
            {
                var maxScroll = Math.Max(MessageHistoryBounds.Height, finalHeight - MessageHistoryBounds.Height);
                ScrollablePanel.endPanel(PanelID, MessageHistoryScrollPosition, panelDest, maxScroll);
            }

            DrawBottomPanel();
        }

        private void DrawBottomPanel()
        {
            Rectangle topBorder = new()
            {
                X = Bounds.X,
                Y = Bounds.Y + Bounds.Height - BOTTOM_PANEL_HEIGHT,
                Width = Bounds.Width,
                Height = 1
            };
            spriteBatch.Draw(Utils.white, topBorder, os.lightGray);

            RenderedRectangle.doRectangle(
                Bounds.X, Bounds.Y + Bounds.Height - BOTTOM_PANEL_HEIGHT + 1,
                Bounds.Width, BOTTOM_PANEL_HEIGHT - 1,
                Color.Lerp(os.moduleColorBacking, Color.Black, 0.1f));
        }

        private void DrawMessage(SMSMessage message)
        {
            bool isLast = SMSSystem.GetActiveMessagesForChannel(message.ChannelName).Last() == message;
            int messageHeight = (int)MeasureMessage(message).Y;

            Rectangle bottomBorder = new()
            {
                X = MessageHistoryBounds.X,
                Y = MessageHistoryBounds.Y + lastMessageOffset + messageHeight + MESSAGE_PADDING - 1,
                Width = MessageHistoryBounds.Width,
                Height = 1
            };
            if(!isLast)
            {
                spriteBatch.Draw(Utils.white, bottomBorder, os.lightGray);
            }

            string content = ComputerLoader.filter(message.Content);
            content = Utils.SuperSmartTwimForWidth(content, MessageHistoryBounds.Width,
                GuiData.smallfont);

            int authorHeight = (int)(GuiData.font.MeasureString(message.Author).Y * 0.65f);

            Color authorColor = Color.White;
            if(SMSSystem.AuthorColors.ContainsKey(message.Author))
            {
                authorColor = SMSSystem.AuthorColors[message.Author];
            }

            spriteBatch.DrawString(GuiData.font, message.Author,
                new Vector2(MessageHistoryBounds.X + MESSAGE_PADDING,
                MessageHistoryBounds.Y + lastMessageOffset + (MESSAGE_PADDING * 2)),
                authorColor, 0f, Vector2.Zero, 0.65f,
                Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 1f);
            spriteBatch.DrawString(GuiData.smallfont, content,
                new(MessageHistoryBounds.X + MESSAGE_PADDING,
                MessageHistoryBounds.Y + lastMessageOffset + (MESSAGE_PADDING * 2) + authorHeight + 3),
                Color.White);
            /*TextItem.doSmallLabel(new Vector2(MessageHistoryBounds.X + MESSAGE_PADDING,
                MessageHistoryBounds.Y + lastMessageOffset + (MESSAGE_PADDING * 2) + authorHeight + 3),
                content, Color.White);*/
            message.ReadMessage();

            lastMessageOffset += messageHeight;
        }

        private int MeasureMessagesHeight(List<SMSMessage> messages)
        {
            int height = 0;
            foreach(var msg in messages)
            {
                height += (int)MeasureMessage(msg).Y;
            }
            return height;
        }

        private Vector2 MeasureMessage(SMSMessage message)
        {
            Vector2 measurement = Vector2.Zero;

            measurement.X = Bounds.Width;
            measurement.Y += MESSAGE_PADDING * 2;

            var authorMeasurement = GuiData.font.MeasureString(message.Author);
            measurement.Y += authorMeasurement.Y * 0.65f;

            measurement.Y += 3;

            string content = ComputerLoader.filter(message.Content);
            content = Utils.SuperSmartTwimForWidth(content, MessageHistoryBounds.Width,
                GuiData.smallfont);

            var messageMeasurement = GuiData.smallfont.MeasureString(content);
            measurement.Y += messageMeasurement.Y;

            return measurement;
        }
    }
}
