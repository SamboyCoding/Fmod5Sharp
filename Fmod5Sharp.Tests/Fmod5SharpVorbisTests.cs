using Fmod5Sharp.FmodVorbis;
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
            
            Assert.NotEmpty(oggBytes);
            
            //Cannot assert on length output bytes because it changes with the version of libvorbis you use.
        }

        [Fact]
        public void LongerFilesWorkToo()
        {
            var rawData = this.LoadResource("long_vorbis.fsb");

            var samples = FsbLoader.LoadFsbFromByteArray(rawData).Samples;

            var sample = samples[0];
            
            var oggBytes = FmodVorbisRebuilder.RebuildOggFile(sample);
            
            Assert.NotEmpty(oggBytes);
        }

        [Fact]
        public void PreviouslyUnrecoverableVorbisFilesWorkWithOurCustomRebuilder()
        {
            var rawData = this.LoadResource("previously_unrecoverable_vorbis.fsb");

            var samples = FsbLoader.LoadFsbFromByteArray(rawData).Samples;

            var sample = samples[0];
            
            var oggBytes = FmodVorbisRebuilder.RebuildOggFile(sample);
            
            Assert.NotEmpty(oggBytes);
        }
    }
}