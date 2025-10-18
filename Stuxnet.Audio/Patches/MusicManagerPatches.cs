using BepInEx;
using Hacknet;
using Hacknet.Effects;
using Hacknet.Extensions;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Media;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Pathfinder.Event.Gameplay;
using Stuxnet_HN;
using StuxnetHN.Audio.Replacements;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace StuxnetHN.Audio.Patches
{
    [HarmonyPatch]
    public class MusicManagerPatches
    {
        public static bool ReplaceManager => StuxnetCore.Configuration.Audio.ReplaceMusicManager;

        public static SongEntry CurrentSongEntry => StuxnetMusicManager.CurrentSongEntry;

        public static bool IsBaseGameSong => 
            !MusicManager.currentSongName.Contains(ExtensionLoader.ActiveExtensionInfo.FolderPath);

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MusicManager), "playSong")]
        public static bool ReplaceMusicManagerPlaySong()
        {
            lastTransitionedSong = string.Empty;
            if (!ReplaceManager) return true;
            if (IsBaseGameSong) return true;

            if(OS.DEBUG_COMMANDS)
            {
                StuxnetAudioCore.Logger.LogDebug(
                    string.Format("Intercepted playSong with value of {0}",
                    MusicManager.currentSongName)
                    );
            }

            if(StuxnetMusicManager.CurrentSongEntry == null)
            {
                StuxnetMusicManager.PlaySong(MusicManager.currentSongName);
                return false;
            }

            if(MusicManager.currentSongName.EndsWith(CurrentSongEntry.path))
            {
                StuxnetMusicManager.LoopBegin = CurrentSongEntry.BeginLoop;
                StuxnetMusicManager.LoopEnd = CurrentSongEntry.EndLoop;
            }

            MediaPlayer.Stop();
            StuxnetMusicManager.PlaySong(MusicManager.currentSongName);

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MusicManager), "toggleMute")]
        public static bool ReplaceMusicManagerToggleMute()
        {
            if (!ReplaceManager) return true;
            if (IsBaseGameSong) return true;

            MusicManager.isMuted = !MusicManager.isMuted;
            StuxnetMusicManager.IsMuted = MusicManager.isMuted;

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MusicManager), "setIsMuted")]
        public static bool ReplaceMusicManagerSetIsMuted(bool muted)
        {
            if (!ReplaceManager) return true;
            if (IsBaseGameSong) return true;

            MusicManager.isMuted = muted;
            StuxnetMusicManager.IsMuted = MusicManager.isMuted;

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MusicManager), "stop")]
        public static bool ReplaceMusicManagerStop()
        {
            if (!ReplaceManager) return true;
            if (IsBaseGameSong) return true;

            StuxnetMusicManager.StopSong();
            MediaPlayer.Stop();

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MusicManager), "getVolume")]
        public static bool ReplaceMusicManagerGetVolume(ref float __result)
        {
            if (!ReplaceManager) return true;
            if (IsBaseGameSong) return true;

            __result = StuxnetMusicManager.Volume;

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MusicManager), "setVolume")]
        public static bool ReplaceMusicManagerSetVolume(float volume)
        {
            if (!ReplaceManager) return true;
            if (IsBaseGameSong) return true;

            StuxnetMusicManager.Volume = volume;

            return true;
        }

        private static string lastTransitionedSong = string.Empty;

        internal static bool ActivatedFromAction = false;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MusicManager), nameof(MusicManager.transitionToSong))]
        public static void CacheLastTransitioned(string songName)
        {
            lastTransitionedSong = songName;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MediaPlayer), "Play", new Type[] { typeof(Song) })]
        public static bool ReplaceMediaPlayerPlay(Song song)
        {
            if (!ReplaceManager) return true;
            if (song == null) return true;
            if (string.IsNullOrWhiteSpace(song.Name)) return true;
            if(OS.DEBUG_COMMANDS && StuxnetCore.Configuration.ShowDebugText)
            {
                StuxnetAudioCore.Logger.LogDebug(
                    string.Format("Intercepted MediaPlayer.Play with value of {0} (lts:{1})",
                    song.Name, lastTransitionedSong)
                    );
            }
            if (lastTransitionedSong.IsNullOrWhiteSpace())
            {
                StuxnetMusicManager.StopSong();
                return true;
            } else if(!lastTransitionedSong.Contains(song.Name))
            {
                StuxnetMusicManager.StopSong();
                lastTransitionedSong = string.Empty;
                return true;
            }

            if (StuxnetMusicManager.CurrentSongEntry == null)
            {
                if(!ActivatedFromAction)
                {
                    StuxnetMusicManager.LoopBegin = -1;
                    StuxnetMusicManager.LoopEnd = -1;
                } else
                {
                    ActivatedFromAction = false;
                }

                StuxnetMusicManager.PlaySong(lastTransitionedSong);
                lastTransitionedSong = string.Empty;
                return false;
            }

            if (!CurrentSongEntry.path.Contains(song.Name))
            {
                StuxnetMusicManager.CurrentSongEntry = null;
            }

            if (CurrentSongEntry != null && lastTransitionedSong.EndsWith(CurrentSongEntry.path))
            {
                StuxnetMusicManager.LoopBegin = CurrentSongEntry.BeginLoop;
                StuxnetMusicManager.LoopEnd = CurrentSongEntry.EndLoop;
            } else
            {
                StuxnetMusicManager.LoopBegin = -1;
                StuxnetMusicManager.LoopEnd = -1;
            }

            MediaPlayer.Stop();
            StuxnetMusicManager.PlaySong(lastTransitionedSong);
            lastTransitionedSong = string.Empty;

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MediaPlayer), "Volume", MethodType.Setter)]
        public static void SetSMMVolume(float value)
        {
            if (!ReplaceManager) return;
            if (IsBaseGameSong || !StuxnetMusicManager.PlayerHasMusic) return;

            StuxnetMusicManager.Volume = value;

            if(value <= 0.1)
            {
                MediaPlayer.Stop();
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(OS), "LoadContent")]
        public static void PreloadStartingSong()
        {
            if (!ReplaceManager || StuxnetMusicManager.Player == null) return;
            var ext = ExtensionLoader.ActiveExtensionInfo;
            if (!ext.IntroStartupSong.EndsWith(".ogg")) return;
            StuxnetMusicManager.Player.PreloadAsync(ext.IntroStartupSong);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(OS), "quitGame")]
        public static void ResetSMMOnQuit()
        {
            if (!ReplaceManager || StuxnetMusicManager.Player == null) return;
            StuxnetMusicManager.StopSong();
            StuxnetMusicManager.OnUnload();
        }

        private static readonly FieldInfo SampListField = AccessTools.Field(typeof(VisualizationData), "sampList");
        private static readonly FieldInfo FreqListField = AccessTools.Field(typeof(VisualizationData), "freqList");

        [HarmonyPrefix]
        [HarmonyPatch(typeof(VisualizationData), "CalculateData")]
        public static bool ReplaceVisDataForVisualizer(VisualizationData __instance)
        {
            if (!ReplaceManager) return true;
            if (IsBaseGameSong || !StuxnetMusicManager.Playing) return true;

            List<float> sampList = (List<float>)SampListField.GetValue(__instance);
            List<float> freqList = (List<float>)FreqListField.GetValue(__instance);

            const int visualizerSize = 256;

            float[] rawSamples = StuxnetMusicManager.Player.GetVisualizerData(visualizerSize);

            for (int i = 0; i < visualizerSize; i++)
            {
                sampList[i] = rawSamples[i];
            }

            for (int i = 0; i < visualizerSize; i++)
            {
                freqList[i] = 0f;
            }

            return false;
        }

        [HarmonyILManipulator]
        [HarmonyPatch(typeof(AudioVisualizer), "Draw")]
        public static void LoadDataIfSMMIsPlaying(ILContext il)
        {
            if (!ReplaceManager) return;

            MethodBase getStateFunc = typeof(MediaPlayer)
                .GetMethod("get_State");

            var body = il.Body;
            var instrs = body.Instructions;

            var c = new ILCursor(il);
            c.GotoNext(MoveType.Before, i => i.MatchCall(getStateFunc));
            var callInstr = c.Next;

            var stlocInstr = callInstr;
            while (stlocInstr != null && !IsStloc(stlocInstr.OpCode))
                stlocInstr = stlocInstr.Next;

            if (stlocInstr == null)
            {
                Console.WriteLine("Couldn't find stloc after get_State — aborting manipulator.");
                return;
            }

            var iter = callInstr;
            while (iter != null && iter != stlocInstr)
            {
                iter.OpCode = OpCodes.Nop;
                iter.Operand = null;
                iter = iter.Next;
            }

            var insertCursor = new ILCursor(il);
            insertCursor.Goto(stlocInstr);

            insertCursor.EmitDelegate<Func<bool>>(() =>
            {
                if (!ReplaceManager || StuxnetMusicManager.Player == null) return MediaPlayer.State != MediaState.Playing;
                return !(MediaPlayer.State == MediaState.Playing || StuxnetMusicManager.Playing);
            });
        }

        private static bool IsStloc(OpCode op)
        {
            return op == OpCodes.Stloc
                || op == OpCodes.Stloc_0
                || op == OpCodes.Stloc_1
                || op == OpCodes.Stloc_2
                || op == OpCodes.Stloc_3
                || op == OpCodes.Stloc_S;
        }

        [Pathfinder.Meta.Load.Event()]
        public static void UpdateVisualizerData(OSUpdateEvent updateEvent)
        {
            if (!ReplaceManager || !StuxnetMusicManager.Playing) return;

            StuxnetMusicManager.Player.UpdateVisualizer(updateEvent.GameTime);
        }
    }
}
