using System.IO;

namespace Fmod5Sharp.ChunkData
{
	internal class UnknownChunkData : IChunkData
	{
		public byte[] UnknownData = new byte[0];

		public void Read(BinaryReader reader, uint expectedSize)
		{
			UnknownData = reader.ReadBytes((int)expectedSize);
		}
	}
}