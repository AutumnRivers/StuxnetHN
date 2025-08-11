using BepInEx;
using BepInEx.Hacknet;
using BepInEx.Logging;
using Hacknet;
using Microsoft.Xna.Framework.Audio;
using Pathfinder.Event;
using Pathfinder.Event.Loading;
using Pathfinder.Meta.Load;
using StuxnetHN.Audio.Actions;
using System;
using System.Collections.Generic;

namespace StuxnetHN.Audio
{
    [BepInPlugin(ModGUID, ModNamae, ModVer)]
    [BepInDependency("autumnrivers.stuxnet", BepInDependency.DependencyFlags.HardDependency)]
    public class StuxnetAudioCore : HacknetPlugin
    {
        public const string ModGUID = "autumnrivers.stuxnet.audio";
        public const string ModNamae = "Stuxnet.Audio";
        public const string ModVer = "0.1.0";

        internal static ManualLogSource Logger;

        public static Dictionary<string, SoundEffect> SFXCache = new();

        public override bool Load()
        {
            Logger = Log;

            Log.LogInfo("[))) SASS < Stuxnet Audio SubSystem > Loading... (((]");
            Log.LogInfo("SASS IS STILL IN ALPHA! REPORT ANY BREAKAGE!");
            Log.LogInfo("<( YOU ACCEPT EVERYTHING THAT WILL HAPPEN FROM NOW ON. )");
            Log.LogDebug(string.Format("--> v{0}", ModVer));

            Log.LogInfo("[))) Registering Events... Yes, you're invited, too... (((]");
            EventManager<OSLoadedEvent>.AddHandler(InitializeSASS);

            Log.LogInfo("[))) SASS Successfully Loaded! Are you proud of me? (((]");

            return true;
        }

        public override bool Unload()
        {
            if(SFXCache.Count > 0)
            {
                Log.LogInfo("[))) Clearing Cache (((]");
                foreach(var sound in SFXCache.Values)
                {
                    sound.Dispose();
                }
                SFXCache.Clear();
                Log.LogInfo("[))) Cache Cleared! Finally, some breathing room... (((]");
            }

            return base.Unload();
        }

        public static void InitializeSASS(OSLoadedEvent oSLoadedEvent)
        {
            Logger.LogInfo("[))) Preloading Built-in SFX (((]");
            PlaySFX.BipSound = OS.currentInstance.content.Load<SoundEffect>("SFX/Bip");
            PlaySFX.StingerSFX = OS.currentInstance.content.Load<SoundEffect>("DLC/SFX/GlassBreak");
            Logger.LogInfo("[))) Preload Complete! Patting myself on the back... (((]");
        }
    }
}
