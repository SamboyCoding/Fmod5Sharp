using System.Collections.Generic;
using System.IO;
using Fmod5Sharp.Util;

namespace Fmod5Sharp.FmodTypes
{
	public class FmodSampleMetadata : IBinaryReadable
	{
		internal bool HasAnyChunks;
		internal uint FrequencyId;
		internal ulong DataOffset;
		internal List<FmodSampleChunk> Chunks = new();
		internal int NumChannels;

		public bool IsStereo;
		public ulong SampleCount;

		public int Frequency => FsbLoader.Frequencies.TryGetValue(FrequencyId, out var actualFrequency) ? actualFrequency : (int)FrequencyId; //If set by FREQUENCY chunk, id is actual frequency
		public uint Channels => (uint)NumChannels;

		void IBinaryReadable.Read(BinaryReader reader)
		{
			var encoded = reader.ReadUInt64();
			
			HasAnyChunks = (encoded & 1) == 1; //Bit 0
			FrequencyId = (uint) encoded.Bits( 1, 4); //Bits 1-4
			var pow2 = (int) encoded.Bits(5, 2); //Bits 5-6
			NumChannels = 1 << pow2;
			if (NumChannels > 2)
				throw new("> 2 channels not supported");
			
			IsStereo = NumChannels == 2;
			
			DataOffset = encoded.Bits(7, 27) * 32;
			SampleCount = encoded.Bits(34, 30);
		}
	}
}