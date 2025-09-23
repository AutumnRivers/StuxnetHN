using System;
using BepInEx;
using Hacknet;
using Pathfinder.Action;
using Pathfinder.Util;

namespace Stuxnet_HN.Cutscenes.Actions
{
    public static class CutsceneActionsRegister
    {
        public static void RegisterActions()
        {
            ActionManager.RegisterAction<SAPreloadCutscene>("PreloadCutscene");
            ActionManager.RegisterAction<SATriggerCutscene>("TriggerCutscene");
        }
    }

    public class SAPreloadCutscene : PathfinderAction
    {
        [XMLStorage]
        public string FilePath;

        public override void Trigger(object os_obj)
        {
            string path = Utils.GetFileLoadPrefix() + FilePath;

            StuxnetCutscene cutscene = new();
            cutscene.LoadFromXml(path);

            StuxnetCache.CacheCutscene(cutscene);
        }
    }

    public class SATriggerCutscene : PathfinderAction
    {
        [XMLStorage]
        public string FilePath;

        public override void Trigger(object os_obj)
        {
            FilePath = Utils.GetFileLoadPrefix() + FilePath;

            if(StuxnetCache.TryGetCachedCutscene(FilePath, out var cutscene))
            {
                cutscene.LoadInCutscene();
                cutscene.Active = true;
            } else
            {
                try
                {
                    StuxnetCutscene cs = new();
                    cs.LoadFromXml(FilePath);
                    cs.LoadInCutscene();
                    cs.Active = true;
                } catch(Exception e)
                {
                    StuxnetCore.Logger.LogWarning("Caught exception when attempting to load cutscene:\n" +
                        string.Format("{0}\n{1}", e.ToString(), (e.InnerException ?? e).StackTrace)
                        );
                }
            }
        }
    }
}
