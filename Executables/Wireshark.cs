using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet;
using Hacknet.Gui;
using Hacknet.UIUtils;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Pathfinder.Executable;
using Pathfinder.Util;

using Stuxnet_HN.Patches;

namespace Stuxnet_HN.Executables
{
    public class WiresharkExecutable : GameExecutable, MainDisplayOverrideEXE
    {
        private enum WiresharkState
        {
            Loading,
            ShowEntries,
            ShowEntry,
            Error
        }

        private ScrollableSectionedPanel entriesPanel;

        private List<WiresharkEntry> entries = new List<WiresharkEntry>();

        public bool DisplayOverrideIsActive { get; set; }

        private Vector2 lastScroll = Vector2.Zero;

        private float loadingProgress = 0f;
        private float visibleProgress = 0f;
        private float loadingProgressWhole = 0f;

        private WiresharkState currentState = WiresharkState.Loading;

        public WiresharkEntry CurrentEntry { get; private set; }

        public WiresharkExecutable() : base()
        {
            this.baseRamCost = 350;
            this.ramCost = 350;
            this.IdentifierName = "Wireshark";
            this.name = "Wireshark";
            this.needsProxyAccess = false;
        }

        public override void OnInitialize()
        {
            base.OnInitialize();

            Computer targetComp = ComputerLookup.FindByIp(targetIP);

            if (!targetComp.PlayerHasAdminPermissions())
            {
                this.needsRemoval = true;
                os.terminal.writeLine("[WSHRK] Wireshark requires admin permissions to read from a node's PCAP files!");
            }

            foreach (var exe in os.exes)
            {
                if (exe is WiresharkExecutable)
                {
                    this.needsRemoval = true;
                    os.terminal.writeLine("[WSHRK] Only one instance of Wireshark can be ran at a time!");
                }
            }

            DisplayOverrideIsActive = true;
            entriesPanel = new ScrollableSectionedPanel(415, GuiData.spriteBatch.GraphicsDevice);

            string filename = Args[1];

            Folder currentFolder = Programs.getCurrentFolder(os);

            if(currentFolder.searchForFile(filename) != null)
            {
                FileEntry captureFile = currentFolder.searchForFile(filename);

                if(!captureFile.data.StartsWith("WIRESHARK_NETWORK_CAPTURE(PCAP) :: 1.37.2 -----------"))
                {
                    currentState = WiresharkState.Error;
                    os.terminal.writeLine("[WHSRK] Invalid File");
                    return;
                }

                WiresharkContents captureContents = WiresharkContents.GetContentsFromEncodedFileString(captureFile.data);

                if(captureContents == null)
                {
                    currentState = WiresharkState.Error;
                    os.terminal.writeLine("[WHSRK] Invalid File");
                    return;
                }

                entries = captureContents.entries;
            }
        }

        public override void Update(float t)
        {
            base.Update(t);

            if (loadingProgress < 1.0f)
            {
                loadingProgress += t / 5f;
                loadingProgressWhole += t;

                if (Math.Floor(loadingProgressWhole) % 2 == 0)
                {
                    visibleProgress = loadingProgress;
                }
            } else if (currentState != WiresharkState.ShowEntry)
            {
                currentState = WiresharkState.ShowEntries;
            }
        }

        public override void Draw(float t)
        {
            drawTarget();
            drawOutline();
        }

        public void RenderMainDisplay(Rectangle dest, SpriteBatch sb)
        {
            if (currentState == WiresharkState.Loading)
            {
                PatternDrawer.draw(dest, 1f, Color.Transparent, Color.CornflowerBlue * 0.3f, sb);
                RenderLoadingScreen(dest);
            } else if (currentState == WiresharkState.ShowEntries)
            {
                PatternDrawer.draw(dest, 1f, Color.Transparent, Color.CornflowerBlue * 0.15f, sb);
                RenderEntries(dest);
            } else if (currentState == WiresharkState.ShowEntry)
            {
                PatternDrawer.draw(dest, 1f, Color.Transparent, Color.CornflowerBlue * 0.15f, sb);
                RenderEntryDetails(dest);
            } else
            {
                PatternDrawer.draw(dest, 1f, Color.Transparent, Color.CornflowerBlue * 0.3f, sb);
            }
        }

        private void RenderLoadingScreen(Rectangle bounds)
        {
            int rectWidth = (int)(bounds.Width / 1.5f);

            Vector2 textVector = GuiData.font.MeasureString("Wireshark");
            textVector.Y *= 1.5f;
            Vector2 textPosition = new Vector2(
                bounds.Center.X - (rectWidth / 2),
                (float)((bounds.Y + bounds.Height / 2) - textVector.Y / 2f) - 13f);

            GuiData.spriteBatch.DrawString(GuiData.font, "Wireshark", textPosition, Color.White, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 0.1f);

            RenderedRectangle.doRectangleOutline(bounds.Center.X - (rectWidth / 2), bounds.Center.Y + 10,
                rectWidth, 50, 5, Color.White);
            RenderedRectangle.doRectangle(bounds.Center.X - (rectWidth / 2), bounds.Center.Y + 10,
                (int)(rectWidth * visibleProgress), 50, Color.White);
        }

        private void RenderEntries(Rectangle bounds)
        {
            DrawMainWindowTitle(bounds);

            string pageTitle = "The Wireshark Network Analyzer";
            Vector2 titleVector = GuiData.smallfont.MeasureString(pageTitle);
            Color textColor = Color.White;

            if (entries.Count <= 0)
            {
                DrawCenteredText(bounds, "No Entries Found :(", GuiData.font);
                return;
            }

            entriesPanel.NumberOfPanels = entries.Count + 1;

            Vector2 textVec = GuiData.smallfont.MeasureString("test");

            entriesPanel.PanelHeight = (int)(textVec.Y + 3);

            Vector2 headerPosition = new Vector2(bounds.X + 5, bounds.Y + 23 + (int)titleVector.Y);
            Vector2 entryPosition = new Vector2(bounds.X + 5, headerPosition.Y + textVec.Y + 8);

            Rectangle panelRect = new Rectangle()
            {
                X = bounds.X,
                Y = bounds.Y + ((int)headerPosition.Y),
                Width = bounds.Width,
                Height = bounds.Height - (int)headerPosition.Y
            };

            DrawHeader(bounds, headerPosition, textColor);
            RenderedRectangle.doRectangle(bounds.X, (int)(headerPosition.Y + textVec.Y + 3), bounds.Width, 2,
                Color.White);

            int entryNumber = 0;

            Action<int, Rectangle, SpriteBatch> drawEntries = delegate (int index, Rectangle drawbounds, SpriteBatch sb)
            {
                if(index + 1 < entries.Count)
                {
                    DrawEntryLine(entries[index], drawbounds, entryPosition, textColor, 3718347 + entryNumber);
                    entryPosition.Y += textVec.Y + 3;
                    entryNumber++;
                } else
                {
                    DrawEntryLine(entries[entries.Count - 1], drawbounds, entryPosition, textColor, 3718347 + entryNumber);
                    return;
                }
            };

            Vector2 panelPosition = new Vector2(5, 23 + (int)titleVector.Y);
            entriesPanel.Draw(drawEntries, GuiData.spriteBatch, panelRect);
        }

        private void RenderEntryDetails(Rectangle bounds)
        {
            DrawMainWindowTitle(bounds);

            string pageTitle = "The Wireshark Network Analyzer";
            Vector2 titleVector = GuiData.smallfont.MeasureString(pageTitle);
            Color textColor = Color.White;

            int offset = bounds.Y + (int)titleVector.Y + 18;

            TextItem.doLabel(new Vector2(bounds.X + 10, offset), CurrentEntry.id.ToString(), textColor);

            bool exitButton = Button.doButton(49120473, bounds.X + 10, bounds.Height + bounds.Y - 60,
                150, 50, "Go Back...", Color.Red);

            if(exitButton)
            {
                currentState = WiresharkState.ShowEntries;
            }
        }

        private void DrawMainWindowTitle(Rectangle bounds)
        {
            string pageTitle = "The Wireshark Network Analyzer";
            string version = "v1.37.2";
            Vector2 titleVector = GuiData.smallfont.MeasureString(pageTitle);
            Vector2 versionVector = GuiData.smallfont.MeasureString(version);
            Color borderColor = Color.CornflowerBlue;
            Color textColor = Color.White;

            TextItem.doFontLabel(new Vector2(bounds.X + 10, bounds.Y + 10), pageTitle,
                GuiData.smallfont, textColor);

            TextItem.doFontLabel(new Vector2(bounds.X + bounds.Width - (versionVector.X + 20), bounds.Y + 10),
                version, GuiData.smallfont, textColor);

            RenderedRectangle.doRectangle(bounds.X, bounds.Y + 15 + (int)titleVector.Y,
                bounds.Width, 3, borderColor);
        }

        private void DrawHeader(Rectangle bounds, Vector2 position, Color textColor)
        {
            Vector2 entryPosition = position;
            float[] xPositions = new float[7];

            xPositions[0] = entryPosition.X;
            xPositions[1] = xPositions[0] + (bounds.Width * 0.175f); // ID
            xPositions[2] = xPositions[1] + (bounds.Width * 0.07f); // ip From
            xPositions[3] = xPositions[2] + (bounds.Width * 0.2f); // ip To
            xPositions[4] = xPositions[3] + (bounds.Width * 0.2f); // protocol
            xPositions[5] = xPositions[4] + (bounds.Width * 0.1f);  // length
            xPositions[6] = xPositions[5] + (bounds.Width * 0.1f);  // method

            entryPosition.X = xPositions[1];
            GuiData.spriteBatch.DrawString(GuiData.smallfont, "ID", entryPosition, textColor);

            entryPosition.X = xPositions[2];
            GuiData.spriteBatch.DrawString(GuiData.smallfont, "SOURCE", entryPosition, textColor);

            entryPosition.X = xPositions[3];
            GuiData.spriteBatch.DrawString(GuiData.smallfont, "DEST.", entryPosition, textColor);

            entryPosition.X = xPositions[4];
            GuiData.spriteBatch.DrawString(GuiData.smallfont, "PRTCL.", entryPosition, textColor);

            entryPosition.X = xPositions[5];
            GuiData.spriteBatch.DrawString(GuiData.smallfont, "SIZE", entryPosition, textColor);

            entryPosition.X = xPositions[6];
            GuiData.spriteBatch.DrawString(GuiData.smallfont, "METHOD", entryPosition, textColor);
        }

        private void DrawEntryLine(WiresharkEntry entry, Rectangle bounds, Vector2 position, Color textColor, int buttonID)
        {
            Vector2 entryPosition = position;
            float[] xPositions = new float[7];
            Vector2 fontVector = GuiData.smallfont.MeasureString("test");

            xPositions[0] = entryPosition.X;
            xPositions[1] = xPositions[0] + (bounds.Width * 0.175f); // ID
            xPositions[2] = xPositions[1] + (bounds.Width * 0.07f); // ip From
            xPositions[3] = xPositions[2] + (bounds.Width * 0.2f); // ip To
            xPositions[4] = xPositions[3] + (bounds.Width * 0.2f); // protocol
            xPositions[5] = xPositions[4] + (bounds.Width * 0.1f);  // length
            xPositions[6] = xPositions[5] + (bounds.Width * 0.1f);  // method

            entryPosition.X = xPositions[0];
            bool viewButton = Button.doButton(buttonID, (int)entryPosition.X, (int)entryPosition.Y, (int)(bounds.Width * 0.15f),
                (int)fontVector.Y, "View...", Color.CornflowerBlue);

            if(viewButton)
            {
                CurrentEntry = entry;
                currentState = WiresharkState.ShowEntry;
            }

            entryPosition.X = xPositions[1];
            GuiData.spriteBatch.DrawString(GuiData.smallfont, entry.id.ToString(), entryPosition, textColor);

            entryPosition.X = xPositions[2];
            GuiData.spriteBatch.DrawString(GuiData.smallfont, entry.ipFrom, entryPosition, textColor);

            entryPosition.X = xPositions[3];
            GuiData.spriteBatch.DrawString(GuiData.smallfont, entry.ipTo, entryPosition, textColor);

            entryPosition.X = xPositions[4];
            GuiData.spriteBatch.DrawString(GuiData.smallfont, entry.protocol, entryPosition, textColor);

            entryPosition.X = xPositions[5];
            GuiData.spriteBatch.DrawString(GuiData.smallfont, entry.length.ToString(), entryPosition, textColor);

            entryPosition.X = xPositions[6];
            GuiData.spriteBatch.DrawString(GuiData.smallfont, entry.method, entryPosition, textColor);
        }

        private void DrawCenteredText(Rectangle bounds, string text, SpriteFont font)
        {
            Vector2 textVector = font.MeasureString(text);
            Vector2 textPosition = new Vector2(
                (float)(bounds.X + bounds.Width / 2) - textVector.X / 2f,
                (float)(bounds.Y + bounds.Height / 2) - textVector.Y / 2f);

            GuiData.spriteBatch.DrawString(font, text, textPosition, Color.White);
        }
    }

    public class WiresharkEntry
    {
        public uint id;
        public string ipFrom;
        public string ipTo;
        public string method;
        public string protocol;
        public int length;
        public bool secure;

        public string originIP = "#PLAYER_IP#";

        public string Content { get; private set; }

        public WiresharkEntry(uint id, string ipTo, bool isSecure = false, string method = "GET", string protocol = "TCP")
        {
            this.id = id;
            this.ipFrom = "127.0.0.1";
            this.ipTo = ipTo;
            this.method = method;
            this.protocol = protocol;
            this.length = 0;
            this.secure = isSecure;

            Content = "-- Empty Packet Data --";
        }

        public WiresharkEntry(uint id, string ipFrom, string ipTo,
            bool isSecure = false, string method = "GET", string protocol = "TCP")
        {
            this.id = id;
            this.ipFrom = ipFrom;
            this.ipTo = ipTo;
            this.method = method;
            this.protocol = protocol;
            this.length = 0;
            this.secure = isSecure;

            Content = "-- Empty Packet Data --";
        }

        public WiresharkEntry(uint id, string ipFrom, string ipTo, string content,
            bool isSecure = false, string method = "GET", string protocol = "TCP")
        {
            this.id = id;
            this.ipFrom = ipFrom;
            this.ipTo = ipTo;
            this.method = method;
            this.protocol = protocol;
            this.secure = isSecure;

            Content = content;

            this.length = content.Length;
        }
    }
}
