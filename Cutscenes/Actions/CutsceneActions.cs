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
            ActionManager.RegisterAction<SAPreloadCutscene>("RegisterCutscene");
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
            StuxnetCore.Logger.LogWarning("RegisterCutscene will be removed in favor of PreloadCutscene in Stuxnet 2.1.0, so please " +
                "change it in your extension ASAP.");

            string path = Utils.GetFileLoadPrefix() + FilePath;

            StuxnetCutscene cutscene = new();
            cutscene.LoadFromXml(path);

            StuxnetCache.CacheCutscene(cutscene);
        }
    }

    public class SATriggerCutscene : PathfinderAction
    {
        [XMLStorage]
        public string CutsceneID;

        [XMLStorage]
        public string FilePath;

        public override void Trigger(object os_obj)
        {
            if(!CutsceneID.IsNullOrWhiteSpace())
            {
                if(!FilePath.IsNullOrWhiteSpace())
                {
                    StuxnetCore.Logger.LogWarning(
                        "TriggerCutscene will exclusively use the FilePath variable in the future, " +
                        "so please remove it from your actions."
                        );
                } else
                {
                    throw new ArgumentException("TriggerCutscene now uses FilePath instead of CutsceneID - check " +
                        "Stuxnet 2.0's breaking changes list.");
                }
            }

            FilePath = Utils.GetFileLoadPrefix() + FilePath;

            if(StuxnetCache.TryGetCachedCutscene(FilePath, out var cutscene))
            {
                cutscene.LoadInCutscene();
            } else
            {
                try
                {
                    StuxnetCutscene cs = new();
                    cs.LoadFromXml(FilePath);
                    cs.LoadInCutscene();
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
