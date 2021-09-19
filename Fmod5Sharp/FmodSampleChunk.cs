using System;
using System.IO;
using Fmod5Sharp.ChunkData;

namespace Fmod5Sharp
{
	internal class FmodSampleChunk : IBinaryReadable
	{
		internal static FmodSampleMetadata? CurrentSample;
		
		public FmodSampleChunkType ChunkType;
		public uint ChunkSize;
		public bool MoreChunks;
		internal IChunkData ChunkData;

		void IBinaryReadable.Read(BinaryReader reader)
		{
			var chunkInfoRaw = reader.ReadUInt32();
			MoreChunks = chunkInfoRaw.Bits(0, 1) == 1;
			ChunkSize = (uint)chunkInfoRaw.Bits(1, 24);
			ChunkType = (FmodSampleChunkType) chunkInfoRaw.Bits(25, 7);

			ChunkData = ChunkType switch
			{
				FmodSampleChunkType.VORBISDATA => new VorbisChunkData(),
				FmodSampleChunkType.FREQUENCY => new FrequencyChunkData(),
				FmodSampleChunkType.CHANNELS => new ChannelChunkData(),
				FmodSampleChunkType.LOOP => new LoopChunkData(),
				FmodSampleChunkType.DSPCOEFF => new DspCoefficientsBlockData(CurrentSample!),
				_ => new UnknownChunkData(),
			};

			var startPos = reader.Position();
			
			ChunkData.Read(reader, ChunkSize);

			var actualBytesRead = reader.Position() - startPos;

			if (actualBytesRead != ChunkSize)
			{
				throw new Exception($"Expected fmod sample chunk to read {ChunkSize} bytes, but it only read {actualBytesRead}");
			}
		}
	}
}