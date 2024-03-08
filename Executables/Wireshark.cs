using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet;
using Hacknet.Gui;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Pathfinder.Executable;
using Pathfinder.Util;
using Pathfinder.GUI;

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

        private readonly List<WiresharkEntry> entries = new List<WiresharkEntry>()
        {
            new WiresharkEntry(1, "1.2.3.4"),
            new WiresharkEntry(323, "127.0.0.1", "5.6.7.8", "Test Content", "GET", "TLSv1.2"),
            new WiresharkEntry(42, "127.0.0.1", "1.2.3.4", "More Test Content", "POST", "TCP")
        };

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

            foreach(var entry in entries)
            {
                Console.WriteLine(entry.id.ToString());
            }

            DisplayOverrideIsActive = true;
        }

        public override void Update(float t)
        {
            base.Update(t);

            loadingProgress += t / 5f;
            loadingProgressWhole += t;

            if (Math.Floor(loadingProgressWhole) % 2 == 0)
            {
                visibleProgress = loadingProgress;
            }

            if (loadingProgress >= 1.0f)
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

            Rectangle panelRect = new Rectangle()
            {
                X = bounds.X,
                Y = bounds.Y + 18 + (int)titleVector.Y,
                Width = bounds.Width,
                Height = bounds.Height - 18 + (int)titleVector.Y
            };

            ScrollablePanel.beginPanel(12982387, panelRect, lastScroll);

            Vector2 entryPosition = new Vector2(5, 23 + (int)titleVector.Y);
            Vector2 textVec = GuiData.smallfont.MeasureString("test");

            DrawHeader(bounds, entryPosition, textColor);
            entryPosition.Y += textVec.Y + 3;
            RenderedRectangle.doRectangle(0, (int)(entryPosition.Y - 5), bounds.Width, 2,
                Color.White);

            int entryNumber = 0;

            foreach (var entry in entries)
            {
                DrawEntryLine(entry, panelRect, entryPosition, textColor, 3718347 + entryNumber++);
                entryPosition.Y += textVec.Y + 3;
            }

            lastScroll = ScrollablePanel.endPanel(12982387, lastScroll, bounds, bounds.Height);
        }

        private void RenderEntryDetails(Rectangle bounds)
        {
            DrawMainWindowTitle(bounds);

            bool exitButton = Button.doButton(49120473, bounds.X + 10, bounds.Height + bounds.Y - 10,
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

        public string Content { get; private set; }

        public WiresharkEntry(uint id, string ipTo, string method = "GET", string protocol = "TCP")
        {
            this.id = id;
            this.ipFrom = "127.0.0.1";
            this.ipTo = ipTo;
            this.method = method;
            this.protocol = protocol;
            this.length = 0;
        }

        public WiresharkEntry(uint id, string ipFrom, string ipTo, string method = "GET", string protocol = "TCP")
        {
            this.id = id;
            this.ipFrom = ipFrom;
            this.ipTo = ipTo;
            this.method = method;
            this.protocol = protocol;
            this.length = 0;
        }

        public WiresharkEntry(uint id, string ipFrom, string ipTo, string content, string method = "GET", string protocol = "TCP")
        {
            this.id = id;
            this.ipFrom = ipFrom;
            this.ipTo = ipTo;
            this.method = method;
            this.protocol = protocol;

            Content = content;

            this.length = content.Length;
        }
    }
}
