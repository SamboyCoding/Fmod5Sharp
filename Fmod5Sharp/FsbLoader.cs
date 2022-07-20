using System;
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
            { 1, 8000 },
            { 2, 11_000 },
            { 3, 11_025 },
            { 4, 16_000 },
            { 5, 22_050 },
            { 6, 24_000 },
            { 7, 32_000 },
            { 8, 44_100 },
            { 9, 48_000 },
            { 10, 96_000 },
        };
        
        private static FmodSoundBank? LoadInternal(byte[] bankBytes, bool throwIfError)
        {
            using MemoryStream stream = new(bankBytes);
            using BinaryReader reader = new(stream);

            FmodAudioHeader header = new(reader);

            if (!header.IsValid)
            {
                if (throwIfError)
                    throw new("File is probably not an FSB file (magic number mismatch)");

                return null;
            }

            List<FmodSample> samples = new();

            //Remove header from data block.
            var bankData = bankBytes.AsSpan((int)(header.SizeOfThisHeader + header.SizeOfNameTable + header.SizeOfSampleHeaders));

            for (var i = 0; i < header.Samples.Count; i++)
            {
                var sampleMetadata = header.Samples[i];

                var firstByteOfSample = (int)sampleMetadata.DataOffset;
                var lastByteOfSample = (int)header.SizeOfData;

                if (i < header.Samples.Count - 1)
                {
                    lastByteOfSample = (int)header.Samples[i + 1].DataOffset;
                }

                var sample = new FmodSample(sampleMetadata, bankData[firstByteOfSample..lastByteOfSample].ToArray());

                if (header.SizeOfNameTable > 0)
                {
                    var nameOffsetOffset = header.SizeOfThisHeader + header.SizeOfSampleHeaders + 4 * i;
                    reader.BaseStream.Position = nameOffsetOffset;
                    var nameOffset = reader.ReadUInt32();

                    nameOffset += header.SizeOfThisHeader + header.SizeOfSampleHeaders;

                    sample.Name = bankBytes.ReadNullTerminatedString((int)nameOffset);
                }

                samples.Add(sample);
            }

            return new FmodSoundBank(header, samples);
        }

        public static bool TryLoadFsbFromByteArray(byte[] bankBytes, out FmodSoundBank? bank)
        {
            bank = LoadInternal(bankBytes, false);
            return bank != null;
        }

        public static FmodSoundBank LoadFsbFromByteArray(byte[] bankBytes)
            => LoadInternal(bankBytes, true)!;
    }
}