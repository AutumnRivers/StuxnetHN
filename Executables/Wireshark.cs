using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BepInEx;

using Hacknet;
using Hacknet.Gui;
using Hacknet.UIUtils;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Pathfinder.Executable;
using Pathfinder.GUI;
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
            Error,
            Capturing,
            CaptureSuccess,
            CaptureFailure
        }

        private ScrollableSectionedPanel entriesPanel;

        private List<WiresharkEntry> entries = new List<WiresharkEntry>();

        public bool DisplayOverrideIsActive { get; set; }

        private float loadingProgress = 0f;
        private float visibleProgress = 0f;
        private float loadingProgressWhole = 0f;

        private WiresharkState currentState = WiresharkState.Loading;
        private bool loaded = false;

        private float captureProgress = 0f;
        private bool isWiresharkedPC = false;

        private WiresharkContents currentContents;
        private WiresharkContents capturedContents;

        // Button IDs
        private int exeExitButtonID = PFButton.GetNextID();
        private int entryExitButtonID = PFButton.GetNextID();
        private int spareButtonID = PFButton.GetNextID();

        private List<int> entryButtonIDs = new List<int>();

        public TrailLoadingSpinnerEffect spinner;
        public float timeActive = 0f;

        public WiresharkEntry CurrentEntry { get; private set; }

        public WiresharkExecutable() : base()
        {
            this.baseRamCost = 250;
            this.ramCost = 250;
            this.IdentifierName = "Wireshark";
            this.name = "Wireshark";
            this.needsProxyAccess = false;

            spinner = new TrailLoadingSpinnerEffect(os);
        }

        public override void OnInitialize()
        {
            base.OnInitialize();

            Computer targetComp = ComputerLookup.FindByIp(targetIP);

            foreach (var exe in os.exes)
            {
                if (exe is WiresharkExecutable)
                {
                    this.needsRemoval = true;
                    os.terminal.writeLine("[WSHRK] Only one instance of Wireshark can be ran at a time!");
                }
            }

            if(Args.Length < 2)
            {
                this.needsRemoval = true;
                os.terminal.writeLine("[WSHRK] No Arguments Found - Please refer to the user manual");
                return;
            }

            DisplayOverrideIsActive = true;
            entriesPanel = new ScrollableSectionedPanel(415, GuiData.spriteBatch.GraphicsDevice);

            if (Args[1] == "--capture")
            {
                if(!targetComp.PlayerHasAdminPermissions())
                {
                    this.needsRemoval = true;
                    os.terminal.writeLine("[WSHRK] You must have administrator access to capture a node's traffic.");
                } else if(os.ramAvaliable < 350)
                {
                    this.needsRemoval = true;
                    os.terminal.writeLine("[WSHRK] Capturing traffic requires 350mb of RAM or more.");
                }

                this.ramCost = 350;
                this.CanBeKilled = false;

                os.terminal.writeLine("[WSHRK] Capturing network traffic...");

                currentState = WiresharkState.Capturing;

                return;
            }

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

                if(captureContents == null || !captureContents.IsValid)
                {
                    currentState = WiresharkState.Error;
                    os.terminal.writeLine("[WHSRK] Invalid File");
                    return;
                }

                currentContents = captureContents;
                entries = captureContents.entries;

                foreach(var _ in entries)
                {
                    entryButtonIDs.Add(PFButton.GetNextID());
                }
            }
        }

        public override void Update(float t)
        {
            base.Update(t);

            if(isExiting) { return; }

            if (loadingProgress < 1.0f && currentState == WiresharkState.Loading)
            {
                loadingProgress += t / 5f;
                loadingProgressWhole += t;

                if (Math.Floor(loadingProgressWhole) % 2 == 0)
                {
                    visibleProgress = loadingProgress;
                }
            } else if(loaded == false && currentState == WiresharkState.Loading)
            {
                loaded = true;
                currentState = WiresharkState.ShowEntries;
            }

            if(currentState == WiresharkState.CaptureSuccess)
            {
                GenerateCaptureFile();
                isExiting = true;
                return;
            }
        }

        private void GenerateCaptureFile()
        {
            WiresharkContents contents = capturedContents;
            Computer target = ComputerLookup.FindByIp(targetIP);
            string filename = $"{targetIP}.pcap";
            contents.originID = target.idName;

            Folder userWiresharkFolder = os.thisComputer.getFolderFromPath("home/wireshark", true);

            if(userWiresharkFolder.containsFile(filename))
            {
                return;
            }

            FileEntry pcapFile = new FileEntry(contents.GetEncodedFileString(), $"{targetIP}.pcap");
            userWiresharkFolder.files.Add(pcapFile);

            os.terminal.writeLine($"[WSHRK] Capture file written to /home/wireshark/{filename}! <3");

            target.log("WIRESHARK_CAPTURE_CREATED");
        }

        public override void Draw(float t)
        {
            base.Draw(t);
            drawTarget();
            drawOutline();

            if(currentState != WiresharkState.Capturing && currentState != WiresharkState.Error)
            {
                timeActive += (float)os.lastGameTime.ElapsedGameTime.TotalSeconds;
                spinner.Draw(bounds, GuiData.spriteBatch, 1f, 1f - timeActive * 0.4f, 0f, Color.CornflowerBlue * 0.15f);
            }

            string currentStateString = "UNKNWON";

            switch(currentState)
            {
                case WiresharkState.Loading:
                    currentStateString = "LOADING...";
                    DisplayOverrideIsActive = true;
                    break;
                case WiresharkState.ShowEntry:
                case WiresharkState.ShowEntries:
                case WiresharkState.CaptureSuccess:
                    currentStateString = "ACTIVE";
                    DisplayOverrideIsActive = true;
                    break;
                case WiresharkState.Capturing:
                    currentStateString = "CAPTURING";
                    RenderCaptureProgress(bounds, t);
                    DisplayOverrideIsActive = false;
                    break;
                case WiresharkState.CaptureFailure:
                case WiresharkState.Error:
                    currentStateString = "!! ERROR !!";
                    DisplayOverrideIsActive = false;
                    break;
            }

            if(isExiting) { currentStateString = "Shutting down..."; }

            Vector2 smallVec = GuiData.tinyfont.MeasureString("test");

            TextItem.doTinyLabel(new Vector2(bounds.X + 5, bounds.Y + 10), "-- Wireshark --\n(c)Wireshark Foundation",
                Color.White);
            TextItem.doLabel(new Vector2(bounds.X + 5, bounds.Y + smallVec.Y + 18), currentStateString,
                Color.White);

            if(currentState == WiresharkState.Capturing) { return; }

            bool exitButton = Button.doButton(exeExitButtonID, bounds.X + 5, bounds.Y + bounds.Height - 30, bounds.Width / 3,
                25, "Exit...", Color.Red);

            if(exitButton)
            {
                this.DisplayOverrideIsActive = false;
                this.isExiting = true;
            }
        }

        public void RenderCaptureProgress(Rectangle bounds, float t)
        {
            captureProgress += t;

            if(captureProgress >= 7.5f && !isWiresharkedPC)
            {
                Computer targetComp = ComputerLookup.FindByIp(targetIP);

                if(!StuxnetCore.wiresharkComps.ContainsKey(targetComp.idName))
                {
                    currentState = WiresharkState.Error;
                    os.terminal.writeLine("[WSHRK] No Relevant Network Traffic Found");
                    return;
                }

                isWiresharkedPC = true;
            }

            if(captureProgress >= 20f)
            {
                Computer targetComp = ComputerLookup.FindByIp(targetIP);

                currentState = WiresharkState.CaptureSuccess;
                capturedContents = StuxnetCore.wiresharkComps[targetComp.idName];
            }

            string startBar = "[";
            string endBar = "]";

            StringBuilder loadingBar = new StringBuilder("");

            Vector2 smallVec = GuiData.smallfont.MeasureString("[");

            TextItem.doSmallLabel(new Vector2(bounds.X + 10, bounds.Center.Y - (smallVec.Y / 2f)),
                startBar, Color.White);
            TextItem.doSmallLabel(new Vector2(bounds.X + bounds.Width - 15, bounds.Center.Y - (smallVec.Y / 2f)),
                endBar, Color.White);

            int circlesToDraw = (int)Math.Floor(captureProgress);

            for(var i = 0; i < circlesToDraw; i++)
            {
                loadingBar.Append("=");
            }

            TextItem.doSmallLabel(new Vector2(bounds.X + smallVec.X + 8, bounds.Center.Y - (smallVec.Y / 2f)),
                loadingBar.ToString(), Color.CornflowerBlue);
        }

        public void RenderMainDisplay(Rectangle dest, SpriteBatch sb)
        {
            if(isExiting) { return; }

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
            } else if (currentState == WiresharkState.Capturing)
            {
                PatternDrawer.draw(dest, 1f, Color.Transparent, Color.CornflowerBlue * 0.3f, sb);
                RenderCapturingScreen(dest);
            } else
            {
                PatternDrawer.draw(dest, 1f, Color.Transparent, Color.CornflowerBlue * 0.3f, sb);
                RenderErrorScreen(dest);
            }
        }

        public void RenderErrorScreen(Rectangle dest)
        {
            DrawCenteredText(dest, "ERROR :: Check Terminal for More Information", GuiData.font);
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

            Action<int, Rectangle, SpriteBatch> drawEntries = delegate (int index, Rectangle drawbounds, SpriteBatch sb)
            {
                if(index + 1 < entries.Count)
                {
                    DrawEntryLine(entries[index], drawbounds, entryPosition, textColor, entryButtonIDs[index]);
                    entryPosition.Y += textVec.Y + 3;
                } else
                {
                    DrawEntryLine(entries[entries.Count - 1], drawbounds, entryPosition, textColor, spareButtonID);
                    return;
                }
            };

            Vector2 panelPosition = new Vector2(5, 23 + (int)titleVector.Y);
            entriesPanel.Draw(drawEntries, GuiData.spriteBatch, panelRect);
        }

        private void RenderEntryDetails(Rectangle bounds)
        {
            DrawMainWindowTitle(bounds);

            Computer targetComp = ComputerLookup.FindByIp(targetIP);

            string pageTitle = "The Wireshark Network Analyzer";
            Vector2 titleVector = GuiData.smallfont.MeasureString(pageTitle);
            Color textColor = Color.White;

            Vector2 fontVector = GuiData.font.MeasureString("test");

            int offset = bounds.Y + (int)titleVector.Y + 20;
            string id = CurrentEntry.id.ToString();

            TextItem.doLabel(new Vector2(bounds.X + 10, offset), $"Packet ID: {id} :: " +
                $"{CurrentEntry.ipFrom} -> {CurrentEntry.ipTo}", textColor);
            TextItem.doFontLabel(new Vector2(bounds.X + 10, offset + fontVector.Y + 5),
                $"PROTOCOL: {CurrentEntry.protocol}", GuiData.smallfont, textColor);

            RenderedRectangle.doRectangle(bounds.X, bounds.Center.Y - 1, bounds.Width, 2, Color.White);

            TextItem.doFontLabel(new Vector2(bounds.X + 10, bounds.Center.Y + 5), "--- PACKET CONTENTS:",
                GuiData.smallfont, textColor);

            // Packet Contents
            string displayContent = CurrentEntry.Content;

            if(CurrentEntry.secure && 
                ((!targetComp.PlayerHasAdminPermissions() && targetComp.idName == currentContents.originID) ||
                targetComp.idName != currentContents.originID)
                )
            {
                displayContent = "-- UNABLE TO VIEW ENCRYPTED PACKET --\n\n" +
                    "Please refer to the user manual.";
            }

            TextItem.doLabel(new Vector2(bounds.X + 10, bounds.Center.Y + 20), displayContent, textColor);

            bool exitButton = Button.doButton(entryExitButtonID, bounds.X + 10, bounds.Height + bounds.Y - 60,
                150, 50, "Go Back...", Color.Red);

            if(exitButton)
            {
                currentState = WiresharkState.ShowEntries;
            }
        }

        public void RenderCapturingScreen(Rectangle bounds)
        {
            DrawCenteredText(bounds, "Capturing... Follow Progress In EXE Display", GuiData.smallfont);
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

            if (content.IsNullOrWhiteSpace())
            {
                Content = "-- Empty Packet Data --";
                this.length = 0;
            } else
            {
                Content = content;
                this.length = Content.Length;
            }
        }
    }
}
