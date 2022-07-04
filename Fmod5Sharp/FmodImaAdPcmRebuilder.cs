using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;

namespace Fmod5Sharp
{
    public static class FmodImaAdPcmRebuilder
    {
        public const int SamplesPerFramePerChannel = 64;
        
        static readonly int[] ADPCMTable = {
            7, 8, 9, 10, 11, 12, 13, 14,
            16, 17, 19, 21, 23, 25, 28, 31,
            34, 37, 41, 45, 50, 55, 60, 66,
            73, 80, 88, 97, 107, 118, 130, 143,
            157, 173, 190, 209, 230, 253, 279, 307,
            337, 371, 408, 449, 494, 544, 598, 658,
            724, 796, 876, 963, 1060, 1166, 1282, 1411,
            1552, 1707, 1878, 2066, 2272, 2499, 2749, 3024,
            3327, 3660, 4026, 4428, 4871, 5358, 5894, 6484,
            7132, 7845, 8630, 9493, 10442, 11487, 12635, 13899,
            15289, 16818, 18500, 20350, 22385, 24623, 27086, 29794,
            32767
        };

        private static readonly int[] IMA_IndexTable = {
            -1, -1, -1, -1, 2, 4, 6, 8,
            -1, -1, -1, -1, 2, 4, 6, 8,
        };
        
        private static void ExpandNibble(MemoryStream stream, long byteOffset, int nibbleShift, ref int hist, ref int stepIndex)
        {
            stream.Seek(byteOffset, SeekOrigin.Begin);
            var sampleNibble = (stream.ReadByte() >> nibbleShift) & 0xf;
            var sampleDecoded = hist;
            var step = ADPCMTable[stepIndex];

            var delta = step >> 3;
            if ((sampleNibble & 1) != 0) delta += step >> 2;
            if ((sampleNibble & 2) != 0) delta += step >> 1;
            if ((sampleNibble & 4) != 0) delta += step;
            if ((sampleNibble & 8) != 0) delta = -delta;
            sampleDecoded += delta;

            hist = Utils.Clamp((short)sampleDecoded, short.MinValue, short.MaxValue);
            stepIndex += IMA_IndexTable[sampleNibble];
            stepIndex = Utils.Clamp((short)stepIndex, 0, 88);
        }

        private static short[] GetPcm(FmodSample sample)
        {
            var blockSamples = 0x40;
            var numChannels = (int)sample.Metadata.Channels;
            
            using var stream = new MemoryStream(sample.SampleBytes);
            using var reader = new BinaryReader(stream);

            short[] ret = new short[sample.Metadata.SampleCount * 2];
            var sampleIndex = 0;
            
            for (var channel = 0; channel < numChannels; channel++)
            {
                sampleIndex = channel;
                
                var numFrames = (int) sample.Metadata.SampleCount / SamplesPerFramePerChannel;

                for (var frameNum = 0; frameNum < numFrames; frameNum++)
                {
                    var frameOffset = 0x24 * numChannels * frameNum;
                    
                    //Read header
                    var headerIndex = frameOffset + 4 * channel;

                    stream.Seek(headerIndex, SeekOrigin.Begin);
                    int hist = reader.ReadInt16();
                    stream.Seek(headerIndex + 2, SeekOrigin.Begin);
                    int stepIndex = reader.ReadByte();

                    stepIndex = Utils.Clamp((short)stepIndex, 0, 88);
                    ret[sampleIndex] = (short)hist;
                    sampleIndex += numChannels;

                    for (var sampleNum = 1; sampleNum <= SamplesPerFramePerChannel; sampleNum++)
                    {
                        // var byteOffset = relativePos + 4 * numChannels + 2 * channel + (i - 1) / 4 * 2 * numChannels + ((i - 1) % 4) / 2;
                        var byteOffset = frameOffset + 4 * 2 + 4 * (channel % 2) + 4 * 2 * ((sampleNum - 1) / 8) + ((sampleNum - 1) % 8) / 2;
                        if (numChannels == 0)
                            byteOffset = frameOffset + 4 + (sampleNum - 1) / 2;

                        var nibbleShift = ((sampleNum - 1) & 1) != 0 ? 4 : 0;

                        if (sampleNum < blockSamples)
                        {
                            ExpandNibble(stream, byteOffset, nibbleShift, ref hist, ref stepIndex);
                            ret[sampleIndex] = ((short)hist);
                            sampleIndex += numChannels;
                        }
                        else
                        {
                            // relativePos += 0x24 * numChannels;
                        }
                    }
                }
            }

            return ret;
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

            var pcmShorts = GetPcm(sample);
            
            writer.WriteSamples(pcmShorts, 0, pcmShorts.Length);

            return stream.ToArray();
        }
    }
}