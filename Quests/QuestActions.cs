using Hacknet;
using Hacknet.Extensions;
using HarmonyLib;
using Pathfinder.Action;
using Pathfinder.Meta.Load;
using Pathfinder.Util;
using System;
using System.IO;
using System.Xml;

namespace Stuxnet_HN.Quests
{
    [Action("ReplaceMainMission")]
    public class SAReplaceMainMission : DelayablePathfinderAction
    {
        [XMLStorage]
        public string MissionName;

        public override void Trigger(OS os)
        {
            ComputerLoader.loadMission(Utils.GetFileLoadPrefix() + MissionName);
        }
    }

    [Action("LoadSideQuest")]
    public class SALoadSideQuest : DelayablePathfinderAction
    {
        [XMLStorage]
        public string MissionName;

        [XMLStorage]
        public string ID = null;

        public override void Trigger(OS os)
        {
            ActiveMission mission = (ActiveMission)ComputerLoader.readMission(Utils.GetFileLoadPrefix() + MissionName);
            string missionID = QuestActionPatches.GetIDFromMissionFile(MissionName);

            ID ??= missionID;

            StuxnetQuest quest = new(mission)
            {
                ID = ID
            };
            QuestManager.AddQuest(quest);
        }
    }

    [Action("UnloadSideQuest")]
    public class SAUnloadSideQuest : DelayablePathfinderAction
    {
        [XMLStorage]
        public string QuestID;

        public override void Trigger(OS os)
        {
            QuestManager.RemoveQuest(QuestID);
        }
    }

    [HarmonyPatch]
    public class QuestActionPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SALoadMission), "Trigger")]
        public static bool LoadMissionAsSidequest(SALoadMission __instance)
        {
            if (OS.currentInstance.currentMission == null ||
                !StuxnetCore.Configuration.Quests.ReplaceLoadMissionAction) return true;

            ActiveMission mission = (ActiveMission)ComputerLoader.readMission(Utils.GetFileLoadPrefix() + __instance.MissionName);
            string missionID = GetIDFromMissionFile(__instance.MissionName);

            StuxnetQuest quest = new(mission)
            {
                ID = missionID
            };
            QuestManager.AddQuest(quest);

            return false;
        }

        public static string GetIDFromMissionFile(string filename)
        {
            string filepath = Utils.GetFileLoadPrefix() + filename;
            Stream input = File.OpenRead(filepath);
            XmlReader xmlReader = XmlReader.Create(input);
            while(xmlReader.Name != "mission")
            {
                xmlReader.Read();
                if (xmlReader.EOF)
                {
                    return null;
                }
            }

            if(xmlReader.MoveToAttribute("id"))
            {
                string id = xmlReader.ReadContentAsString();
                xmlReader.Close();
                return id;
            }

            xmlReader.Close();
            return null;
        }
    }
}
