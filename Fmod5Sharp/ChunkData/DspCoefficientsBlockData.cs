using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Fmod5Sharp.ChunkData
{
    public class DspCoefficientsBlockData : IChunkData
    {
        public List<short>[] ChannelData;
        private readonly FmodSampleMetadata _sampleMetadata;

        public DspCoefficientsBlockData(FmodSampleMetadata sampleMetadata)
        {
            _sampleMetadata = sampleMetadata;
            ChannelData = new List<short>[_sampleMetadata.Channels];
            for (var i = 0; i < _sampleMetadata.Channels; i++) 
                ChannelData[i] = new();
        }
        
        public void Read(BinaryReader reader, uint expectedSize)
        {
            for (var ch = 0; ch < _sampleMetadata.Channels; ch++)
            {
                //0x2E bytes per channel. First 0x20 (=> 0x10 shorts) are the coefficients.
                for (var i = 0; i < 16; i++)
                {
                    //We can't use ReadInt16 here because BinaryReader is little-endian, and FSB5 encodes this data big-endian
                    //So instead, read 2 bytes, reverse, then convert to short.
                    ChannelData[ch].Add(BitConverter.ToInt16(reader.ReadBytes(2).Reverse().ToArray(), 0));
                }
                //Extra 0xE = 14 bytes
                reader.ReadInt64();
                reader.ReadInt32();
                reader.ReadInt16();
            }
        }
    }
}