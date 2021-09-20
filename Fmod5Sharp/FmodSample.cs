using System;
using System.Diagnostics.CodeAnalysis;
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

		public bool RebuildAsStandardFileFormat([NotNullWhen(true)] out byte[]? data, [NotNullWhen(true)] out string? fileExtension)
		{
			switch(MyBank!.Header.AudioType)
			{
				case FmodAudioType.VORBIS:
					data = FmodVorbisRebuilder.RebuildOggFile(this);
					fileExtension = "ogg";
					return data.Length > 0;
				case FmodAudioType.PCM8:
				case FmodAudioType.PCM16:
				case FmodAudioType.PCM32:
					data = FmodPcmRebuilder.Rebuild(this, MyBank.Header.AudioType);
					fileExtension = "wav";
					return data.Length > 0;
				case FmodAudioType.GCADPCM:
					data = FmodGcadPcmRebuilder.Rebuild(this);
					fileExtension = "wav";
					return data.Length > 0;
				case FmodAudioType.IMAADPCM:
					data = FmodImaAdPcmRebuilder.Rebuild(this);
					fileExtension = "wav";
					return data.Length > 0;
				default:
					data = null;
					fileExtension = null;
					return false;
			}
		}
	}
}