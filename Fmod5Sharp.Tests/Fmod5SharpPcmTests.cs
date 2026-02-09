using Fmod5Sharp.CodecRebuilders;
using Fmod5Sharp.FmodTypes;
using NAudio.Wave;
using System.IO;
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

            var sample = fsb.Samples[0];

            var wavFile = FmodPcmRebuilder.Rebuild(sample, fsb.Header.AudioType);

            Assert.Equal(sample.Metadata.SampleCount, new WaveFileReader(new MemoryStream(wavFile)).SampleCount);
        }
    }
}