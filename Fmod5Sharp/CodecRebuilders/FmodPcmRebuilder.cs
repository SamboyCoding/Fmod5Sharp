using System.IO;
using Fmod5Sharp.FmodTypes;
using NAudio.Wave;

namespace Fmod5Sharp.CodecRebuilders
{
    public static class FmodPcmRebuilder
    {
        public static byte[] Rebuild(FmodSample sample, FmodAudioType type)
        {
            var width = type switch
            {
                FmodAudioType.PCM8 => 1,
                FmodAudioType.PCM16 => 2,
                FmodAudioType.PCM32 => 4,
                _ => throw new($"FmodPcmRebuilder does not support encoding of type {type}"),
            };

            var numChannels = sample.Metadata.IsStereo ? 2 : 1;
            var format = WaveFormat.CreateCustomFormat(
                WaveFormatEncoding.Pcm,
                sample.Metadata.Frequency,
                numChannels,
                sample.Metadata.Frequency * numChannels * width,
                numChannels * width,
                width * 8
            );
            using var stream = new MemoryStream();
            using var writer = new WaveFileWriter(stream, format);
            
            writer.Write(sample.SampleBytes, 0, sample.SampleBytes.Length);

            return stream.GetBuffer();
        }
    }
}