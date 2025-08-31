using BepInEx;
using Hacknet;
using Pathfinder.GUI;
using System.Collections.Generic;
using System.Linq;

namespace Stuxnet_HN.Quests
{
    public static class QuestManager
    {
        public static ActiveMission MainMission
        {
            get
            {
                if (OS.currentInstance == null) return null;

                return OS.currentInstance.currentMission;
            }
        }

        public static List<StuxnetQuest> Quests { get; set; } = new();

        public static void Initialize()
        {
            if(StuxnetCore.XMODLoaded)
            {
                StuxnetCore.Logger.LogDebug("XMOD is detected. Stuxnet.Quests will also load XMissions.");
                XMODCompat.QuestsXMODCompat.AddXMissionsAsQuests();
            }
        }

        public static void AttemptCompleteMissions()
        {
            foreach(var quest in Quests)
            {
                if (!quest.Mission.activeCheck) continue;
                quest.AttemptCompleteMission();
            }
        }

        public static bool AttemptCompleteQuest(StuxnetQuest quest, List<string> details = null)
        {
            var additionalDetails = details ?? new List<string>();
            quest.AdditionalDetails = additionalDetails;

            return quest.AttemptCompleteMission();
        }

        public static void AddQuest(ActiveMission mission, int priority = -1)
        {
            if (Quests.Any(q => q.Mission == mission)) return;
            StuxnetQuest quest = new(mission, priority);
            AddQuest(quest);
        }

        public static void AddQuest(StuxnetQuest quest, int priority = -1)
        {
            if (Quests.Any(q => q.Mission == quest.Mission)) return;
            
            if(priority >= 0)
            {
                Quests.Insert(priority, quest);
            } else
            {
                Quests.Add(quest);
            }
        }

        public static void RemoveQuest(StuxnetQuest quest)
        {
            if (!Quests.Contains(quest)) return;

            if(StuxnetCore.XMODLoaded && quest.IsXMission)
            {
                XMODCompat.QuestsXMODCompat.RemoveXMissionFromXMOD(quest.OriginalXMission);
            }

            int index = Quests.IndexOf(quest);
            Quests.RemoveAt(index);
        }

        public static void RemoveQuest(string questID)
        {
            var quest = Quests.FirstOrDefault(q => q.ID == questID);
            if (quest == null) return;
            PFButton.ReturnID(quest.CompletionButtonID);
            RemoveQuest(quest);
        }

        public static int IndexOfQuest(StuxnetQuest quest)
        {
            return Quests.IndexOf(quest);
        }
    }

    public class StuxnetQuest
    {
        public int Priority { get; set; } = -1;
        public ActiveMission Mission { get; set; }
        public bool IsXMission { get; set; } = false;
        public object OriginalXMission { get; set; } = null;

        public string ID { get; set; } = string.Empty;
        public int CompletionButtonID { get; set; } = PFButton.GetNextID();

        public bool ShowFailureTicker { get; set; } = false;

        public string Title
        {
            get
            {
                string title;
                if(Mission.postingTitle.IsNullOrWhiteSpace())
                {
                    title = Mission.email.subject;
                } else
                {
                    title = Mission.postingTitle;
                }
                return title;
            }
        }
        public string Description
        {
            get
            {
                string description;
                if(Mission.postingTitle.IsNullOrWhiteSpace())
                {
                    description = Mission.email.body;
                } else
                {
                    description = Mission.postingBody;
                }
                return description;
            }
        }

        public List<string> AdditionalDetails { get; set; } = new();

        public StuxnetQuest(ActiveMission mission)
        {
            Mission = mission;
        }

        public StuxnetQuest(ActiveMission mission, int priority)
        {
            Mission = mission;
            Priority = priority;
        }

        public bool AttemptCompleteMission()
        {
            bool complete = Mission.isComplete(AdditionalDetails);
            if (!complete) return false;

            if(IsXMission)
            {
                QuestManager.RemoveQuest(this);
                var xMissionType = OriginalXMission.GetType();
                var finishMethod = xMissionType.GetMethod("finish",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance);
                finishMethod.Invoke(OriginalXMission, null);
                return true;
            }

            if(Mission.endFunctionName != null)
            {
                MissionFunctions.runCommand(Mission.endFunctionValue, Mission.endFunctionName);
            }

            if(Mission.nextMission != "NONE")
            {
                Mission = (ActiveMission)ComputerLoader.readMission(Mission.nextMission);
            } else
            {
                QuestManager.RemoveQuest(this);
            }

            OS.currentInstance.saveGame();

            return true;
        }
    }
}
