using Hacknet;
using Microsoft.Xna.Framework.Audio;
using Pathfinder.Action;
using Pathfinder.Util;
using System;
using System.Collections.Generic;

namespace StuxnetHN.Audio.Actions
{
    [Pathfinder.Meta.Load.Action("PlaySound")]
    public class PlaySFX : DelayablePathfinderAction
    {
        [XMLStorage]
        public string SoundID;

        internal static SoundEffect BipSound;
        internal static SoundEffect StingerSFX;

        public override void Trigger(OS os)
        {
            if(SoundID.ToLower() == "random")
            {
                SoundID = Utils.random.Next(1, 8).ToString();
            }

            switch(SoundID.ToLower())
            {
                case "beep":
                case "warning":
                case "1":
                default:
                    OS.currentInstance.beepSound.Play();
                    break;
                case "crash":
                case "harsh":
                case "stinger":
                case "melt":
                case "meltimpact":
                case "2":
                    StingerSFX.Play();
                    break;
                case "connect":
                case "addnode":
                case "bip":
                case "3":
                    BipSound.Play();
                    break;
                case "mail":
                case "newmail":
                case "4":
                    OS.currentInstance.mailicon.newMailSound.Play();
                    break;
                case "etasspindown":
                case "spindown":
                case "etas1":
                case "5":
                    OS.currentInstance.TraceDangerSequence.spinDownSound.Play();
                    break;
                case "etasspinup":
                case "spinup":
                case "etas2":
                case "6":
                    OS.currentInstance.TraceDangerSequence.spinUpSound.Play();
                    break;
                case "etasimpact":
                case "impact":
                case "etas3":
                case "7":
                    OS.currentInstance.TraceDangerSequence.impactSound.Play();
                    break;
                case "bang":
                case "gunshot":
                case "8":
                    OS.currentInstance.IncConnectionOverlay.sound1.Play();
                    break;
                case "irc":
                case "notification":
                case "9":
                    OS.currentInstance.hubServerAlertsIcon.alertSound.Play();
                    break;
            }
        }
    }

    [Pathfinder.Meta.Load.Action("PlayCustomSound")]
    public class PlayCustomSoundAction : DelayablePathfinderAction
    {
        [XMLStorage]
        public string SoundFile;

        public override void Trigger(OS os)
        {
            string filepath = Utils.GetFileLoadPrefix() + SoundFile;

            if (filepath.StartsWith("Extensions", StringComparison.OrdinalIgnoreCase) &&
                !filepath.EndsWith(".ogg"))
            {
                filepath = "../" + filepath;
            }

            // If file is in cache, just use that (minimizes disk reading)
            // Could fill up memory, but should be fine so long as nobody is using, like
            // ridiculously large OGG files for sound effects.
            if(StuxnetAudioCore.SFXCache.ContainsKey(filepath))
            {
                StuxnetAudioCore.SFXCache[filepath].Play();
            } else if(filepath.EndsWith(".ogg"))
            {
                SoundEffect customSfx = OGGSoundEffectLoader.LoadOgg(filepath);
                StuxnetAudioCore.SFXCache.Add(filepath, customSfx.CreateInstance());
                customSfx.Play();
            } else if(filepath.EndsWith(".wav"))
            {
                StuxnetAudioCore.Logger.LogWarning("<!> Loading WAVE files is supported by FNA, but not fully supported " +
                    "by Stuxnet. It is recommended to use OGG Vorbis files, instead!");
                SoundEffect customSfx = OS.currentInstance.content.Load<SoundEffect>(filepath);
                StuxnetAudioCore.SFXCache.Add(filepath, customSfx.CreateInstance());
                customSfx.Play();
            } else
            {
                StuxnetAudioCore.Logger.LogError(string.Format("Attempted to load sound file at {0}, but it does not use " +
                    "a recognized extension!", filepath));
                throw new NotSupportedException("Stuxnet.Audio does not support this audio file! Attempted to load " + SoundFile);
            }
        }
    }
}
