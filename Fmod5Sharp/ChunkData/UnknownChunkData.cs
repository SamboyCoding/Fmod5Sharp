using System.IO;

namespace Fmod5Sharp.ChunkData
{
	internal class UnknownChunkData : IChunkData
	{
		public byte[] UnknownData;

		public void Read(BinaryReader reader, uint expectedSize)
		{
			UnknownData = reader.ReadBytes((int)expectedSize);
		}
	}
}