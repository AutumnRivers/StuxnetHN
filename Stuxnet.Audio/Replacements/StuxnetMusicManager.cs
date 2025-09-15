using BepInEx;
using Hacknet;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using NVorbis;
using Stuxnet_HN;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

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

            Player = new();
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
            Player.Dispose();
        }

        public static async void PlaySong(string filePath)
        {
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
                //Player.Load(filePath, LoopBegin, LoopEnd);
                await Player.LoadAsync(filePath, LoopBegin, LoopEnd);
            }
            Player.Volume = MediaPlayer.Volume;
            Player.Play();

            if(OS.DEBUG_COMMANDS && StuxnetCore.Configuration.ShowDebugText)
            {
                StuxnetAudioCore.Logger.LogDebug("Playing song with SMM: " + filePath);
            }
        }

        public static void StopSong()
        {
            if(Player.IsPlaying)
            {
                Player.Stop();
            }
        }
    }

    public class OggMusicPlayer : IDisposable
    {
        private readonly DynamicSoundEffectInstance instance;
        private VorbisReader reader;
        private float[] floatBuffer;
        private byte[] byteBuffer;

        internal string loadedFilePath;

        // This will hold the entire song's audio data.
        private float[] fullAudioBuffer;

        public TimeSpan Position => reader.TimePosition;
        public TimeSpan Length => reader.TotalTime;

        private const int BUFFER_MS = 250; // ~0.25s buffer
        private int samplesPerBuffer;

        public bool IsPlaying => instance.State == SoundState.Playing;
        public bool IsLooping { get; set; } = true;

        public TimeSpan LoopStart { get; set; } = TimeSpan.Zero;

        public TimeSpan LoopEnd { get; set; }

        public float Volume { get; set; } = 1.0f;

        public bool IsMuted { get; set; } = false;

        private CircularBuffer<float> visualizerBuffer;
        private const int VISUALIZER_BUFFER_SIZE = 512;
        public float[] TargetVisualizerData { get; private set; }

        private static readonly LruCache<string, float[]> AudioCache = new LruCache<string, float[]>(5);

        private float visualizerHead;

        public int SampleRate => reader?.SampleRate ?? 44100;

        public OggMusicPlayer()
        {
            instance = new DynamicSoundEffectInstance(44100, AudioChannels.Stereo);
            instance.BufferNeeded += OnBufferNeeded;
            visualizerBuffer = new CircularBuffer<float>(VISUALIZER_BUFFER_SIZE);
            TargetVisualizerData = new float[256];
        }

        public async Task LoadAsync(string filePath, int loopStart = -1, int loopEnd = -1)
        {
            if (AudioCache.TryGetValue(filePath, out var cachedData))
            {
                fullAudioBuffer = cachedData;

                await Task.Run(() =>
                {
                    reader?.Dispose();
                    reader = new VorbisReader(filePath);
                });
            }
            else
            {
                await Task.Run(() =>
                {
                    reader?.Dispose();
                    reader = new VorbisReader(filePath);

                    long totalSamples = reader.TotalSamples * reader.Channels;
                    fullAudioBuffer = new float[totalSamples];

                    reader.ReadSamples(fullAudioBuffer, 0, (int)totalSamples);

                    AudioCache.Add(filePath, fullAudioBuffer);
                });
            }

            reader.TimePosition = TimeSpan.Zero;
            samplesPerBuffer = reader.SampleRate * reader.Channels * BUFFER_MS / 1000;
            floatBuffer = new float[samplesPerBuffer];
            byteBuffer = new byte[samplesPerBuffer * sizeof(short)];

            LoopStart = loopStart > -1 ? TimeSpan.FromMilliseconds(loopStart) : TimeSpan.Zero;
            LoopEnd = loopEnd > -1 ? TimeSpan.FromMilliseconds(loopEnd) : reader.TotalTime;
            loadedFilePath = filePath;
        }

        private void OnBufferNeeded(object sender, EventArgs e)
        {
            int samplesRead = reader.ReadSamples(floatBuffer, 0, floatBuffer.Length);

            if (samplesRead == 0 || reader.TimePosition >= LoopEnd)
            {
                if (IsLooping)
                {
                    reader.TimePosition = LoopStart;
                    samplesRead = reader.ReadSamples(floatBuffer, 0, floatBuffer.Length);
                    if(OS.DEBUG_COMMANDS && StuxnetCore.Configuration.ShowDebugText)
                    {
                        StuxnetAudioCore.Logger.LogDebug("SMM hit loop point - restarting");
                    }
                }
            }

            if (samplesRead > 0)
            {
                visualizerBuffer.Write(floatBuffer, 0, samplesRead);

                // Directly update the target visualizer data with the new samples.
                float[] newSamples = GetVisualizerData(256);

                Array.Copy(newSamples, TargetVisualizerData, newSamples.Length);

                float gain = IsMuted ? 0.0f : Volume;
                int ch = reader.Channels;

                for (int i = 0; i < samplesRead; i++)
                {
                    float scaled = floatBuffer[i] * gain;

                    short sample = (short)MathHelper.Clamp(
                        scaled * short.MaxValue,
                        short.MinValue,
                        short.MaxValue
                    );

                    byteBuffer[i * 2] = (byte)(sample & 0xFF);
                    byteBuffer[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
                }

                instance.SubmitBuffer(byteBuffer, 0, samplesRead * sizeof(short));
            }
        }

        public void UpdateVisualizer(GameTime gameTime)
        {
            float samplesPerFrame = (float)(gameTime.ElapsedGameTime.TotalSeconds * reader.SampleRate);
            visualizerHead += samplesPerFrame;

            float loopEndInSamples = (float)LoopEnd.TotalSeconds * reader.SampleRate;

            if (visualizerHead >= loopEndInSamples)
            {
                visualizerHead = reader.SamplePosition;
            }
        }

        public float[] GetVisualizerData(int count)
        {
            float[] visualizerData = new float[count];

            int startIndex = (int)visualizerHead * reader.Channels;

            for (int i = 0; i < count; i++)
            {
                int sourceIndex = startIndex + (i * reader.Channels);

                if (sourceIndex < fullAudioBuffer.Length)
                {
                    visualizerData[i] = fullAudioBuffer[sourceIndex];
                }
                else
                {
                    visualizerData[i] = 0f;
                }
            }

            return visualizerData;
        }

        public void Play()
        {
            if (!IsPlaying && instance != null)
            {
                visualizerHead = 0;
                OnBufferNeeded(null, EventArgs.Empty);
                OnBufferNeeded(null, EventArgs.Empty);
                instance.Play();
            }
        }

        public void Pause() => instance.Pause();

        public void Stop()
        {
            if (instance == null || reader == null) return;

            instance.Stop();
            reader.TimePosition = TimeSpan.Zero;
        }

        public void Seek(TimeSpan position)
        {
            reader.TimePosition = position;
        }

        public void Dispose()
        {
            instance?.Dispose();
            reader?.Dispose();
        }
    }

    public class CircularBuffer<T>
    {
        private T[] buffer;
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
