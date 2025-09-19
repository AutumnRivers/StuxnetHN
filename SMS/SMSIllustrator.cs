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
using Stuxnet_HN.Extensions;

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

            if(Active)
            {
                // Hide other modules while SMS module is active
                var nonRamModules = os.modules.Where(m => m is not RamModule);
                foreach (var module in nonRamModules)
                {
                    if (!Active) break;
                    if (module.visible) module.visible = false;
                }
            }

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

        public bool InputLocked { get; set; } = false;

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

        public override void Update(float t)
        {
            if(InputLocked && name != "Messenger (INPUT LOCKED)")
            {
                name = "Messenger (INPUT LOCKED)";
            } else if(name != "Messenger")
            {
                name = "Messenger";
            }
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
            SMSSystem.Active = true;
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
            SMSSystem.Active = false;
        }

        private int lastMessageOffset = 0;

        private bool MouseInRectangle(Rectangle dest)
        {
            var mousePos = GuiData.getMousePos();

            return
                (mousePos.X >= dest.X &&
                mousePos.X <= dest.X + dest.Width) &&
                (mousePos.Y >= dest.Y &&
                mousePos.Y <= dest.Y + dest.Height);
        }

        public override void Draw(float t)
        {
            if (!visible) return;

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
            if(State != SMSModuleState.ViewMessageHistory && !InputLocked)
            {
                bool exit = Button.doButton(ExitButtonID,
                    bounds.X + 10, bounds.Y + bounds.Height - 35,
                    100, 25, "Exit...", os.brightLockedColor);
                if (exit) Deactivate();
            }

            if(InputLocked && MouseInRectangle(Bounds))
            {
                RenderedRectangle.doRectangle(Bounds.X + 1, Bounds.Y + 1,
                    Bounds.Width - 2, Bounds.Height - 2, Color.Black * 0.1f);
                TextItem.doCenteredFontLabel(Bounds, "-- INPUT LOCKED --",
                    GuiData.smallfont, os.lightGray * 0.4f);
            }
        }

        private readonly int PreviewScrollID = PFButton.GetNextID();
        private float PreviewScrollValue = 0.0f;

        private void DrawDisplay()
        {
            var channels = SMSSystem.ActiveChannels;
            switch(State)
            {
                case SMSModuleState.PreviewMessages:
                default:
                    if(channels.Count == 0)
                    {
                        TextItem.doCenteredFontLabel(MessageListBounds,
                            "No Messages :(", GuiData.font, Color.White);
                        return;
                    }
                    var finalPreviewHeight = ActiveUsers.Count * MeasureChannelEntry();
                    bool needsScroll = finalPreviewHeight > MessageListBounds.Height;
                    if(needsScroll)
                    {
                        var drawbounds = MessageListBounds;
                        drawbounds.Height = finalPreviewHeight;
                        ScrollablePanel.beginPanel(PreviewScrollID, drawbounds, new(0, PreviewScrollValue));
                    }
                    for(int i = 0; i < channels.Count; i++)
                    {
                        DrawChannelEntry(channels[i], needsScroll);
                    }
                    if(needsScroll)
                    {
                        var finalScroll = ScrollablePanel.endPanel(PreviewScrollID, new(0, PreviewScrollValue),
                            MessageListBounds, finalPreviewHeight);
                        PreviewScrollValue = finalScroll.Y;
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

        private const float USER_TITLE_SCALE = 0.7f;

        private int MeasureChannelEntry()
        {
            string channelName = "Test Channel abcdefg";
            string lastMessage = "abcdefgHIJKLMNOP";

            var userHeight = GuiData.font.GetTextHeight(channelName) * USER_TITLE_SCALE;
            var messageHeight = GuiData.smallfont.GetTextHeight(lastMessage);
            return (int)userHeight + messageHeight + 15;
        }

        private void DrawChannelEntry(string channel, bool needsScroll = false)
        {
            int startingOffset = lastMessageOffset;

            var activeChannels = SMSSystem.ActiveChannels;

            SMSMessage lastMessage = SMSSystem.ActiveMessages.Last(msg => msg.ChannelName == channel);
            string lastMessageContent = lastMessage.Content;

            lastMessageContent = lastMessageContent.Truncate(27, splitNewlines: true);
            if(lastMessage.Author != SMSSystemMessage.SYSTEM_AUTHOR)
            {
                lastMessageContent = lastMessage.Author + ": " + lastMessageContent;
            }

            int xOffset = MessageListBounds.X;
            int yOffset = MessageListBounds.Y;

            if(needsScroll)
            {
                xOffset = 0;
                yOffset = 0;
            }

            if(activeChannels.First() == channel)
            {
                Rectangle borderTop = new()
                {
                    X = xOffset,
                    Y = yOffset + lastMessageOffset + 5,
                    Width = MessageListBounds.Width,
                    Height = 1
                };
                spriteBatch.Draw(Utils.white, borderTop, os.lightGray);
            }

            if (!lastMessage.HasBeenRead)
            {
                channel += " (!)";
            }

            var userVector = GuiData.font.MeasureString(channel) * USER_TITLE_SCALE;
            var previewVector = GuiData.smallfont.MeasureString(lastMessageContent);
            lastMessageOffset += (int)(userVector.Y + previewVector.Y + 15);

            Rectangle activeBox = new()
            {
                X = xOffset,
                Y = yOffset + startingOffset,
                Width = MessageListBounds.Width,
                Height = lastMessageOffset - startingOffset
            };
            if (mouseIsHovering() && !InputLocked)
            {
                GuiData.spriteBatch.Draw(Utils.white, activeBox, Color.White * 0.12f);
                if(GuiData.mouseLeftUp())
                {
                    ActiveChannel = lastMessage.ChannelName;
                    State = SMSModuleState.ViewMessageHistory;
                }
            }

            lastMessageContent = ComputerLoader.filter(lastMessageContent);

            spriteBatch.DrawString(GuiData.font, channel,
                new Vector2(MessageListBounds.X + 13, MessageListBounds.Y + 10 + startingOffset), Color.White,
                0f, Vector2.Zero, USER_TITLE_SCALE, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 1f);
            TextItem.doSmallLabel(new Vector2(MessageListBounds.X + 13,
                MessageListBounds.Y + userVector.Y + 5 + startingOffset), lastMessageContent,
                os.lightGray);

            Rectangle borderBottom = new()
            {
                X = xOffset,
                Y = yOffset + lastMessageOffset,
                Width = MessageListBounds.Width,
                Height = 1
            };
            spriteBatch.Draw(Utils.white, borderBottom, os.lightGray);

            bool mouseIsHovering()
            {
                var mousePos = GuiData.getMousePos();

                return
                    (mousePos.X >= activeBox.X &&
                    mousePos.X <= activeBox.X + activeBox.Width) &&
                    (mousePos.Y >= activeBox.Y &&
                    mousePos.Y <= activeBox.Y + activeBox.Height);
            }
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
            if(goBackToMessages && !InputLocked)
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

            Rectangle panelDest = MessageHistoryBounds;
            panelDest.Height = finalHeight + MESSAGE_PADDING;
            if (needsScroll)
            {
                ScrollablePanel.beginPanel(PanelID, panelDest, MessageHistoryScrollPosition);
            }

            lastMessageOffset = 0;
            foreach(var msg in messages)
            {
                DrawMessage(msg, needsScroll);
            }

            if(needsScroll)
            {
                var maxScroll = Math.Max(MessageHistoryBounds.Height, finalHeight - MessageHistoryBounds.Height);
                if(!InputLocked)
                {
                    MessageHistoryScrollPosition = ScrollablePanel.endPanel(PanelID, MessageHistoryScrollPosition,
                        MessageHistoryBounds, maxScroll);
                } else
                {
                    ScrollablePanel.endPanel(PanelID, MessageHistoryScrollPosition, MessageHistoryBounds, maxScroll);
                }
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

            DrawChoicesIfAny();
        }

        private static readonly int[] ChoiceButtonIDs = new int[]
        {
            PFButton.GetNextID(),
            PFButton.GetNextID(),
            PFButton.GetNextID()
        };

        private void DrawChoicesIfAny()
        {
            if (SMSSystem.ActiveChoices.Count <= 0) return;

            string choiceChannel = SMSSystem.ActiveChoices[0].ChannelName;
            if (ActiveChannel != choiceChannel) return;

            for(int choiceIndex = 0; choiceIndex < SMSSystem.ActiveChoices.Count; choiceIndex++)
            {
                DrawChoiceButton(choiceIndex);
            }
        }

        private void DrawChoiceButton(int index)
        {
            int buttonID = ChoiceButtonIDs[index];
            int amountOfChoices = SMSSystem.ActiveChoices.Count;
            var choice = SMSSystem.ActiveChoices[index];

            int width = Bounds.Width / amountOfChoices;
            width -= 4;
            int height = BOTTOM_PANEL_HEIGHT - 4;

            int xPos = (MessageListBounds.X + 2) + (width * index) +
                (index > 0 ? 2 : 0);
            int yPos = MessageHistoryBounds.Y + MessageHistoryBounds.Height + 2;

            bool chosen = Button.doButton(buttonID, xPos, yPos, width, height,
                choice.Content, Color.Transparent);
            if(chosen)
            {
                choice.Chosen();
                SMSSystem.ActiveChoices.Clear();
            }
        }

        private void DrawMessage(SMSMessage message, bool needsScroll)
        {
            if(message is SMSSystemMessage systemMessage)
            {
                DrawSystemMessage(systemMessage, needsScroll);
                return;
            }

            int msgIndex = SMSSystem.ActiveMessages.IndexOf(message);
            SMSMessage prevMessage = null;

            if(msgIndex > 0)
            {
                prevMessage = SMSSystem.ActiveMessages[msgIndex - 1];
            }

            bool isLast = SMSSystem.GetActiveMessagesForChannel(message.ChannelName).Last() == message;
            int messageHeight = (int)MeasureMessage(message).Y;

            int xOffset = MessageHistoryBounds.X;
            int baseYOffset = MessageHistoryBounds.Y;
            if(needsScroll)
            {
                xOffset = 0;
                baseYOffset = 0;
            }

            if(prevMessage is SMSSystemMessage)
            {
                Rectangle topBorder = new()
                {
                    X = xOffset,
                    Y = baseYOffset + lastMessageOffset,
                    Width = MessageHistoryBounds.Width,
                    Height = 1
                };
                GuiData.spriteBatch.Draw(Utils.white, topBorder, os.lightGray);
            }

            Rectangle bottomBorder = new()
            {
                X = xOffset,
                Y = baseYOffset + lastMessageOffset + messageHeight + MESSAGE_PADDING - 1,
                Width = MessageHistoryBounds.Width,
                Height = 1
            };
            if(!isLast)
            {
                GuiData.spriteBatch.Draw(Utils.white, bottomBorder, os.lightGray);
            }

            string content = ComputerLoader.filter(message.Content);
            content = Utils.SuperSmartTwimForWidth(content, MessageHistoryBounds.Width,
                GuiData.smallfont);

            int authorHeight = (int)(GuiData.font.MeasureString(message.Author).Y * 0.65f);
            int contentHeight = (int)GuiData.smallfont.MeasureString(content).Y;

            Color authorColor = Color.White;
            if(SMSSystem.AuthorColors.ContainsKey(message.Author))
            {
                authorColor = SMSSystem.AuthorColors[message.Author];
            }
            if(authorColor == Color.Transparent)
            {
                authorColor = os.defaultHighlightColor;
            }

            GuiData.spriteBatch.DrawString(GuiData.font, message.Author,
                new Vector2(xOffset + MESSAGE_PADDING,
                baseYOffset + lastMessageOffset + (MESSAGE_PADDING * 2)),
                authorColor, 0f, Vector2.Zero, 0.65f,
                Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 1f);
            GuiData.spriteBatch.DrawString(GuiData.smallfont, content,
                new(xOffset + MESSAGE_PADDING,
                baseYOffset + lastMessageOffset + (MESSAGE_PADDING * 2) + authorHeight + 3),
                Color.White);

            message.ReadMessage();

            var baseAttachmentYOffset = baseYOffset + lastMessageOffset + contentHeight +
                (MESSAGE_PADDING * 2) + authorHeight + 3;
            foreach(var attachment in message.Attachments)
            {
                RenderAttachment(attachment,
                    new Vector2(xOffset + (MESSAGE_PADDING * 2), baseAttachmentYOffset));
                baseAttachmentYOffset +=
                    (int)GuiData.smallfont.MeasureString(attachment.DisplayName).Y + MESSAGE_PADDING;
            }

            lastMessageOffset += messageHeight;
        }

        private void DrawSystemMessage(SMSSystemMessage systemMessage, bool needsScroll)
        {
            string content = string.Format("- {0} -", systemMessage.Content);
            Vector2 contentSize = GuiData.smallfont.MeasureString(content);

            int xOffset = MessageHistoryBounds.X;
            int baseYOffset = MessageHistoryBounds.Y;
            if (needsScroll)
            {
                xOffset = 0;
                baseYOffset = 0;
            }

            Rectangle textDest = new()
            {
                X = xOffset,
                Y = baseYOffset + lastMessageOffset,
                Width = MessageHistoryBounds.Width,
                Height = (int)contentSize.Y * 5
            };
            TextItem.doCenteredFontLabel(textDest, content, GuiData.smallfont,
                Utils.SlightlyDarkGray);

            systemMessage.ReadMessage();

            lastMessageOffset += (int)contentSize.Y * 5;
        }

        private void RenderAttachment(SMSAttachment attachment, Vector2 position)
        {
            var mousePosition = GuiData.getMousePos();
            var attachmentTextSize = GuiData.smallfont.MeasureString(attachment.DisplayName);

            int xOffset = (int)position.X;
            GuiData.spriteBatch.DrawString(GuiData.smallfont, "* ",
                new Vector2(xOffset, position.Y), os.lightGray);
            xOffset += (int)GuiData.smallfont.MeasureString("* ").X;

            if (mouseHovering(xOffset) && !InputLocked)
            {
                RenderedRectangle.doRectangle(
                    xOffset, (int)position.Y,
                    (int)attachmentTextSize.X, (int)attachmentTextSize.Y,
                    Color.White * 0.1f);
            }

            GuiData.spriteBatch.DrawString(GuiData.smallfont, attachment.DisplayName,
                new Vector2(xOffset, position.Y), Color.White);

            if(mouseHovering(xOffset) && GuiData.mouseLeftUp())
            {
                attachment.OnButtonClicked();
            }

            bool mouseHovering(int xOffset)
            {
                return
                    (mousePosition.X >= xOffset &&
                    mousePosition.X <= xOffset + attachmentTextSize.X) &&
                    (mousePosition.Y >= position.Y &&
                    mousePosition.Y <= position.Y + attachmentTextSize.Y);
            }
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
            if(message is SMSSystemMessage systemMessage)
            {
                return MeasureSystemMessage(systemMessage);
            }

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

            foreach(var atch in message.Attachments)
            {
                measurement.Y += (int)GuiData.smallfont.MeasureString(atch.DisplayName).Y + MESSAGE_PADDING;
            }

            return measurement;
        }

        private Vector2 MeasureSystemMessage(SMSSystemMessage systemMessage)
        {
            Vector2 measurement = Vector2.Zero;

            measurement.X = Bounds.Width;

            string content = string.Format("- {0} -", systemMessage.Content);
            measurement.Y = GuiData.smallfont.MeasureString(content).Y * 5;

            return measurement;
        }
    }
}
