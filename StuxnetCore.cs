using BepInEx;
using BepInEx.Hacknet;
using BepInEx.Logging;
using Hacknet;
using Hacknet.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Pathfinder.Action;
using Pathfinder.Command;
using Pathfinder.Daemon;
using Pathfinder.Event;
using Pathfinder.Event.Gameplay;
using Pathfinder.Event.Loading;
using Pathfinder.Event.Saving;
using Pathfinder.Executable;
using Pathfinder.Meta;
using Pathfinder.Meta.Load;
using Pathfinder.Replacements;
using Pathfinder.Util.XML;
using Stuxnet_HN.Actions;
using Stuxnet_HN.Actions.Dialogue;
using Stuxnet_HN.Actions.Nodes;
using Stuxnet_HN.Commands;
using Stuxnet_HN.Configuration;
using Stuxnet_HN.Cutscenes;
using Stuxnet_HN.Daemons;
using Stuxnet_HN.Executables;
using Stuxnet_HN.Gamemode;
using Stuxnet_HN.Patches;
using Stuxnet_HN.SMS;
using Stuxnet_HN.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Stuxnet_HN
{
    [BepInPlugin(ModGUID, ModName, ModVer)]
    [BepInDependency(XMOD_ID, BepInDependency.DependencyFlags.SoftDependency)]
    [Updater("https://git.gay/AutumnRivers/StuxnetHN/releases/download/latest/Stuxnet_HN.dll",
        "StuxnetHN.dll")]
    public class StuxnetCore : HacknetPlugin
    {
        public const string ModGUID = "autumnrivers.stuxnet";
        public const string ModName = "Stuxnet";
        public const string ModVer = "2.0.0";
        public const string VersionName = "WannaCry";

        public const uint CopyrightYear = 2025;

        public const string XMOD_ID = "tenesiss.XMOD";

        private readonly bool defaultSave = ExtensionLoader.ActiveExtensionInfo.AllowSave;

        public static ManualLogSource Logger;

        public static StuxnetConfig Configuration
        {
            get
            {
                return StuxnetConfig.GlobalConfig;
            }
        }

        public static List<string> redeemedCodes = new();
        public static List<string> unlockedRadio = new();
        public static Dictionary<string, int> receivedKeys = new();
        public static bool allowRadio = true;
        public static readonly string logPrefix = $"[{ModName}] ";
        public static string currentSequencerID = null;
        public static SequencerInfo currentSequencerInfo = null;
        public static string saveFlag = null;
        public static bool disableAlerts = false;

        public static Dictionary<string, WiresharkContents> wiresharkComps = new();

        // Custom Replacements
        public static Dictionary<string, string> customReplacements = new();

        // Illustrator
        public static States.IllustratorStates illustState = States.IllustratorStates.None;

        public static ChapterData ChapterData { get; set; }

        // Illustrator - Dialogue
        public static VisualNovelTextData CurrentVNTextData { get; set; }
        public static bool dialogueIsActive = false;

        public static StuxnetCutscene CurrentlyLoadedCutscene { get; set; }
        public static bool CutsceneIsActive
        {
            get
            {
                if (CurrentlyLoadedCutscene == null) return false;
                return CurrentlyLoadedCutscene.Active;
            }
        }

        public static bool XMODLoaded { get
            {
                return HacknetChainloader.Instance.Plugins.ContainsKey(XMOD_ID);
            }
        }

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
            "All roads lead to Rome!",
            "Goddamnit, Kris, where the HELL are we!?",
            "Preem. Let's delta outta here, choom.",
            "Oissu! Nice-a Neicha desu!",
            "KAGUYA, LOOK, A PLANE! BAHAHAHAHAHA",
            "What is small child doing with a 'hacked net'?",
            "[ GONE FISHIN' ]",
            "* But nobody came.",
            "They fly now? They fly now.",
            "Hacknet is technically connected to Hollow Knight.",
            "WORD OF THE DAY: Extirpate"
        };

        public override bool Load()
        {
            Random random = new();

            if(ExtensionLoader.ActiveExtensionInfo == null)
            {
                throw new Exception("Stuxnet is designed as an EXTENSION MOD, and WILL NOT WORK " +
                    "as a global mod. Please move the Stuxnet DLL to your extension's Plugins folder.");
            }

            Logger = Log;

            LogDebug("Initializing...");
            HarmonyInstance.PatchAll(typeof(StuxnetCore).Assembly);

            string extFolderPath = ExtensionLoader.ActiveExtensionInfo.FolderPath;

            LogDebug("Loading Daemons...");
            DaemonManager.RegisterDaemon<CodeRedemptionDaemon>();
            DaemonManager.RegisterDaemon<DebugDaemon>();
            DaemonManager.RegisterDaemon<VaultDaemon>();

            LogDebug("Registering Executables...");
            ExecutableManager.RegisterExecutable<RadioV3>("#RADIO_V3#");
            ExecutableManager.RegisterExecutable<WiresharkExecutable>("#WIRESHARK_EXE#");

            LogDebug("Registering Commands...");
            CommandManager.RegisterCommand("messenger", SMSCommands.ActivateSMS);
            CommandManager.RegisterCommand("unread", SMSCommands.CheckUnread);
            DebugCommands.RegisterCommands();

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

            ActionManager.RegisterAction<VisualNovelTextActions.CTCDialogueAction>("ShowCTCDialogue");
            ActionManager.RegisterAction<VisualNovelTextActions.AutoDialogueAction>("ShowAutoDialogue");

            // Node Actions
            ActionManager.RegisterAction<PlaceOnNetMap>("PlaceNodeOnNetMap");

            // Custom Replacement Actions
            ActionManager.RegisterAction<AddCustomReplacements>("AddCustomWildcard");
            ActionManager.RegisterAction<AddNodeCustomReplacement>("AddNodeIPWildcard");
            ActionManager.RegisterAction<AddAdminPassCustomReplacement>("AddNodeAdminWildcard");

            // Cutscene Actions
            Cutscenes.Actions.CutsceneActionsRegister.RegisterActions();

            // Misc. Actions
            ActionManager.RegisterAction<ForceConnect>("ForceConnectPlayer");
            ActionManager.RegisterAction<DisableAlertsIcon>("DisableAlertsIcon");
            ActionManager.RegisterAction<EnableAlertsIcon>("EnableAlertsIcon");
            ActionManager.RegisterAction<WriteToTerminal>("WriteToTerminal");
            FullscreenCredits.RegisterActions();

            // Quests Actions
            Quests.QuestActions.RegisterActions();

            // Persistence Actions
            Persistence.PersistenceActions.RegisterActions();
            #endregion register actions


            LogDebug("Creating events...");
            Action<SaveEvent> stuxnetSaveDelegate = InjectStuxnetSaveData;
            Action<OSUpdateEvent> stuxnetSaveCheckDelegate = CheckIfUserCanSave;
            Action<OSLoadedEvent> stuxnetInitDelegate = InitializeStuxnet;
            Action<SaveComputerEvent> wiresharkSaveDelegate = InjectWiresharkIntoComps;
            Action<SaveComputerLoadedEvent> wiresharkLoadDelegate = ParseWiresharkComps;

            EventManager<SaveEvent>.AddHandler(stuxnetSaveDelegate);
            EventManager<OSUpdateEvent>.AddHandler(stuxnetSaveCheckDelegate);
            EventManager<OSLoadedEvent>.AddHandler(stuxnetInitDelegate);
            EventManager<SaveComputerEvent>.AddHandler(wiresharkSaveDelegate);
            EventManager<SaveComputerLoadedEvent>.AddHandler(wiresharkLoadDelegate);

            LogDebug("Early initialization...");
            StuxnetConfig.LoadFromJson();
            EarlyInitializeStuxnet();

            LogDebug("Adding the finishing touches...");
            InitializeRadio();

            LogDebug("--- Finished Loading! :D");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("----------------------------------------------------");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("              < << <<< STUXNET >>> >> >             ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            string copyrightLine = string.Format("              AUTUMN RIVERS   (C)  {0}             ", CopyrightYear);
            Console.WriteLine(copyrightLine);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("     This one won't destroy your PC.  Probably.     ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("----------------------------------------------------");
            LogDebug(string.Format("--> v{0} ({1})", ModVer, VersionName));
            Console.ResetColor();

            LogDebug(postMsg[random.Next(0, postMsg.Length)]);

            return true;
        }

        public override bool Unload()
        {
            Persistence.PersistenceManager.Reset();
            GamemodeMenu.CloseMenu();
            return base.Unload();
        }

        public void EarlyInitializeStuxnet()
        {
            Localization.Localizer.Initialize();
            Persistence.PersistenceManager.Initialize();
            GamemodeMenu.Initialize();
        }

        public const string GitLink = "https://git.gay/AutumnRivers/StuxnetHN";

        public void InitializeStuxnet(OSLoadedEvent os_event)
        {
            if (disableAlerts) { os_event.Os.DisableEmailIcon = true; }

            SMSSystem.Initialize();
            NodeAttachment.BipSound = os_event.Os.content.Load<SoundEffect>("SFX/Bip");

            RamModule ramModule = os_event.Os.ram;
            int screenHeight = Math.Max(os_event.Os.terminal.Bounds.Height,
                os_event.Os.display.Bounds.Height);
            Rectangle smsBounds = new()
            {
                X = ramModule.Bounds.X + ramModule.Bounds.Width + 1,
                Y = ramModule.Bounds.Y,
                Height = screenHeight,
                Width = os_event.Os.display.Bounds.Width + os_event.Os.terminal.Bounds.Width + 2
            };
            SMSModule smsModule = new(smsBounds, os_event.Os);
            os_event.Os.modules.Add(smsModule);

            Quests.QuestManager.Initialize();

            string copyright = string.Format("StuxnetHN (C) {0} Autumn Rivers", CopyrightYear);
            UpdateCustomReplacement("STUXNET_COPYRIGHT", copyright);
            UpdateCustomReplacement("STUXNET_URL", GitLink);
            UpdateCustomReplacement("STUXNET_VERSION", ModVer);
            UpdateCustomReplacement("STUXNET_VERSION_NAME", VersionName);

            if(OS.DEBUG_COMMANDS && Configuration.ShowDebugText)
            {
                os_event.Os.terminal.writeLine($"[DEBUG] Stuxnet Initialized -- StuxnetHN v{ModVer} \"{VersionName}\" (GUID:{ModGUID})");
                os_event.Os.terminal.writeLine("[DEBUG] ( You're seeing this because debug commands are enabled. )");
            }
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

            Persistence.PersistenceManager.SavePersistentData();

            XAttribute stuxCodes = new XAttribute("RedeemedCodes", string.Join(" ", redeemedCodes));
            XAttribute stuxRadio = new XAttribute("UnlockedRadioIDs", string.Join(",", unlockedRadio));
            XAttribute stuxSeqID = new XAttribute("SetSequencerID", currentSequencerID ?? "NONE");
            XAttribute stuxSaveFlag = new XAttribute("SaveFlag", saveFlag ?? "NONE");
            XAttribute stuxDisableAlerts = new XAttribute("DisableAlerts", disableAlerts);

            stuxnetElem.Add(stuxCodes);
            stuxnetElem.Add(stuxRadio);
            stuxnetElem.Add(stuxSeqID);
            stuxnetElem.Add(stuxSaveFlag);
            stuxnetElem.Add(stuxDisableAlerts);

            if(AnimatedThemeIllustrator.CurrentTheme != null)
            {
                if(!string.IsNullOrWhiteSpace(AnimatedThemeIllustrator.CurrentTheme.FilePath))
                {
                    XAttribute stuxnetAnimatedThemePath = new("AnimatedThemePath",
                        AnimatedThemeIllustrator.CurrentTheme.FilePath);
                    stuxnetElem.Add(stuxnetAnimatedThemePath);
                }
            }

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

        public void InjectWiresharkIntoComps(SaveComputerEvent saveComp)
        {
            Computer c = saveComp.Comp;

            if(wiresharkComps.ContainsKey(c.idName))
            {
                WiresharkContents contents = wiresharkComps[c.idName];
                XElement compElem = saveComp.Element;

                LogDebug($"Saving Wireshark data on node {c.idName}...");

                XElement wiresharkElem = new XElement("WiresharkEntries");

                foreach(var entry in contents.entries)
                {
                    XElement wiresharkEntryElem = new XElement("pcap");

                    XAttribute wID = new XAttribute("id", entry.id.ToString());
                    XAttribute wFrom = new XAttribute("from", entry.ipFrom);
                    XAttribute wTo = new XAttribute("to", entry.ipTo);
                    XAttribute wMethod = new XAttribute("method", entry.method);
                    XAttribute wProtocol = new XAttribute("protocol", entry.protocol);
                    XAttribute wSecure = new XAttribute("secure", entry.secure.ToString());

                    wiresharkEntryElem.SetValue(entry.Content);

                    wiresharkEntryElem.Add(wID, wFrom, wTo, wMethod, wProtocol, wSecure);
                    wiresharkElem.Add(wiresharkEntryElem);
                }

                compElem.FirstNode.AddAfterSelf(wiresharkElem);
            }
        }

        public void ParseWiresharkComps(SaveComputerLoadedEvent saveComp)
        {
            Computer comp = saveComp.Comp;
            ElementInfo xCompElem = saveComp.Info;

            if(xCompElem.Children.FirstOrDefault(e => e.Name == "WiresharkEntries") != null)
            {
                ElementInfo wiresharkElem = xCompElem.Children.First(e => e.Name == "WiresharkEntries");
                WiresharkContents contents = new WiresharkContents();

                for (var i = 0; i < wiresharkElem.Children.Count; i++)
                {
                    ElementInfo e = wiresharkElem.Children[i];

                    uint id = uint.Parse(e.Attributes["id"]);
                    bool secure = bool.Parse(e.Attributes["secure"].ToLower());

                    WiresharkEntry entry = new WiresharkEntry(id, e.Attributes["from"], e.Attributes["to"],
                        e.Content, secure, e.Attributes["method"], e.Attributes["protocol"]);

                    contents.entries.Add(entry);
                }

                contents.originID = comp.idName;

                wiresharkComps.Add(comp.idName, contents);
            }
        }

        private void InitializeRadio()
        {
            var songs = Configuration.Audio.Songs;

            foreach(var song in songs)
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

            Logger.LogDebug("adding custom replacement: " + name + " value: " + value);

            if(customReplacements.ContainsKey(name))
            {
                customReplacements[name] = value;
            } else
            {
                customReplacements.Add(name, value);
            }
        }

        public static void CatchAndLogException(string message, Exception exception)
        {
            message = string.IsNullOrWhiteSpace(message) ? "Caught an exception" : message;
            string format = "{0}: {1}\n{2}";

            var exceptionMessage = exception.Message;
            var exceptionTrace = (exception.InnerException ?? exception).StackTrace;

            Logger.LogError(string.Format(format, message, exceptionMessage, exceptionTrace));
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
            StuxnetCore.disableAlerts = bool.Parse(info.Attributes["DisableAlerts"] ?? "false");

            if(info.Attributes.ContainsKey("AnimatedThemePath"))
            {
                var themePath = info.Attributes["AnimatedThemePath"];
                AnimatedTheme theme = new();
                try
                {
                    theme.LoadFromXml(themePath);
                    AnimatedThemeIllustrator.CurrentTheme = theme;
                } catch(Exception e)
                {
                    StuxnetCore.CatchAndLogException("Exception caught when attempting to load saved current animated theme", e);
                }
            }
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
#nullable enable
        public static string? Truncate(this string? value, int maxLength, string truncationSuffix = "...",
            bool splitNewlines = false)
        {
            if(splitNewlines && value.Contains("\n"))
            {
                value = value.Split('\n')[0];
                return value?.Length > maxLength
                ? value.Substring(0, maxLength) + truncationSuffix
                : value + truncationSuffix;
            }
            return value?.Length > maxLength
                ? value.Substring(0, maxLength) + truncationSuffix
                : value;
        }
#nullable disable
    }
}
