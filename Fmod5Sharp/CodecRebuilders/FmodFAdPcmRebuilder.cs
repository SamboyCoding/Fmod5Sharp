using System.IO;
using Fmod5Sharp.FmodTypes;
using Fmod5Sharp.Util;
using NAudio.Wave;

namespace Fmod5Sharp.CodecRebuilders;

public static class FmodFAdPcmRebuilder
{
    private static readonly byte[][] globalCoefs = new[]
    {
        new byte[] { 0, 0 },
        new byte[] { 60, 0 },
        new byte[] { 122, 60 },
        new byte[] { 115, 52 },
        new byte[] { 98, 55 },
        new byte[] { 0, 0 },
        new byte[] { 0, 0 },
        new byte[] { 0, 0 },
    };
    
    private static short[] RebuildPcmData(FmodSample fmodSample)
    {
        const int headerLength = 0xc;
        const int bytesPerFrame = 0x8c;
        const int samplesPerFrame = (bytesPerFrame - headerLength) * 2; //256

        var numFrames = fmodSample.SampleBytes.Length / bytesPerFrame;

        var pcmData = new short[numFrames * samplesPerFrame];
        
        using var stream = new MemoryStream(fmodSample.SampleBytes);
        using var reader = new BinaryReader(stream);
        
        var outPos = 0;
        while (outPos < pcmData.Length)
        {
            //Read header
            var coefs = reader.ReadUInt32();
            var shifts = reader.ReadUInt32();
            var hist1 = (int) reader.ReadInt16();
            var hist2 = (int) reader.ReadInt16();

            for (var i = 0; i < 8; i++)
            {
                var index = (((int) coefs >> i * 4) & 0x0F) % 7;
                var shift = (int) (shifts >> i * 4) & 0x0F;
                
                var coef1 = (int) globalCoefs[index][0];
                var coef2 = (int) globalCoefs[index][1];

                shift = 22 - shift;

                for (var j = 0; j < 4; j++)
                {
                    var nibbles = reader.ReadUInt32();

                    for (var k = 0; k < 8; k++)
                    {
                        int sample;
                        
                        sample = (int)((nibbles >> k * 4) & 0x0F);
                        sample = (sample << 28) >> shift;
                        sample = (sample - hist2 * coef2 + hist1 * coef1) >> 6;
                        sample = Utils.ClampInt(sample, short.MinValue, short.MaxValue);
                        
                        pcmData[outPos++] = (short) sample;
                        
                        hist2 = hist1;
                        hist1 = sample;
                    }
                }
            }
        }

        return pcmData;
    }

    public static byte[] RebuildFile(FmodSample sample)
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

        var pcmShorts = RebuildPcmData(sample);

        writer.WriteSamples(pcmShorts, 0, pcmShorts.Length);

        return stream.ToArray();
    }
}