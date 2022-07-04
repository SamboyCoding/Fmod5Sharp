using System.Linq;
using Xunit;

namespace Fmod5Sharp.Tests
{
    public class Fmod5ImaAdPcmTests
    {
        [Fact]
        public void BanksCanBeLoaded()
        {
            var rawData = this.LoadResource("imaadpcm_short.fsb");

            var fsb = FsbLoader.LoadFsbFromByteArray(rawData);
            
            Assert.Equal(FmodAudioType.IMAADPCM, fsb.Header.AudioType);
            Assert.Equal(2u, fsb.Samples.Single().Metadata.Channels);
        }
        
        [Fact]
        public void ImaAdPcmBanksCanBeRebuilt()
        {
            var rawData = this.LoadResource("imaadpcm_short.fsb");

            var fsb = FsbLoader.LoadFsbFromByteArray(rawData);

            var bytes = FmodImaAdPcmRebuilder.Rebuild(fsb.Samples[0]);
            
            Assert.NotEmpty(bytes);
        }

        [Fact]
        public void XboxImaAdPcmBanksCanBeRebuilt()
        {
            var rawData = this.LoadResource("xbox_imaad.fsb");
            
            var fsb = FsbLoader.LoadFsbFromByteArray(rawData);
            
            Assert.Equal(FmodAudioType.IMAADPCM, fsb.Header.AudioType);
            
            var bytes = FmodImaAdPcmRebuilder.Rebuild(fsb.Samples[0]);
            
            Assert.NotEmpty(bytes);
        }
    }
}