using Fmod5Sharp.CodecRebuilders;
using Fmod5Sharp.FmodTypes;
using NVorbis;
using System.IO;
using Xunit;

namespace Fmod5Sharp.Tests
{

    public class Fmod5SharpVorbisTests
    {
        [Fact]
        public void SoundBanksCanBeLoaded()
        {
            var rawData = this.LoadResource("short_vorbis.fsb");

            var samples = FsbLoader.LoadFsbFromByteArray(rawData).Samples;

            Assert.Single(samples, s => !s.Metadata.IsStereo && s.SampleBytes.Length > 0);
        }

        [Fact]
        public void VorbisAudioCanBeRestoredWithoutExceptions()
        {
            var rawData = this.LoadResource("short_vorbis.fsb");

            var samples = FsbLoader.LoadFsbFromByteArray(rawData).Samples;

            var sample = samples[0];
            
            var oggBytes = FmodVorbisRebuilder.RebuildOggFile(sample);
            
            CheckSampleCount(sample, oggBytes);
        }

        [Fact]
        public void LongerFilesWorkToo()
        {
            var rawData = this.LoadResource("long_vorbis.fsb");

            var samples = FsbLoader.LoadFsbFromByteArray(rawData).Samples;

            var sample = samples[0];
            
            var oggBytes = FmodVorbisRebuilder.RebuildOggFile(sample);
            
            CheckSampleCount(sample, oggBytes);
        }

        [Fact]
        public void PreviouslyUnrecoverableVorbisFilesWorkWithOurCustomRebuilder()
        {
            var rawData = this.LoadResource("previously_unrecoverable_vorbis.fsb");

            var samples = FsbLoader.LoadFsbFromByteArray(rawData).Samples;

            var sample = samples[0];
            
            var oggBytes = FmodVorbisRebuilder.RebuildOggFile(sample);
            
            CheckSampleCount(sample, oggBytes);
        }

        [Fact]
        public void VorbisFilesThatPreviouslyThrewExceptionsDoNot()
        {
            var rawData = this.LoadResource("vorbis_with_blockflag_exception.fsb");

            var samples = FsbLoader.LoadFsbFromByteArray(rawData).Samples;

            var sample = samples[0];
            
            var oggBytes = FmodVorbisRebuilder.RebuildOggFile(sample);
            
            CheckSampleCount(sample, oggBytes);
        }

        private bool CheckSampleCount(FmodSample sample, byte[] oggBytes)
        {
            Assert.Equal(sample.Metadata.SampleCount, new VorbisReader(new MemoryStream(oggBytes)).TotalSamples);
        }
    }
}