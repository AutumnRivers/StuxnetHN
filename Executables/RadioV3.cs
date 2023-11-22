using System.Collections.Generic;
using System.Linq;
using System.IO;

using Hacknet;
using Hacknet.Effects;
using Hacknet.Extensions;

using Pathfinder.Executable;

using Newtonsoft.Json;

using Microsoft.Xna.Framework;

using Hacknet.Gui;

namespace Stuxnet_HN.Executables
{
    public class RadioV3 : GameExecutable
    {
        public RaindropsEffect backdrop;

        SongEntry currentSong;

        List<SongEntry> songs = new List<SongEntry>();

        readonly List<string> songTitles = new List<string>();

        int selected = -1;
        int scroll = 0;

        bool canPlay = true;

        public RadioV3() : base()
        {
            this.baseRamCost = 300;
            this.ramCost = 300;
            this.IdentifierName = "RadioV3";
            this.name = "RadioV3";
            this.needsProxyAccess = false;
        }

        public override void OnInitialize()
        {
            base.OnInitialize();

            backdrop = new RaindropsEffect();
            backdrop.Init(OS.currentInstance.content);
            backdrop.MaxVerticalLandingVariane = 0.06f;
            backdrop.FallRate = 0.5f;

            UpdateSongList();
            UpdateGraphicalSongList();
        }

        public override void Update(float t)
        {
            base.Update(t);

            backdrop.Update(t, 30f);

            UpdateSongList();
            UpdateGraphicalSongList();
        }

        public override void Draw(float t)
        {
            base.Draw(t);

            drawTarget();
            drawOutline();

            backdrop.Render(bounds, spriteBatch, Color.MediumPurple * 0.6f, 5f, 30f);

            if(isExiting) { return; }

            if(StuxnetCore.allowRadio == false)
            {
                Vector2 blockVector = GuiData.smallfont.MeasureString(LocaleTerms.Loc("-- Radio access is denied --"));
                Vector2 blockPosition = new Vector2((float)(bounds.X + bounds.Width / 2) - blockVector.X / 2f, (float)(bounds.Y + bounds.Height / 2 - 10) - 20f);
                GuiData.spriteBatch.DrawString(GuiData.smallfont, LocaleTerms.Loc("-- Radio access is denied --"), blockPosition, Color.White);

                bool exitButton1 = Button.doButton(192017, bounds.X + 10, (int)blockPosition.Y + 25,
                bounds.Width - 20, 20, "Exit", Color.Red);

                isExiting = exitButton1;
            }

            if (StuxnetCore.allowRadio == false) { return; }

            if (currentSong != null && selected > -1) { selected = songs.FindIndex(s => s.path == currentSong.path); }

            SelectableTextList.scrollOffset = scroll;

            selected = SelectableTextList.doFancyList(192019, bounds.X + 5, bounds.Y + 20, bounds.Width - 11, bounds.Height - 70,
                songTitles.ToArray(), selected, Color.LightBlue, true);

            scroll = SelectableTextList.scrollOffset;

            bool exitButton = Button.doButton(192018, bounds.Center.X + 40, (bounds.Height + bounds.Y) - 30,
                (bounds.Width - bounds.Center.X) - 50, 20, "Exit", Color.Red);

            isExiting = exitButton;

            if(selected == -1) { return; }

            TextItem.doFontLabel(new Vector2(
                bounds.X + 10, (bounds.Y + bounds.Height) - 30),
                songs[selected].artist.Truncate(16), GuiData.smallfont, Color.White);

            if (songs[selected] == currentSong || !canPlay) { return; }

            PlaySong(songs[selected]);
        }

        public void PlaySong(SongEntry song)
        {
            canPlay = false;
            currentSong = song;

            string songPath = song.path;
            string extFolder = "../" + ExtensionLoader.ActiveExtensionInfo.FolderPath;

            MusicManager.transitionToSong(extFolder + songPath);

            canPlay = true;
        }

        public void UpdateSongList()
        {
            songs.Clear();

            string extensionFolder = ExtensionLoader.ActiveExtensionInfo.FolderPath;
            string expectedRadioFilePath = extensionFolder + "/radio.json";

            if (!File.Exists(expectedRadioFilePath)) { return; }

            StreamReader radioFileStream = new StreamReader(expectedRadioFilePath);
            string radioFile = radioFileStream.ReadToEnd();
            radioFileStream.Close();

            var radioJSON = JsonConvert.DeserializeObject<Dictionary<string, SongEntry>>(radioFile);

            foreach (string unlockedSongID in StuxnetCore.unlockedRadio)
            {
                if (!radioJSON.ContainsKey(unlockedSongID)
                    || songs.Contains(radioJSON[unlockedSongID])) { continue; }

                SongEntry song = radioJSON[unlockedSongID];

                song.songId = unlockedSongID;

                songs.Add(song);
            }

            songs = songs.OrderBy(s => s.artist).ToList();
        }

        public void UpdateGraphicalSongList()
        {
            songTitles.Clear();

            foreach (var song in songs)
            {
                string songTitle = song.artist + " - " + song.title;
                if (songTitles.Contains(songTitle)) { continue; }

                songTitles.Add(songTitle);
            }
        }
    }

    public class SongEntry
    {
        public string artist;
        public string title;
        public string path;
        public bool initial = false;
        public string songId;
    }
}
