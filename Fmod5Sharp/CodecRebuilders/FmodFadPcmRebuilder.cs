using Fmod5Sharp.FmodTypes;
using Fmod5Sharp.Util;
using NAudio.Wave;
using System;
using System.Buffers.Binary;
using System.IO;

namespace Fmod5Sharp.CodecRebuilders;

// Credits: https://github.com/vgmstream/vgmstream/blob/master/src/coding/fadpcm_decoder.c
public class FmodFadPcmRebuilder
{
    private static readonly short[,] FadpcmCoefs = {
        { 0, 0 },
        { 60, 0 },
        { 122, 60 },
        { 115, 52 },
        { 98, 55 },
        { 0, 0 },
        { 0, 0 },
        { 0, 0 }
    };

    public static short[] DecodeFadpcm(FmodSample sample)
    {
        const int FrameSize = 0x8C;
        const int SamplesPerFrame = (FrameSize - 0x0C) * 2;

        ReadOnlySpan<byte> sampleBytes = sample.SampleBytes;
        int numChannels = sample.Metadata.NumChannels;
        int totalFrames = sampleBytes.Length / FrameSize;

        // Total samples across all channels
        short[] outputBuffer = new short[totalFrames * SamplesPerFrame];
        Span<short> outputSpan = outputBuffer;

        int[] hist1 = new int[numChannels];
        int[] hist2 = new int[numChannels];
        for (int f = 0; f < totalFrames; f++)
        {
            int channel = f % numChannels;
            int frameOffset = f * FrameSize;

            ReadOnlySpan<byte> frameSpan = sampleBytes.Slice(frameOffset, FrameSize);

            // Parse Header
            uint coefsLookup = BinaryPrimitives.ReadUInt32LittleEndian(frameSpan[..4]);
            uint shiftsLookup = BinaryPrimitives.ReadUInt32LittleEndian(frameSpan[0x04..]);
            hist1[channel] = BinaryPrimitives.ReadInt16LittleEndian(frameSpan[0x08..]);
            hist2[channel] = BinaryPrimitives.ReadInt16LittleEndian(frameSpan[0x0A..]);

            int frameIndexInChannel = f / numChannels;
            int frameBaseOutIndex = (frameIndexInChannel * SamplesPerFrame * numChannels) + channel;

            // Decode nibbles, grouped in 8 sets of 0x10 * 0x04 * 2
            for (int i = 0; i < 8; i++)
            {
                // Each set has its own coefs/shifts (indexes > 7 are repeat, ex. 0x9 is 0x2)
                int index = (int)((coefsLookup >> (i * 4)) & 0x0F) % 0x07;
                int shift = (int)((shiftsLookup >> (i * 4)) & 0x0F);

                int coef1 = FadpcmCoefs[index, 0];
                int coef2 = FadpcmCoefs[index, 1];
                int finalShift = 22 - shift; // Pre-adjust for 32b sign extend

                for (int j = 0; j < 4; j++)
                {
                    uint nibbles = BinaryPrimitives.ReadUInt32LittleEndian(frameSpan[(0x0C + (0x10 * i) + (0x04 * j))..]);

                    for (int k = 0; k < 8; k++)
                    {
                        int sampleValue = (int)((nibbles >> (k * 4)) & 0x0F);
                        sampleValue = (sampleValue << 28) >> finalShift; // 32b sign extend + scale
                        sampleValue = (sampleValue - (hist2[channel] * coef2) + (hist1[channel] * coef1)) >> 6;

                        short finalSample = Utils.ClampToShort(sampleValue);

                        int outIndex = frameBaseOutIndex + ((i * 32 + j * 8 + k) * numChannels);

                        if (outIndex < outputSpan.Length)
                            outputSpan[outIndex] = finalSample;

                        hist2[channel] = hist1[channel];
                        hist1[channel] = finalSample;
                    }
                }
            }
        }

        return outputBuffer;
    }

    public static byte[] Rebuild(FmodSample sample)
    {
        var format = new WaveFormat(sample.Metadata.Frequency, 16, sample.Metadata.NumChannels);

        using var stream = new MemoryStream();
        using (var writer = new WaveFileWriter(stream, format))
        {
            short[] pcmSamples = DecodeFadpcm(sample);
            writer.WriteSamples(pcmSamples, 0, pcmSamples.Length);
        }

        return stream.ToArray();
    }
}