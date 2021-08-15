using System.IO;

namespace Fmod5Sharp.ChunkData
{
	internal class ChannelChunkData : IChunkData
	{
		public byte NumChannels;

		public void Read(BinaryReader reader, uint expectedSize)
		{
			NumChannels = reader.ReadByte();
		}
	}
}