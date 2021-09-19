using System;
using System.IO;
using System.Linq;
using Fmod5Sharp.ChunkData;
using NAudio.Wave;

namespace Fmod5Sharp
{
    public static class FmodGcadPcmRebuilder
    {
        private const int BytesPerFrame = 8;
        private const int SamplesPerFrame = 14;
        private const int NibblesPerFrame = 16;

        private static short[] GetPcmData(FmodSample sample)
        {
            //Constants for this sample
            var sampleCount = ByteCountToSampleCount(sample.SampleBytes.Length);
            var frameCount = Math.Ceiling((double)sampleCount / SamplesPerFrame);

            //Result array
            var pcmData = new short[sampleCount];

            //Read the data we need from the stream
            var adpcm = sample.SampleBytes;
            var coeffChunk = (DspCoefficientsBlockData)sample.Metadata.Chunks.First(c => c.ChunkType == FmodSampleChunkType.DSPCOEFF).ChunkData;
            var coeffs = coeffChunk.ChannelData[0];

            //Initialize indices
            var currentSample = 0;
            var outIndex = 0;
            var inIndex = 0;

            //History values - current value is based on previous ones
            short hist1 = 0;
            short hist2 = 0;

            for (var i = 0; i < frameCount; i++)
            {
                //Each byte is a scale and a predictor
                var combined = adpcm[inIndex++];
                var scale = 1 << (combined & 0xF);
                var predictor = combined >> 4;

                //Coefficients are based on the predictor value
                var coeff1 = coeffs[predictor * 2];
                var coeff2 = coeffs[predictor * 2 + 1];

                //Either read 14 - all the samples in this frame - or however many are left, if this is a partial frame
                var samplesToRead = Math.Min(SamplesPerFrame, sampleCount - currentSample);

                for (var s = 0; s < samplesToRead; s++)
                {
                    //Raw value
                    var adpcmSample = (int) (s % 2 == 0 ? GetHighNibbleSigned(adpcm[inIndex]) : GetLowNibbleSigned(adpcm[inIndex++]));

                    //Adaptive processing
                    adpcmSample = (adpcmSample * scale) << 11;
                    adpcmSample = (adpcmSample + 1024 + coeff1 * hist1 + coeff2 * hist2) >> 11;
                    var clampedSample = Clamp16(adpcmSample);

                    //Bump history along
                    hist2 = hist1;
                    hist1 = clampedSample;

                    //Set result
                    pcmData[outIndex++] = clampedSample;

                    //Move to next sample
                    currentSample++;
                }
            }

            return pcmData;
        }

        public static byte[] Rebuild(FmodSample sample)
        {
            var numChannels = sample.Metadata.IsStereo ? 2 : 1;
            var format = WaveFormat.CreateCustomFormat(
                WaveFormatEncoding.Pcm,
                sample.Metadata.Frequency,
                numChannels,
                sample.Metadata.Frequency * numChannels * 2,
                numChannels * 2,
                16
            );
            using var stream = new MemoryStream();
            using var writer = new WaveFileWriter(stream, format);

            var pcmShorts = GetPcmData(sample);
            
            writer.WriteSamples(pcmShorts, 0, pcmShorts.Length);

            return stream.GetBuffer();
        }

        private static int NibbleCountToSampleCount(int nibbleCount)
        {
            var frames = nibbleCount / NibblesPerFrame;
            var extraNibbles = nibbleCount % NibblesPerFrame;
            var extraSamples = extraNibbles < 2 ? 0 : extraNibbles - 2;

            return SamplesPerFrame * frames + extraSamples;
        }

        private static int ByteCountToSampleCount(int byteCount) => NibbleCountToSampleCount(byteCount * 2);

        private static readonly sbyte[] SignedNibbles = { 0, 1, 2, 3, 4, 5, 6, 7, -8, -7, -6, -5, -4, -3, -2, -1 };
        private static sbyte GetHighNibbleSigned(byte value) => SignedNibbles[(value >> 4) & 0xF];
        private static sbyte GetLowNibbleSigned(byte value) => SignedNibbles[value & 0xF];

        private static short Clamp16(int value)
        {
            if (value > short.MaxValue)
                return short.MaxValue;
            if (value < short.MinValue)
                return short.MinValue;
            return (short)value;
        }
    }
}