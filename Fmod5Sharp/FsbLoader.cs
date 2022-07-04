using System.Collections.Generic;
using System.IO;
using System.Linq;
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

		public static FmodSoundBank LoadFsbFromByteArray(byte[] bankBytes)
		{
			using MemoryStream stream = new(bankBytes);
			using BinaryReader reader = new(stream);

			FmodAudioHeader header = new(reader);

			List<FmodSample> samples = new();

			//Remove header from data block.
			bankBytes = bankBytes.Skip((int)reader.Position()).ToArray();
			
			for (var i = 0; i < header.Samples.Count; i++)
			{
				FmodSampleMetadata sampleMetadata = header.Samples[i];

				var firstByteOfSample = sampleMetadata.DataOffset;
				ulong lastByteOfSample = header.DataSize;

				if (i < header.Samples.Count - 1)
				{
					lastByteOfSample = header.Samples[i + 1].DataOffset;
				}

				samples.Add(
					new FmodSample(
						sampleMetadata,
						bankBytes.Skip((int)firstByteOfSample).Take((int)(lastByteOfSample - firstByteOfSample)).ToArray()
					)
				);
			}

			return new FmodSoundBank(header, samples);
		}
	}
}