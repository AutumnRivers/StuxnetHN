using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using NVorbis;
using System;
using System.IO;
using System.Text;

namespace StuxnetHN.Audio
{
    public static class OGGSoundEffectLoader
    {
        public static SoundEffect LoadOgg(string filePath)
        {
#if DEBUG
            Console.WriteLine(string.Format("SASS.OGG: Attempting to load OGG at path: {0}", filePath));
#endif

            if(!IsOggVorbis(filePath))
            {
                throw new FileLoadException(string.Format("The OGG file at {0} could not be loaded, as it does not " +
                    "use OGG Vorbis as its audio container.", filePath));
            }

            using (var vorbis = new VorbisReader(filePath))
            {
                int channels = vorbis.Channels;
                int sampleRate = vorbis.SampleRate;

                // Read all samples
                float[] floatBuffer = new float[vorbis.TotalSamples * channels];
                int samplesRead = vorbis.ReadSamples(floatBuffer, 0, floatBuffer.Length);

                // Convert float (-1..1) to 16-bit PCM
                byte[] byteBuffer = new byte[samplesRead * 2];
                int byteIndex = 0;
                for (int i = 0; i < samplesRead; i++)
                {
                    short val = (short)MathHelper.Clamp(floatBuffer[i] * short.MaxValue, short.MinValue, short.MaxValue);
                    byteBuffer[byteIndex++] = (byte)(val & 0xFF);
                    byteBuffer[byteIndex++] = (byte)((val >> 8) & 0xFF);
                }

                // Create SoundEffect from raw PCM
                return new SoundEffect(byteBuffer, sampleRate, (AudioChannels)channels);
            }
        }

        // Checks if the given .ogg file uses the Vorbis codec
        public static bool IsOggVorbis(string filePath)
        {
            if (!File.Exists(filePath))
                return false;

            using (var fs = File.OpenRead(filePath))
            {
                var header = new byte[30];
                if (fs.Read(header, 0, header.Length) != header.Length)
                    return false;

                // Check for 'OggS' capture pattern at start
                if (!(header[0] == 'O' && header[1] == 'g' && header[2] == 'g' && header[3] == 'S'))
                    return false;

                // Vorbis packets start with 0x01 + "vorbis" ASCII
                // This is usually at offset 29 (start of first packet payload)
                const string vorbisSignature = "\x01vorbis";

                fs.Seek(29, SeekOrigin.Begin);
                var vorbisHeader = new byte[7];
                if (fs.Read(vorbisHeader, 0, vorbisHeader.Length) != vorbisHeader.Length)
                    return false;

                string vorbisStr = Encoding.ASCII.GetString(vorbisHeader);
                return vorbisStr == vorbisSignature;
            }
        }
    }
}
