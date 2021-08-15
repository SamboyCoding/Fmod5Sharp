using System.Collections.Generic;
using System.IO;

namespace Fmod5Sharp
{
	public class FmodSampleMetadata : IBinaryReadable
	{
		internal bool HasAnyChunks;
		internal uint FrequencyId;
		internal ulong DataOffset;
		internal List<FmodSampleChunk> Chunks = new();

		public bool IsStereo;
		public ulong SampleCount;
		public int Frequency => FsbLoader.Frequencies.TryGetValue(FrequencyId, out var actualFrequency) ? actualFrequency : (int)FrequencyId; //If set by FREQUENCY chunk, id is actual frequency
		public uint Channels => IsStereo ? 2u : 1u;

		void IBinaryReadable.Read(BinaryReader reader)
		{
			var encoded = reader.ReadUInt64();
			
			HasAnyChunks = (encoded & 1) == 1;
			FrequencyId = (uint) encoded.Bits(1, 4);
			IsStereo = encoded.Bits(5, 1) == 1;
			DataOffset = encoded.Bits(6, 28) * 16;
			SampleCount = encoded.Bits(34, 30);
		}
	}
}