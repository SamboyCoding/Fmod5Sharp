using System;
using Fmod5Sharp.FmodVorbis;

namespace Fmod5Sharp
{
	public class FmodSample
	{
		public FmodSampleMetadata Metadata;
		public byte[] SampleBytes;
		internal FmodSoundBank? MyBank;

		public FmodSample(FmodSampleMetadata metadata, byte[] sampleBytes)
		{
			Metadata = metadata;
			SampleBytes = sampleBytes;
		}

		public byte[] RebuildAsStandardFileFormat() =>
			MyBank!.Header.AudioType switch
			{
				FmodAudioType.VORBIS => FmodVorbisRebuilder.RebuildOggFile(this),
				FmodAudioType.PCM8 => FmodPcmRebuilder.Rebuild(this, MyBank.Header.AudioType),
				FmodAudioType.PCM16 => FmodPcmRebuilder.Rebuild(this, MyBank.Header.AudioType),
				FmodAudioType.PCM32 => FmodPcmRebuilder.Rebuild(this, MyBank.Header.AudioType),
				_ => throw new NotSupportedException($"Rebuilding of audio type {MyBank.Header.AudioType} not yet implemented. Please open a ticket on the GitHub repository for support.")
			};
	}
}