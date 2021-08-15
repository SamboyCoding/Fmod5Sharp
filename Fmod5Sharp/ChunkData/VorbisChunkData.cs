using System.IO;

namespace Fmod5Sharp.ChunkData
{
	internal class VorbisChunkData : IChunkData
	{
		public uint Crc32;

		public void Read(BinaryReader reader, uint expectedSize)
		{
			Crc32 = reader.ReadUInt32();
			byte[] unknown = reader.ReadBytes((int)(expectedSize - 4));
		}
	}
}