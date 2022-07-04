using System.IO;
using Fmod5Sharp.FmodTypes;
using Fmod5Sharp.Util;
using NAudio.Wave;

namespace Fmod5Sharp.CodecRebuilders
{
    public static class FmodImaAdPcmRebuilder
    {
        public const int SamplesPerFramePerChannel = 0x40;

        static readonly int[] ADPCMTable =
        {
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

        private static readonly int[] IMA_IndexTable =
        {
            -1, -1, -1, -1, 2, 4, 6, 8,
            -1, -1, -1, -1, 2, 4, 6, 8,
        };

        private static void ExpandNibble(MemoryStream stream, long byteOffset, int nibbleShift, ref int hist, ref int stepIndex)
        {
            //Read the raw nibble
            stream.Seek(byteOffset, SeekOrigin.Begin);
            var sampleNibble = (stream.ReadByte() >> nibbleShift) & 0xf;

            //Initial value for the sample is the previous value
            var sampleDecoded = hist;

            //Apply the step from the table of values above
            var step = ADPCMTable[stepIndex];

            var delta = step >> 3;
            if ((sampleNibble & 1) != 0) delta += step >> 2;
            if ((sampleNibble & 2) != 0) delta += step >> 1;
            if ((sampleNibble & 4) != 0) delta += step;
            if ((sampleNibble & 8) != 0) delta = -delta;

            //Sample changes by the delta
            sampleDecoded += delta;

            //New sample becomes the previous value, but clamped to a short.
            hist = Utils.Clamp((short)sampleDecoded, short.MinValue, short.MaxValue);

            //Step index changes based on what was stored in the file, clamped to fit in the array
            stepIndex += IMA_IndexTable[sampleNibble];
            stepIndex = Utils.Clamp((short)stepIndex, 0, 88);
        }

        private static short[] DecodeSamplesFsbIma(FmodSample sample)
        {
            const int blockSamples = 0x40;

            var numChannels = (int)sample.Metadata.Channels;

            using var stream = new MemoryStream(sample.SampleBytes);
            using var reader = new BinaryReader(stream);

            var ret = new short[sample.Metadata.SampleCount * 2];

            // Calculate frame count from sample count
            var numFrames = (int)sample.Metadata.SampleCount / SamplesPerFramePerChannel;

            for (var channel = 0; channel < numChannels; channel++)
            {
                var sampleIndex = channel;

                for (var frameNum = 0; frameNum < numFrames; frameNum++)
                {
                    //Offset of this frame in the entire sample data
                    var frameOffset = 0x24 * numChannels * frameNum;

                    //Read frame header
                    var headerIndex = frameOffset + 4 * channel;
                    stream.Seek(headerIndex, SeekOrigin.Begin);
                    int hist = reader.ReadInt16();
                    stream.Seek(headerIndex + 2, SeekOrigin.Begin);
                    int stepIndex = reader.ReadByte();

                    //Calculate initial sample value for this frame
                    stepIndex = Utils.Clamp((short)stepIndex, 0, 88);
                    ret[sampleIndex] = (short)hist;
                    sampleIndex += numChannels;

                    for (var sampleNum = 1; sampleNum <= SamplesPerFramePerChannel; sampleNum++)
                    {
                        //Offset of this sample in the entire sample data
                        //Note that this is, slightly confusingly, two different definitions of the word sample.
                        //What i mean is "index of this value within the current frame which is part of one of the channels in the FMOD 'sample', which should really be called a sound file"
                        var byteOffset = frameOffset + 4 * 2 + 4 * (channel % 2) + 4 * 2 * ((sampleNum - 1) / 8) + ((sampleNum - 1) % 8) / 2;
                        if (numChannels == 0)
                            byteOffset = frameOffset + 4 + (sampleNum - 1) / 2;

                        //Each sample is only half a byte, so odd samples use the upper half of the byte, and even samples use the lower half.
                        var nibbleShift = ((sampleNum - 1) & 1) != 0 ? 4 : 0;

                        if (sampleNum < blockSamples)
                        {
                            //Apply the IMA algorithm to convert this nibble into a full byte of data.
                            ExpandNibble(stream, byteOffset, nibbleShift, ref hist, ref stepIndex);

                            //Move to next sample
                            ret[sampleIndex] = ((short)hist);
                            sampleIndex += numChannels;
                        }
                    }
                }
            }

            return ret;
        }

        private static short[] DecodeSamplesXboxIma(FmodSample sample)
        {
            //This is a simplified version of the algorithm, because we know that this will only ever be called if we have one channel.

            const int frameSize = 0x24;
            
            using var stream = new MemoryStream(sample.SampleBytes);
            using var reader = new BinaryReader(stream);
            
            var numFrames = (int)sample.Metadata.SampleCount / SamplesPerFramePerChannel;
            
            var ret = new short[sample.Metadata.SampleCount];
            var sampleIndex = 0;

            for (var frameNum = 0; frameNum < numFrames; frameNum++)
            {
                
                
                //Offset of this frame in the entire sample data
                var frameOffset = frameSize * frameNum;

                //Read frame header
                stream.Seek(frameOffset, SeekOrigin.Begin);
                int hist = reader.ReadInt16();
                stream.Seek(frameOffset + 2, SeekOrigin.Begin);
                int stepIndex = reader.ReadByte();

                //Calculate initial sample value for this frame
                stepIndex = Utils.Clamp((short)stepIndex, 0, 88);
                ret[sampleIndex] = (short)hist;
                sampleIndex ++;

                for (var sampleNum = 1; sampleNum <= SamplesPerFramePerChannel; sampleNum++)
                {
                    //Offset of this sample in the entire sample data
                    //Note that this is, slightly confusingly, two different definitions of the word sample.
                    //What i mean is "index of this value within the current frame which is part of one of the channels in the FMOD 'sample', which should really be called a sound file"
                    var byteOffset = frameOffset + 4 + (sampleNum - 1) / 2;

                    //Each sample is only half a byte, so odd samples use the upper half of the byte, and even samples use the lower half.
                    var nibbleShift = ((sampleNum - 1) & 1) != 0 ? 4 : 0;

                    if (sampleNum < SamplesPerFramePerChannel)
                    {
                        //Apply the IMA algorithm to convert this nibble into a full byte of data.
                        ExpandNibble(stream, byteOffset, nibbleShift, ref hist, ref stepIndex);

                        //Move to next sample
                        ret[sampleIndex] = ((short)hist);
                        sampleIndex ++;
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

            var pcmShorts = numChannels == 1 ? DecodeSamplesXboxIma(sample) : DecodeSamplesFsbIma(sample);

            writer.WriteSamples(pcmShorts, 0, pcmShorts.Length);

            return stream.ToArray();
        }
    }
}