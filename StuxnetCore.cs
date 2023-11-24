using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.IO;

using Hacknet;
using Hacknet.Extensions;
using Pathfinder.Daemon;
using Pathfinder.Executable;
using Pathfinder.Action;
using Pathfinder.Replacements;

using Pathfinder.Event.Saving;
using Pathfinder.Meta.Load;
using Pathfinder.Util.XML;

using BepInEx;
using BepInEx.Hacknet;

using Stuxnet_HN.Conditions;
using Stuxnet_HN.Daemons;
using Stuxnet_HN.Executables;
using Stuxnet_HN.Actions;

using Newtonsoft.Json;

using SongEntry = Stuxnet_HN.Executables.SongEntry;
using Pathfinder.Event;
using Pathfinder.Event.Gameplay;

namespace Stuxnet_HN
{
    [BepInPlugin(ModGUID, ModName, ModVer)]
    public class StuxnetCore : HacknetPlugin
    {
        public const string ModGUID = "autumnrivers.stuxnet";
        public const string ModName = "Stuxnet";
        public const string ModVer = "1.0.1";

        private readonly bool defaultSave = ExtensionLoader.ActiveExtensionInfo.AllowSave;

        public static string[] redeemedCodes = new string[]{};
        public static List<string> unlockedRadio = new List<string>();

        public static bool allowRadio = true;

        public static readonly string logPrefix = $"[{ModName}] ";

        public static string currentSequencerID = null;
        public static SequencerInfo currentSequencerInfo = null;

        public static string saveFlag = null;

        public static readonly string[] postMsg = new string[]
        {
            "So I got that going for me, which is nice.",
            "Also try Uplink!",
            "Also try NITE Team 4!",
            "Also try Midnight Protocol!",
            "Also try Wrestledunk Sports!",
            "Have you heard of Archipelago?",
            "He's right behind me, isn't he?",
            "Bit hard to kill a guy that's already dead.",
            "No refunds!",
            "I hope you know what you're doing. I sure don't.",
            "I am a raft floating on an ocean of shame.",
            "Track down everyone responsible for this mess.",
            "I'd just like to interject for moment.",
            "All hail Mott!",
            "Splines: Reticulated."
        };

        public override bool Load()
        {
            Random random = new Random();

            LogDebug("Initializing...");
            HarmonyInstance.PatchAll(typeof(StuxnetCore).Assembly);

            LogDebug("Loading Daemons...");
            DaemonManager.RegisterDaemon<CodeRedemptionDaemon>();
            DaemonManager.RegisterDaemon<DebugDaemon>();

            LogDebug("Registering Executables...");
            ExecutableManager.RegisterExecutable<RadioV3>("#RADIO_V3#");

            LogDebug("Registering Actions...");
            // Radio Actions
            ActionManager.RegisterAction<RadioActions.AddSong>("AddSongToRadio");
            ActionManager.RegisterAction<RadioActions.RemoveSong>("RemoveSongFromRadio");
            ActionManager.RegisterAction<RadioActions.PreventRadioAccess>("PreventRadioAccess");
            ActionManager.RegisterAction<RadioActions.AllowRadioAccess>("AllowRadioAccess");

            // Sequencer Actions
            ActionManager.RegisterAction<SequencerActions.ChangeSequencerManually>("ChangeSequencerManually");
            ActionManager.RegisterAction<SequencerActions.ChangeSequencerFromID>("ChangeSequencerFromID");
            ActionManager.RegisterAction<SequencerActions.ClearCustomSequencer>("ClearCustomSequencer");

            // Save Actions
            ActionManager.RegisterAction<SaveActions.DenySaves>("DenySaves");
            ActionManager.RegisterAction<SaveActions.AllowSaves>("AllowSaves");
            ActionManager.RegisterAction<SaveActions.RequireFlagForSaves>("RequireFlagForSaves");

            // Misc. Actions
            ActionManager.RegisterAction<ForceConnect>("ForceConnectPlayer");
            ActionManager.RegisterAction<DisableAlertsIcon>("DisableAlertsIcon");
            ActionManager.RegisterAction<EnableAlertsIcon>("EnableAlertsIcon");
            ActionManager.RegisterAction<WriteToTerminal>("WriteToTerminal");

            LogDebug("Registering Conditions...");
            ConditionManager.RegisterCondition<OnSequencerKill>("OnExtSequencerKill");

            LogDebug("Creating events...");
            Action<SaveEvent> stuxnetSaveDelegate = InjectStuxnetSaveData;
            Action<OSUpdateEvent> stuxnetSaveCheckDelegate = CheckIfUserCanSave;

            EventManager<SaveEvent>.AddHandler(stuxnetSaveDelegate);
            EventManager<OSUpdateEvent>.AddHandler(stuxnetSaveCheckDelegate);

            LogDebug("Reticulating more splines...");
            InitializeRadio();

            LogDebug("--- Finished Initialization! \\o/");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("----------------------------------------------------");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("                       STUXNET                      ");
            Console.WriteLine("              AUTUMN RIVERS  (C) 2023               ");
            Console.WriteLine("     This one won't destroy your PC.  Probably.     ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("----------------------------------------------------");
            LogDebug("--> v" + ModVer);
            Console.ResetColor();

            LogDebug(postMsg[random.Next(0, postMsg.Length)]);

            return true;
        }

        public void CheckIfUserCanSave(OSUpdateEvent os_update)
        {
            OS os = os_update.OS;

            if(saveFlag.IsNullOrWhiteSpace()) {
                ExtensionLoader.ActiveExtensionInfo.AllowSave = defaultSave;
                return;
            }

            bool avoidFlag = false;
            string targetFlag = saveFlag;

            if(saveFlag.StartsWith("!"))
            {
                avoidFlag = true;
                targetFlag = saveFlag.Substring(1);
            }

            ProgressionFlags userFlags = os.Flags;

            ExtensionLoader.ActiveExtensionInfo.AllowSave = (
                (userFlags.HasFlag(targetFlag) && !avoidFlag) || (!userFlags.HasFlag(targetFlag) && avoidFlag)
            );
        }

        public void InjectStuxnetSaveData(SaveEvent save_event)
        {
            XElement stuxnetElem = new XElement("StuxnetData");

            XAttribute stuxCodes = new XAttribute("RedeemedCodes", string.Join(" ", redeemedCodes));
            XAttribute stuxRadio = new XAttribute("UnlockedRadioIDs", string.Join(",", unlockedRadio));
            XAttribute stuxSeqID = new XAttribute("SetSequencerID", currentSequencerID);
            XAttribute stuxSaveFlag = new XAttribute("SaveFlag", saveFlag);

            stuxnetElem.Add(stuxCodes);
            stuxnetElem.Add(stuxRadio);
            stuxnetElem.Add(stuxSeqID);
            stuxnetElem.Add(stuxSaveFlag);

            save_event.Save.FirstNode.AddBeforeSelf(stuxnetElem);

            // Manual sequencer info
            if(currentSequencerInfo == null) { return; }

            XElement seqElem = new XElement("StuxnetSequencerInfo");

            XAttribute seqTargetID = new XAttribute("TargetID", currentSequencerInfo.targetIDorIP);
            XAttribute seqFlag = new XAttribute("RequiredFlag", currentSequencerInfo.requiredFlag);
            XAttribute seqActions = new XAttribute("ActionsToRun", currentSequencerInfo.sequencerActions);
            XAttribute seqSpinUp = new XAttribute("SpinUpTime", currentSequencerInfo.spinUpTime.ToString());

            seqElem.Add(seqTargetID);
            seqElem.Add(seqFlag);
            seqElem.Add(seqActions);
            seqElem.Add(seqSpinUp);

            stuxnetElem.AddAfterSelf(seqElem);
        }

        private void InitializeRadio()
        {
            string extensionFolder = ExtensionLoader.ActiveExtensionInfo.FolderPath;
            string expectedRadioFilePath = extensionFolder + "/radio.json";

            if(!File.Exists(expectedRadioFilePath)) { return; }

            StreamReader radioFileStream = new StreamReader(expectedRadioFilePath);
            string radioFile = radioFileStream.ReadToEnd();
            radioFileStream.Close();

            var radioJSON = JsonConvert.DeserializeObject<Dictionary<string, SongEntry>>(radioFile);

            foreach(var song in radioJSON)
            {
                string id = song.Key;
                SongEntry songEntry = song.Value;

                if(!songEntry.initial) { continue; }

                unlockedRadio.Add(id);
            }
        }

        private void LogDebug(string message)
        {
            Log.LogDebug(logPrefix + message);
        }
    }

    [SaveExecutor("HacknetSave.StuxnetData")]
    public class ReadStuxnetSaveData : SaveLoader.SaveExecutor
    {
        public void LoadSaveData(ElementInfo info)
        {
            StuxnetCore.redeemedCodes = info.Attributes["RedeemedCodes"].Split(' ');
            StuxnetCore.unlockedRadio = info.Attributes["UnlockedRadioIDs"].Split(',').ToList();
            StuxnetCore.currentSequencerID = info.Attributes["SetSequencerID"];
            StuxnetCore.saveFlag = info.Attributes["SaveFlag"];
        }

        public override void Execute(EventExecutor exec, ElementInfo info)
        {
            LoadSaveData(info);
        }
    }

    [SaveExecutor("HacknetSave.StuxnetSequencerInfo")]
    public class ReadStuxnetSequencerSave : SaveLoader.SaveExecutor
    {
        public override void Execute(EventExecutor exec, ElementInfo info)
        {
            SequencerInfo savedSequencer = new SequencerInfo
            {
                requiredFlag = info.Attributes["RequiredFlag"],
                targetIDorIP = info.Attributes["TargetID"],
                sequencerActions = info.Attributes["ActionsToRun"],
                spinUpTime = float.Parse(info.Attributes["SpinUpTime"])
            };

            StuxnetCore.currentSequencerInfo = savedSequencer;
        }
    }

    public static class StringExt
    {
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
