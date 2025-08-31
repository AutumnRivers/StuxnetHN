using Hacknet;
using System.Linq;
using HarmonyLib;

namespace Stuxnet_HN.Quests.XMODCompat
{
    internal static class QuestsXMODCompat
    {
        public static void AddXMissionsAsQuests()
        {
            var missions = XMOD.ParalellMissionManager.currentMissions;
            foreach(var mission in missions)
            {
                var quest = ConvertXMissionToQuest(mission);
                quest.ID = mission.identifier;
                QuestManager.AddQuest(quest);
            }
        }

        public static void CheckForLostMissions()
        {
            var missions = XMOD.ParalellMissionManager.currentMissions;
            foreach(var quest in QuestManager.Quests)
            {
                if (!quest.IsXMission) continue;
                if(!missions.Contains(quest.OriginalXMission))
                {
                    QuestManager.RemoveQuest(quest);
                }
            }
        }

        public static ActiveMission ConvertXMissionToHNMission(XMOD.Mission mission)
        {
            ActiveMission hnMission = new(mission.goals, "NONE", default);
            return hnMission;
        }

        public static StuxnetQuest ConvertXMissionToQuest(XMOD.Mission mission)
        {
            var hnMission = ConvertXMissionToHNMission(mission);
            StuxnetQuest quest = new(hnMission, -1)
            {
                IsXMission = true,
                OriginalXMission = mission
            };
            return quest;
        }

        public static void RemoveXMissionFromXMOD(object mission)
        {
            RemoveXMissionFromXMOD((XMOD.Mission)mission);
        }

        public static void RemoveXMissionFromXMOD(XMOD.Mission mission)
        {
            XMOD.ParalellMissionManager._removeMission(mission);
        }
    }
}
