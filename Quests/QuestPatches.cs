using Pathfinder.Event.Gameplay;
using Pathfinder.Meta.Load;

namespace Stuxnet_HN.Quests
{
    public class QuestCheckPatches
    {
        [Event()]
        public static void CheckQuestsOnUpdate(OSUpdateEvent updateEvent)
        {
            if (!QuestPanelIllustrator.Enabled) return;

            if(StuxnetCore.XMODLoaded
                && !StuxnetCore.Configuration.Quests.IgnoreXMODMissions)
            {
                XMODCompat.QuestsXMODCompat.AddXMissionsAsQuests();
                XMODCompat.QuestsXMODCompat.CheckForLostMissions();
            }

            QuestManager.AttemptCompleteMissions();
        }
    }
}
