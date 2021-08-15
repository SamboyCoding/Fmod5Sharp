using System.IO;

namespace Fmod5Sharp.ChunkData
{
	internal  interface IChunkData
	{
		public void Read(BinaryReader reader, uint expectedSize);
	}
}