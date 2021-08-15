namespace Fmod5Sharp
{
	public class FmodSample
	{
		public FmodSampleMetadata Metadata;
		public byte[] SampleBytes;

		public FmodSample(FmodSampleMetadata metadata, byte[] sampleBytes)
		{
			Metadata = metadata;
			SampleBytes = sampleBytes;
		}
	}
}