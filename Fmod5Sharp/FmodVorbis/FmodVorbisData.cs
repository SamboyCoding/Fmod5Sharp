using System;
using System.Linq;
using System.Text.Json.Serialization;
using BitStreams;

namespace Fmod5Sharp.FmodVorbis;

public class FmodVorbisData
{
    public byte[] headerBytes { get; set; }
    public int seekBit { get; set; }

    [JsonIgnore] public byte[] blockFlags { get; private set; } = Array.Empty<byte>();
    
    private bool _initialized;

    internal void InitBlockFlags()
    {
        if(_initialized)
            return;

        _initialized = true;
        
        var bitStream = new BitStream(headerBytes);

        if (bitStream.ReadByte() != 5) //packing type 5 == books
            return;

        if (bitStream.ReadString(6) != "vorbis") //validate magic
            return;

        //Whole bytes, bit remainder
        bitStream.Seek(seekBit / 8, seekBit % 8);

        //Read 6 bits and add one
        var numModes = bitStream.ReadByte(6) + 1; 

        //Read the first bit of each mode and skip the rest of the mode data. These are our flags.
        blockFlags = Enumerable.Range(0, numModes).Select(_ =>
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

        if (blockFlags.Length > 0)
            mode = bitStream.ReadByte(blockFlags.Length - 1);

        if (blockFlags[mode] == 1)
            return 2048;

        return 256;
    }
}