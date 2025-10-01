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

        [XMLStorage]
        public bool Quietly = false;

        public override void Trigger(OS os)
        {
            if(string.IsNullOrWhiteSpace(Name))
            {
                StuxnetCore.Logger.LogError("SACollectAchievement missing achievement name!");
                return;
            }
            var achvExists = StuxnetAchievementsManager.GetAchievement(Name) != null;
            if(!achvExists)
            {
                StuxnetCore.Logger.LogError(
                    string.Format("SACollectAchievement: '{0}' is not a valid achievement. " +
                    "Remember that names are CASE-SENSITIVE!", Name));
                return;
            }
            if (StuxnetAchievementsManager.HasCollectedAchievement(Name)) return;
            StuxnetAchievementsManager.CollectAchievement(Name);

            if(!Quietly)
            {
                AchievementPatches.QueueAchievement(Name);
            } else
            {
                os.write(
                    string.Format("Achievement unlocked!\n{0}", Name)
                    );
            }
        }
    }
}
