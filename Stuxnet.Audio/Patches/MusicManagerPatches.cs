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
using Stuxnet_HN.Executables;
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

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MusicManager), "getVolume")]
        public static bool ReplaceMusicManagerGetVolume(float __result)
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

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MediaPlayer), "Play", new Type[] { typeof(Song) })]
        public static bool ReplaceMediaPlayerPlay()
        {
            if (!ReplaceManager) return true;
            if (string.IsNullOrWhiteSpace(MusicManager.currentSongName)) return true;
            if (IsBaseGameSong)
            {
                StuxnetMusicManager.StopSong();
                return true;
            }

            if(!MusicManager.currentSongName.EndsWith(CurrentSongEntry.path))
            {
                StuxnetMusicManager.CurrentSongEntry = null;
            }

            if (StuxnetMusicManager.CurrentSongEntry == null)
            {
                StuxnetMusicManager.PlaySong(MusicManager.currentSongName);
                return false;
            }

            if (MusicManager.currentSongName.EndsWith(CurrentSongEntry.path))
            {
                StuxnetMusicManager.LoopBegin = CurrentSongEntry.BeginLoop;
                StuxnetMusicManager.LoopEnd = CurrentSongEntry.EndLoop;
            }

            MediaPlayer.Stop();
            StuxnetMusicManager.PlaySong(MusicManager.currentSongName);

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

        private static readonly FieldInfo SampListField = AccessTools.Field(typeof(VisualizationData), "sampList");
        private static readonly FieldInfo FreqListField = AccessTools.Field(typeof(VisualizationData), "freqList");

        [HarmonyPrefix]
        [HarmonyPatch(typeof(VisualizationData), "CalculateData")]
        public static bool ReplaceVisDataForVisualizer(VisualizationData __instance)
        {
            if (!ReplaceManager) return true;
            if (IsBaseGameSong || !StuxnetMusicManager.PlayerHasMusic) return true;

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
                return !(MediaPlayer.State == MediaState.Playing ||
                       (StuxnetMusicManager.PlayerHasMusic && StuxnetMusicManager.Playing));
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
            if (!StuxnetMusicManager.PlayerHasMusic || !ReplaceManager) return;

            StuxnetMusicManager.Player.UpdateVisualizer(updateEvent.GameTime);
        }
    }
}
