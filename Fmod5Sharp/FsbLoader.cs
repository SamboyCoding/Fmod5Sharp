using System.Collections.Generic;
using System.IO;
using Fmod5Sharp.FmodTypes;
using Fmod5Sharp.Util;

namespace Fmod5Sharp
{
    public static class FsbLoader
    {
        internal static readonly Dictionary<uint, int> Frequencies = new()
        {
            { 0, 4_000 },
            { 1, 8_000 },
            { 2, 11_000 },
            { 3, 12_000 },
            { 4, 16_000 },
            { 5, 22_050 },
            { 6, 24_000 },
            { 7, 32_000 },
            { 8, 44_100 },
            { 9, 48_000 },
            { 10, 96_000 },
        };
        
        private static FmodSoundBank? LoadInternal(Stream stream, bool throwIfError)
        {
            using BinaryReader reader = new(stream);

            FmodAudioHeader header = new(reader);

            if (!header.IsValid)
            {
                if (throwIfError)
                    throw new("File is probably not an FSB file (magic number mismatch)");

                return null;
            }

            long dataStartOffset = header.SizeOfThisHeader + header.SizeOfNameTable + header.SizeOfSampleHeaders;

            List<FmodSample> samples = new(header.Samples.Count);
            for (var i = 0; i < header.Samples.Count; i++)
            {
                var sampleMetadata = header.Samples[i];

                var firstByteOfSample = (long)sampleMetadata.DataOffset;
                var lastByteOfSample = (long)header.SizeOfData;

                if (i < header.Samples.Count - 1)
                {
                    lastByteOfSample = header.Samples[i + 1].DataOffset;
                }

                var sampleData = new byte[lastByteOfSample - firstByteOfSample];
                stream.Position = dataStartOffset + firstByteOfSample;
#if NET8_0_OR_GREATER
                stream.ReadExactly(sampleData, 0, sampleData.Length);
#else 
                stream.Read(sampleData, 0, sampleData.Length);
#endif

                var sample = new FmodSample(sampleMetadata, sampleData);

                if (header.SizeOfNameTable > 0)
                {
                    var nameOffsetOffset = header.SizeOfThisHeader + header.SizeOfSampleHeaders + 4 * i;
                    reader.BaseStream.Position = nameOffsetOffset;
                    var nameOffset = reader.ReadUInt32();

                    nameOffset += header.SizeOfThisHeader + header.SizeOfSampleHeaders;

                    stream.Position = nameOffset;
                    sample.Name = stream.ReadNullTerminatedString();
                }

                samples.Add(sample);
            }

            return new FmodSoundBank(header, samples);
        }

        public static bool TryLoadFsbFromStream(Stream stream, out FmodSoundBank? bank)
        {
            bank = LoadInternal(stream, false);
            return bank != null;
        }

        public static FmodSoundBank LoadFsbFromStream(Stream stream)
            => LoadInternal(stream, true)!;

        public static bool TryLoadFsbFromByteArray(byte[] bankBytes, out FmodSoundBank? bank)
        {
            using var stream = new MemoryStream(bankBytes);
            bank = LoadInternal(stream, false);
            return bank != null;
        }

        public static FmodSoundBank LoadFsbFromByteArray(byte[] bankBytes)
        {
            using var stream = new MemoryStream(bankBytes);
            return LoadInternal(stream, true)!;
        }
    }
}