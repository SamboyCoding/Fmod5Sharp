using System.IO;

namespace Fmod5Sharp.ChunkData
{
	internal class LoopChunkData : IChunkData
	{
		public uint LoopStart;
		public uint LoopEnd;

		public void Read(BinaryReader reader, uint expectedSize)
		{
			LoopStart = reader.ReadUInt32();
			LoopEnd = reader.ReadUInt32();
		}
	}
}