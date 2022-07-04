using System;
using System.Linq;
using System.Text.Json.Serialization;
using BitStreams;

namespace Fmod5Sharp.Util;

internal class FmodVorbisData
{
    [JsonPropertyName("headerBytes")]
    public byte[] HeaderBytes { get; set; }
    
    [JsonPropertyName("seekBit")]
    public int SeekBit { get; set; }
    
    [JsonConstructor]
    public FmodVorbisData(byte[] headerBytes, int seekBit)
    {
        HeaderBytes = headerBytes;
        SeekBit = seekBit;
    }

    [JsonIgnore] private byte[] BlockFlags { get; set; } = Array.Empty<byte>();
    
    private bool _initialized;

    internal void InitBlockFlags()
    {
        if(_initialized)
            return;

        _initialized = true;
        
        var bitStream = new BitStream(HeaderBytes);

        if (bitStream.ReadByte() != 5) //packing type 5 == books
            return;

        if (bitStream.ReadString(6) != "vorbis") //validate magic
            return;

        //Whole bytes, bit remainder
        bitStream.Seek(SeekBit / 8, SeekBit % 8);

        //Read 6 bits and add one
        var numModes = bitStream.ReadByte(6) + 1; 

        //Read the first bit of each mode and skip the rest of the mode data. These are our flags.
        BlockFlags = Enumerable.Range(0, numModes).Select(_ =>
        {
            var flag = (byte)bitStream.ReadBit();

            //Skip the bits we don't care about
            bitStream.ReadBits(16);
            bitStream.ReadBits(16);
            bitStream.ReadBits(8);

            return flag;
        }).ToArray();
    }

    public int GetPacketBlockSize(byte[] packetBytes)
    {
        var bitStream = new BitStream(packetBytes);

        if (bitStream.ReadBit())
            return 0;

        var mode = 0;

        if (BlockFlags.Length > 1)
            mode = bitStream.ReadByte(BlockFlags.Length - 1);

        if (BlockFlags[mode] == 1)
            return 2048;

        return 256;
    }
}