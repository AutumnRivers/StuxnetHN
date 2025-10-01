using Hacknet;
using Pathfinder.Action;
using Pathfinder.Util;

namespace Stuxnet_HN.Persistence.Achievements
{
    public class AchievementActions
    {
        public static void RegisterActions()
        {
            ActionManager.RegisterAction<SACollectAchievement>("CollectAchievement");
        }
    }

    public class SACollectAchievement : DelayablePathfinderAction
    {
        [XMLStorage]
        public string Name;

        public override void Trigger(OS os)
        {
            if(string.IsNullOrWhiteSpace(Name))
            {
                StuxnetCore.Logger.LogError("SACollectAchievement missing achievement name!");
                return;
            }
            StuxnetAchievementsManager.CollectAchievement(Name);
        }
    }
}
