using Fmod5Sharp.CodecRebuilders;
using Fmod5Sharp.FmodTypes;
using Xunit;

namespace Fmod5Sharp.Tests
{
    public class Fmod5SharpPcmTests
    {
        [Fact]
        public void Pcm16FsbFileCanBeLoaded()
        {
            var rawData = this.LoadResource("pcm16.fsb");

            var fsb = FsbLoader.LoadFsbFromByteArray(rawData);
            
            Assert.Equal(FmodAudioType.PCM16, fsb.Header.AudioType);
        }

        [Fact]
        public void PcmFilesCanBeReconstructed()
        {
            var rawData = this.LoadResource("pcm16.fsb");

            var fsb = FsbLoader.LoadFsbFromByteArray(rawData);

            var wavFile = FmodPcmRebuilder.Rebuild(fsb.Samples[0], fsb.Header.AudioType);
            
            Assert.NotEmpty(wavFile);
        }
    }
}