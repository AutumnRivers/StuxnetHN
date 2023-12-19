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

using Pathfinder.Event;
using Pathfinder.Event.Saving;
using Pathfinder.Event.Gameplay;

using Pathfinder.Meta.Load;
using Pathfinder.Util.XML;

using BepInEx;
using BepInEx.Hacknet;

using Stuxnet_HN.Conditions;
using Stuxnet_HN.Daemons;
using Stuxnet_HN.Executables;
using Stuxnet_HN.Static;
using Stuxnet_HN.Commands;

using Stuxnet_HN.Actions;
using Stuxnet_HN.Actions.Dialogue;
using Stuxnet_HN.Actions.Nodes;

using Newtonsoft.Json;

using Microsoft.Xna.Framework;

using SongEntry = Stuxnet_HN.Executables.SongEntry;

using Microsoft.Xna.Framework.Graphics;

using Pathfinder.Command;

namespace Stuxnet_HN
{
    [BepInPlugin(ModGUID, ModName, ModVer)]
    public class StuxnetCore : HacknetPlugin
    {
        public const string ModGUID = "autumnrivers.stuxnet";
        public const string ModName = "Stuxnet";
        public const string ModVer = "1.3.0";

        private readonly bool defaultSave = ExtensionLoader.ActiveExtensionInfo.AllowSave;

        public static List<string> redeemedCodes = new List<string>();
        public static List<string> unlockedRadio = new List<string>();

        public static Dictionary<string, int> receivedKeys = new Dictionary<string, int>();

        public static bool allowRadio = true;

        public static readonly string logPrefix = $"[{ModName}] ";

        public static string currentSequencerID = null;
        public static SequencerInfo currentSequencerInfo = null;

        public static string saveFlag = null;

        public static bool useScanLinesFix = false;

        // Custom Replacements
        public static Dictionary<string, string> customReplacements = new Dictionary<string, string>();

        // Temp. cache
        public static Dictionary<string, Color> colorsCache = new Dictionary<string, Color>();
        public static Dictionary<string, string> stxStringCache = new Dictionary<string, string>();
        public static Dictionary<string, Texture2D> texCache = new Dictionary<string, Texture2D>();

        public static Texture2D originalScanlines;

        // Illustrator
        public static States.IllustratorStates illustState = States.IllustratorStates.None;

        // Illustrator - Chapter Data
        public static string chapterTitle = "Chapter X";
        public static string chapterSubTitle = "Chapter Title";

        #region illustrator dialogue variables
        // Illustrator - Dialogue
        public static string dialogueText;
        public static bool dialogueIsCtc = false;
        public static float dialogueCompleteDelay = 0f;
        public static float dialogueSpeed = 1f;
        public static string dialogueEndActions;
        public static Color dialogueColor = Color.White;
        public static float backingOpacity = 0.6f;

        public static bool dialogueIsActive = false;
        #endregion illustrator dialogue variables

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
            "Splines: Reticulated.",
            "All roads lead to Rome!"
        };

        public override bool Load()
        {
            Random random = new Random();

            LogDebug("Initializing...");
            HarmonyInstance.PatchAll(typeof(StuxnetCore).Assembly);

            string extFolderPath = ExtensionLoader.ActiveExtensionInfo.FolderPath;

            LogDebug("Preloading images...");
            FileStream scanLinesStream = File.OpenRead(extFolderPath + "/Plugins/assets/ScanLinesFix.png");
            texCache["ScanLinesFix"] = Texture2D.FromStream(GuiData.spriteBatch.GraphicsDevice ,scanLinesStream);
            scanLinesStream.Dispose();

            LogDebug("Loading Daemons...");
            DaemonManager.RegisterDaemon<CodeRedemptionDaemon>();
            DaemonManager.RegisterDaemon<DebugDaemon>();
            DaemonManager.RegisterDaemon<VaultDaemon>();

            LogDebug("Registering Executables...");
            ExecutableManager.RegisterExecutable<RadioV3>("#RADIO_V3#");

            LogDebug("Registering Commands...");
            CommandManager.RegisterCommand("slfix", ToggleScanLinesFix.ToggleFix, true, false);

            #region register actions
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

            // Vault Actions
            ActionManager.RegisterAction<VaultKeyActions.AddVaultKey>("AddVaultKey");
            ActionManager.RegisterAction<VaultKeyActions.RemoveVaultKey>("RemoveVaultKey");

            // Dialogue / Chapter Actions
            ActionManager.RegisterAction<ChapterTitleActions.ShowChapterTitle>("ShowChapterTitle");
            ActionManager.RegisterAction<ChapterTitleActions.HideChapterTitle>("HideChapterTitle");

            ActionManager.RegisterAction<VisualNovelText.CTCDialogueAction>("ShowCTCDialogue");
            ActionManager.RegisterAction<VisualNovelText.AutoDialogueAction>("ShowAutoDialogue");

            // Node Actions
            ActionManager.RegisterAction<PlaceOnNetMap>("PlaceNodeOnNetMap");

            // Custom Replacement Actions
            ActionManager.RegisterAction<AddCustomReplacements>("AddCustomWildcard");
            ActionManager.RegisterAction<AddNodeCustomReplacement>("AddNodeIPWildcard");
            ActionManager.RegisterAction<AddAdminPassCustomReplacement>("AddNodeAdminWildcard");

            // Misc. Actions
            ActionManager.RegisterAction<ForceConnect>("ForceConnectPlayer");
            ActionManager.RegisterAction<DisableAlertsIcon>("DisableAlertsIcon");
            ActionManager.RegisterAction<EnableAlertsIcon>("EnableAlertsIcon");
            ActionManager.RegisterAction<WriteToTerminal>("WriteToTerminal");
            #endregion register actions

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

            LogDebug("Saving data...");

            XAttribute stuxCodes = new XAttribute("RedeemedCodes", string.Join(" ", redeemedCodes));
            XAttribute stuxRadio = new XAttribute("UnlockedRadioIDs", string.Join(",", unlockedRadio));
            XAttribute stuxSeqID = new XAttribute("SetSequencerID", currentSequencerID ?? "NONE");
            XAttribute stuxSaveFlag = new XAttribute("SaveFlag", saveFlag ?? "NONE");

            stuxnetElem.Add(stuxCodes);
            stuxnetElem.Add(stuxRadio);
            stuxnetElem.Add(stuxSeqID);
            stuxnetElem.Add(stuxSaveFlag);

            save_event.Save.FirstNode.AddBeforeSelf(stuxnetElem);

            // Manual sequencer info
            if(currentSequencerInfo != null)
            {
                LogDebug("Saving sequencer info...");

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

            // Received keys for vaults
            if(receivedKeys.Any())
            {
                LogDebug("Saving vault keys...");

                XElement keysElem = new XElement("StuxnetKeys");

                foreach(var entry in receivedKeys)
                {
                    XAttribute keyAttr = new XAttribute(entry.Key, entry.Value);

                    keysElem.Add(keyAttr);
                }

                stuxnetElem.AddAfterSelf(keysElem);
            }

            // Custom Replacements
            if(customReplacements.Any())
            {
                LogDebug("Saving custom wildcards...");

                XElement stxnReplacementsElem = new XElement("StuxnetWildcards");

                foreach(var wildcard in customReplacements)
                {
                    XElement wildcardElem = new XElement("Wildcard");
                    XAttribute wildcardName = new XAttribute("Name", wildcard.Key);
                    XAttribute wildcardValue = new XAttribute("Value", wildcard.Value);

                    wildcardElem.Add(wildcardName, wildcardValue);
                    stxnReplacementsElem.Add(wildcardElem);
                }

                stuxnetElem.AddAfterSelf(stxnReplacementsElem);
            }

            LogDebug("Save Successful!");
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

        public static void UpdateCustomReplacement(string name, string value)
        {
            name = $"#{name.ToUpper()}#";

            if(customReplacements.ContainsKey(name))
            {
                customReplacements[name] = value;
            } else
            {
                customReplacements.Add(name, value);
            }
        }
    }

    [SaveExecutor("HacknetSave.StuxnetData")]
    public class ReadStuxnetSaveData : SaveLoader.SaveExecutor
    {
        public void LoadSaveData(ElementInfo info)
        {
            string seqId = info.Attributes["SetSequencerID"] == "NONE" ? null : info.Attributes["SetSequencerID"];
            string sFlag = info.Attributes["SaveFlag"] == "NONE" ? null : info.Attributes["SaveFlag"];

            StuxnetCore.redeemedCodes = info.Attributes["RedeemedCodes"].Split(' ').ToList();
            StuxnetCore.unlockedRadio = info.Attributes["UnlockedRadioIDs"].Split(',').ToList();
            StuxnetCore.currentSequencerID = seqId;
            StuxnetCore.saveFlag = sFlag;
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

    [SaveExecutor("HacknetSave.StuxnetWildcards")]
    public class ReadStuxnetSavedWildcards : SaveLoader.SaveExecutor
    {
        public override void Execute(EventExecutor exec, ElementInfo info)
        {
            if(!info.Children.Any()) { return; }

            foreach(var wildcardElem in info.Children)
            {
                if(wildcardElem.Name != "Wildcard") { return; }
                StuxnetCore.customReplacements.Add(wildcardElem.Attributes["Name"], wildcardElem.Attributes["Value"]);
            }
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
