using BepInEx;
using BepInEx.Logging;
using Hacknet;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using NVorbis;
using Stuxnet_HN;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace StuxnetHN.Audio.Replacements
{
    public static class StuxnetMusicManager
    {
        public static SongEntry CurrentSongEntry { get; set; }

        public static float FadeTime => MusicManager.FADE_TIME;

        public static int LoopBegin { get; set; } = -1;
        public static int LoopEnd { get; set; } = -1;

        public static bool IsLoaded { get; set; } = false;
        public static bool PlayerHasMusic
        {
            get
            {
                return (bool)!Player?.loadedFilePath.IsNullOrWhiteSpace();
            }
        }
        public static bool Playing
        {
            get
            {
                return (bool)Player?.IsPlaying;
            }
        }

        public static float Volume
        {
            get { return Player.Volume; }
            set { Player.Volume = value; }
        }

        public static bool IsMuted
        {
            get { return Player.IsMuted; }
            set { Player.IsMuted = value; }
        }

        public static OggMusicPlayer Player { get; private set; }

        public static void Initialize()
        {
            if (!StuxnetCore.Configuration.Audio.ReplaceMusicManager)
            {
                StuxnetAudioCore.Logger.LogWarning("You (or the extension developer) have chosen not to " +
                    "replace Hacknet's built-in MusicManager in the extensions's stuxnet_config.json.\n" +
                    "Any dynamic loops that were added to music tracks will be completely ignored.");
                return;
            }

            var cachedVolume = MediaPlayer.Volume;
            Player = new();
            Player.Volume = cachedVolume;
            IsLoaded = true;
        }

        public static void OnUnload()
        {
            if (Player == null)
            {
                StuxnetAudioCore.Logger.LogDebug("[!?] Nothing to unload for StuxnetMusicManager.");
                return;
            }

            IsLoaded = false;
            Player = null;
        }

        public static async void PlaySong(string filePath)
        {
            Player.Stop();
            MediaPlayer.Stop();
            if(!filePath.EndsWith(".ogg"))
            {
                filePath += ".ogg";
            }
            if(filePath.StartsWith("../"))
            {
                filePath = filePath.Substring("../".Length);
            }
            if(Player.loadedFilePath != filePath)
            {
                await Player.LoadAsync(filePath, LoopBegin, LoopEnd);
            }

            if(OS.DEBUG_COMMANDS && StuxnetCore.Configuration.ShowDebugText)
            {
                StuxnetAudioCore.Logger.LogDebug("Playing song with SMM: " + filePath);
            }
            Player.Play();
        }

        public static void StopSong()
        {
            if (Player == null) return;
            if(Player.IsPlaying)
            {
                Player.Stop();
            }
        }
    }

    public class OggMusicPlayer
    {
        private readonly DynamicSoundEffectInstance instance;
        //private VorbisReader reader;
        private float[] floatBuffer;
        //private byte[] byteBuffer;

        internal string loadedFilePath;

        // This will hold the entire song's audio data.
        private float[] fullAudioBuffer;

        private const int BUFFER_MS = 250; // ~0.25s buffer
        private int samplesPerBuffer;

        public bool IsPlaying => instance.State == SoundState.Playing;
        public bool IsLooping { get; set; } = true;

        public TimeSpan LoopStart { get; set; } = TimeSpan.Zero;

        public TimeSpan LoopEnd { get; set; }

        public float Volume
        {
            get { return instance == null ? -1.0f : instance.Volume; }
            set { if(instance != null) { instance.Volume = value; } }
        }

        public bool IsMuted { get; set; } = false;

        private CircularBuffer<float> visualizerBuffer;
        private const int VISUALIZER_BUFFER_SIZE = 512;
        public float[] TargetVisualizerData { get; private set; }

        private static readonly LruCache<string, CachedSongData> SongCache = new(5);

        private CachedSongData CurrentSongData;

        private float visualizerHead;

        public OggMusicPlayer()
        {
            instance = new DynamicSoundEffectInstance(44100, AudioChannels.Stereo);
            instance.BufferNeeded += OnBufferNeededRewrite;
            visualizerBuffer = new CircularBuffer<float>(VISUALIZER_BUFFER_SIZE);
            TargetVisualizerData = new float[256];
        }

        public async Task PreloadAsync(string filePath)
        {
            if (SongCache.ContainsKey(filePath)) return;

            filePath = Utils.GetFileLoadPrefix() + filePath;
            if(OS.DEBUG_COMMANDS && StuxnetCore.Configuration.ShowDebugText)
            {
                StuxnetAudioCore.Logger.LogDebug(
                    string.Format("Preloading song file at path {0}", filePath)
                    );
            }
            if(!System.IO.File.Exists(filePath))
            {
                StuxnetAudioCore.Logger.LogError(
                    string.Format("File at {0} doesn't exist - can't preload", filePath)
                    );
                return;
            }

            VorbisReader tempReader;
            await Task.Run(() =>
            {
                tempReader = new VorbisReader(filePath);
                long totalSamples = tempReader.TotalSamples * tempReader.Channels;
                float[] fullBuffer = new float[totalSamples];
                tempReader.ReadSamples(fullBuffer, 0, (int)totalSamples);
                CacheSongData(filePath, fullBuffer, tempReader);
            });
        }

        private void CacheSongData(string filePath, float[] audioBuffer, VorbisReader reader)
        {
            CachedSongData songData = new(audioBuffer, reader.SampleRate, reader.TotalSamples);
            SongCache.Add(filePath, songData);
        }

        private void CacheSongData(string filePath, CachedSongData cachedSongData)
        {
            SongCache.Add(filePath, cachedSongData);
        }

        public async Task LoadAsync(string filePath, int loopStart = -1, int loopEnd = -1)
        {
            if(OS.DEBUG_COMMANDS && StuxnetCore.Configuration.ShowDebugText)
            {
                StuxnetAudioCore.Logger.LogDebug(
                    string.Format("Loading song with filepath of {0}", filePath));
            }

            if(SongCache.TryGetValue(filePath, out var cachedSongData))
            {
                if(OS.DEBUG_COMMANDS && StuxnetCore.Configuration.ShowDebugText)
                {
                    StuxnetAudioCore.Logger.LogDebug("Loading cached song data");
                }
                CurrentSongData = cachedSongData;
            } else
            {
                await Task.Run(() =>
                {
                    var reader = new VorbisReader(filePath);

                    long totalSamples = reader.TotalSamples * reader.Channels;
                    fullAudioBuffer = new float[totalSamples];

                    reader.ReadSamples(fullAudioBuffer, 0, (int)totalSamples);

                    CurrentSongData = new(fullAudioBuffer, reader.SampleRate, reader.TotalSamples, reader.Channels);
                    CacheSongData(filePath, CurrentSongData);
                });
            }

            samplesPerBuffer = CurrentSongData.SampleRate * CurrentSongData.Channels * BUFFER_MS / 1000;
            floatBuffer = new float[samplesPerBuffer];

            int offset = 0;

            if(StuxnetCore.Configuration.Audio.OffsetLoopPoints)
            {
                offset = BUFFER_MS;
            }

            LoopStart = loopStart > -1 ? TimeSpan.FromMilliseconds(loopStart) : TimeSpan.Zero;
            LoopEnd = loopEnd > -1 ? TimeSpan.FromMilliseconds(loopEnd - offset) : TimeSpan.Zero;

            loadedFilePath = filePath;

            samplesPlayed = 0;

            float[] decoded = CurrentSongData.AudioBuffer;
            pcmData = new byte[decoded.Length * 2];
            for (int i = 0; i < decoded.Length; i++)
            {
                short sample = (short)MathHelper.Clamp(decoded[i] * short.MaxValue, short.MinValue, short.MaxValue);
                pcmData[i * 2] = (byte)(sample & 0xFF);
                pcmData[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
            }

            if (OS.DEBUG_COMMANDS && StuxnetCore.Configuration.ShowDebugText)
            {
                StuxnetAudioCore.Logger.LogDebug(
                    string.Format("SMM: Loaded song at {0}\nLoop Begin: {1}\nLoop End: {2}", filePath,
                    loopStart, loopEnd));
            }
        }

        private int samplesPlayed = 0;
        private byte[] pcmData;
        private int channels => CurrentSongData.Channels;
        private int bytesPerSampleFrame => 2 * channels;

        private bool forceReLoop = false;

        private void OnBufferNeededRewrite(object sender, EventArgs e)
        {
            int sampleFramesToRead = 1024;
            byte[] buffer = new byte[sampleFramesToRead * bytesPerSampleFrame];

            int loopStartFrames = (int)(LoopStart.TotalSeconds * CurrentSongData.SampleRate);
            int loopEndFrames = (LoopEnd == TimeSpan.Zero)
                ? pcmData.Length / bytesPerSampleFrame
                : (int)(LoopEnd.TotalSeconds * CurrentSongData.SampleRate);

            int remainingFrames = loopEndFrames - samplesPlayed;

            if (sampleFramesToRead > remainingFrames || forceReLoop)
            {
                int framesFirstPart = remainingFrames;
                int framesSecondPart = sampleFramesToRead - framesFirstPart;

                CopyPcmFrames(pcmData, samplesPlayed, framesFirstPart, buffer, 0);
                CopyPcmFrames(pcmData, loopStartFrames, framesSecondPart, buffer, framesFirstPart * bytesPerSampleFrame);

                samplesPlayed = loopStartFrames + framesSecondPart;
                forceReLoop = false;

                if(OS.DEBUG_COMMANDS && StuxnetCore.Configuration.ShowDebugText)
                {
                    StuxnetAudioCore.Logger.LogDebug("SMM: Hit loop point, restarting at loop start");
                }
            }
            else
            {
                CopyPcmFrames(pcmData, samplesPlayed, sampleFramesToRead, buffer, 0);
                samplesPlayed += sampleFramesToRead;
            }

            instance.SubmitBuffer(buffer);
        }

        private void CopyPcmFrames(byte[] source, int startFrame, int frameCount, byte[] output, int outputOffset)
        {
            int startByte = startFrame * bytesPerSampleFrame;
            int bytesToCopy = frameCount * bytesPerSampleFrame;

            if (outputOffset + bytesToCopy > output.Length)
                bytesToCopy = output.Length - outputOffset;

            if(bytesToCopy <= 0)
            {
                if(StuxnetCore.Configuration.ShowDebugText)
                {
                    StuxnetAudioCore.Logger.LogWarning(
                        "SMM: bytesToCopy is <= 0. If there are no audio glitches, then you can " +
                        "safely ignore this warning."
                        );
                }
                forceReLoop = true;
                return;
            }

            int srcEnd = startByte + bytesToCopy;
            try
            {
                if (srcEnd <= source.Length)
                {
                    Buffer.BlockCopy(source, startByte, output, outputOffset, bytesToCopy);
                }
                else
                {
                    int firstChunk = source.Length - startByte;
                    Buffer.BlockCopy(source, startByte, output, outputOffset, firstChunk);
                    int secondChunk = bytesToCopy - firstChunk;
                    Buffer.BlockCopy(source, 0, output, outputOffset + firstChunk, secondChunk);
                }
            } catch(Exception e)
            {
                Stop();
                StuxnetAudioCore.Logger.LogError(
                    string.Format("CopyPcmFrames messed up again, please report to Autumn\n{0}",
                    e.ToString())
                    );
                OS.currentInstance.warningFlash();
                OS.currentInstance.write("Issue with Stuxnet.Audio - SMM has been stopped - read terminal and report to Autumn");
            }
        }

        public void UpdateVisualizer(GameTime gameTime)
        {
            float samplesPerFrame = (float)(gameTime.ElapsedGameTime.TotalSeconds * CurrentSongData.SampleRate);
            visualizerHead += samplesPerFrame;

            float totalSamples = CurrentSongData.TotalSamples;
            float loopEndInSamples = LoopEnd != TimeSpan.Zero ?
                (float)LoopEnd.TotalSeconds * CurrentSongData.SampleRate :
                totalSamples;
            int loopStartSamples = (int)(LoopStart.TotalSeconds * CurrentSongData.SampleRate);

            if (visualizerHead >= loopEndInSamples)
            {
                visualizerHead = loopStartSamples;
            }

            visualizerHead = MathHelper.Clamp(visualizerHead, 0, totalSamples - 1);
        }

        public float[] GetVisualizerData(int count)
        {
            float[] visualizerData = new float[count];

            int startIndex = (int)visualizerHead * CurrentSongData.Channels;

            for (int i = 0; i < count; i++)
            {
                int sourceIndex = startIndex + (i * CurrentSongData.Channels);

                if (sourceIndex < CurrentSongData.AudioBuffer.Length)
                {
                    visualizerData[i] = CurrentSongData.AudioBuffer[sourceIndex];
                }
                else
                {
                    visualizerData[i] = 0f;
                }
            }

            return visualizerData;
        }

        public async void Play()
        {
            if (!IsPlaying && instance != null)
            {
                for(int i = 0; i < 5; i++)
                {
                    OnBufferNeededRewrite(null, EventArgs.Empty);
                }
                samplesPlayed = 0;
                await Task.Delay(BUFFER_MS + 25);
                visualizerHead = 0;
                instance.Play();
            }
        }

        public void Pause() => instance.Pause();

        public void Stop()
        {
            if (instance == null) return;

            instance.Stop();
            loadedFilePath = string.Empty;
        }

        public bool HasCachedSong(string songName)
        {
            bool result = SongCache.ContainsKey(songName);
            if(!result)
            {
                result = SongCache.ContainsKey(Utils.GetFileLoadPrefix() + songName);
            }
            return result;
        }
    }

    public class CachedSongData
    {
        public float[] AudioBuffer;
        public int SampleRate;
        public float TotalSamples;
        public int Channels = 2;

        public CachedSongData(float[] audioBuffer, int sampleRate, float totalSamples, int channels = 2)
        {
            AudioBuffer = audioBuffer;
            SampleRate = sampleRate;
            TotalSamples = totalSamples;
            Channels = channels;
        }
    }

    public class CircularBuffer<T>
    {
        private readonly T[] buffer;
        private int head;
        private int tail;
        private int size;

        public CircularBuffer(int capacity)
        {
            buffer = new T[capacity];
            head = 0;
            tail = 0;
            size = 0;
        }

        public int Capacity => buffer.Length;

        public int Size => size;

        public bool IsFull => size == Capacity;

        public bool IsEmpty => size == 0;

        public void Write(T[] data, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                buffer[head] = data[offset + i];
                head = (head + 1) % Capacity;
                if (size < Capacity)
                {
                    size++;
                }
                else
                {
                    tail = (tail + 1) % Capacity;
                }
            }
        }

        public int Read(T[] data, int offset, int count)
        {
            int numToRead = Math.Min(count, size);
            for (int i = 0; i < numToRead; i++)
            {
                data[offset + i] = buffer[tail];
                tail = (tail + 1) % Capacity;
            }
            size -= numToRead;
            return numToRead;
        }

        public void Clear()
        {
            head = 0;
            tail = 0;
            size = 0;
        }

        public int Peek(T[] data, int offset, int count)
        {
            int numToRead = Math.Min(count, size);
            int readHead = tail; // Use a temporary pointer to not mess with the main tail
            for (int i = 0; i < numToRead; i++)
            {
                data[offset + i] = buffer[readHead];
                readHead = (readHead + 1) % Capacity;
            }
            return numToRead;
        }
    }
}
