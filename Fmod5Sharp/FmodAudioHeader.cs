using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fmod5Sharp.ChunkData;

namespace Fmod5Sharp
{
	public class FmodAudioHeader
	{
		public FmodAudioType AudioType;
		public uint Version;
		public uint NumSamples;
		
		internal uint DataSize;
		internal List<FmodSampleMetadata> Samples = new();

		public FmodAudioHeader(BinaryReader reader)
		{
			string magic = reader.ReadString(4);

			if (magic != "FSB5")
			{
				return;
			}

			Version = reader.ReadUInt32(); //0x04
			NumSamples = reader.ReadUInt32(); //0x08
			var sizeOfSampleHeaders = reader.ReadUInt32(); //0x0C
			var nameTableSize = reader.ReadUInt32(); //0x10
			DataSize = reader.ReadUInt32(); //0x14
			AudioType = (FmodAudioType) reader.ReadUInt32(); //0x18
			
			if (Version == 0)
			{
				reader.ReadUInt32(); //Version 0 has an extra field at 0x1C
			}
			
			reader.ReadUInt64(); //Skip 0x1C (zero) and 0x20 (flags)

			//128-bit hash
			var hashLower = reader.ReadUInt64(); //0x24
			var hashUpper = reader.ReadUInt64(); //0x30

			reader.ReadUInt64(); //Skip unknown value at 0x34

			var sampleHeadersStart = reader.Position();
			for (var i = 0; i < NumSamples; i++)
			{
				FmodSampleMetadata sampleMetadata = reader.ReadEndian<FmodSampleMetadata>();

				FmodSampleChunk.CurrentSample = sampleMetadata;
				var continueReadingChunks = sampleMetadata.HasAnyChunks;
				List<FmodSampleChunk> chunks = new();
				while (continueReadingChunks)
				{
					FmodSampleChunk nextChunk = reader.ReadEndian<FmodSampleChunk>();
					continueReadingChunks = nextChunk.MoreChunks;
					chunks.Add(nextChunk);
				}
				
				FmodSampleChunk.CurrentSample = null;

				if (chunks.FirstOrDefault(c => c.ChunkType == FmodSampleChunkType.FREQUENCY) is { ChunkData: FrequencyChunkData fcd })
				{
					sampleMetadata.FrequencyId = fcd.ActualFrequencyId;
				}

				sampleMetadata.Chunks = chunks;
				
				Samples.Add(sampleMetadata);
			}

			var actualSampleHeadersLength = reader.Position() - sampleHeadersStart;

			if (actualSampleHeadersLength != sizeOfSampleHeaders)
			{
				//Skip zero-padding so we're in the right place for data.
				reader.ReadBytes((int)(sizeOfSampleHeaders - actualSampleHeadersLength));
			}
		}
	}
}