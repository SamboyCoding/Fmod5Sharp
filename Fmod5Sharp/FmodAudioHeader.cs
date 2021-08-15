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

			Version = reader.ReadUInt32();
			NumSamples = reader.ReadUInt32();
			var sizeOfSampleHeaders = reader.ReadUInt32();
			var nameTableSize = reader.ReadUInt32();
			DataSize = reader.ReadUInt32();
			AudioType = (FmodAudioType) reader.ReadUInt32();
			
			reader.ReadUInt64(); //Ignored, called "zero" in python

			//128-bit hash
			var hashLower = reader.ReadUInt64();
			var hashUpper = reader.ReadUInt64();

			reader.ReadUInt64(); //Ignored, called "dummy" in python

			if (Version == 0)
			{
				reader.ReadUInt32(); //Ignored, called "unknown" in python
			}

			var sampleHeadersStart = reader.Position();
			for (var i = 0; i < NumSamples; i++)
			{
				FmodSampleMetadata sampleMetadata = reader.ReadEnadian<FmodSampleMetadata>();

				var continueReadingChunks = sampleMetadata.HasAnyChunks;
				List<FmodSampleChunk> chunks = new();
				while (continueReadingChunks)
				{
					FmodSampleChunk nextChunk = reader.ReadEnadian<FmodSampleChunk>();
					continueReadingChunks = nextChunk.MoreChunks;
					chunks.Add(nextChunk);
				}

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