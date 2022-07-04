using Fmod5Sharp.CodecRebuilders;
using Fmod5Sharp.FmodTypes;
using Xunit;

namespace Fmod5Sharp.Tests
{
    public class Fmod5SharpGcadPcmTests
    {
        [Fact]
        public void GcadPcmBanksCanBeLoaded()
        {
            var rawData = this.LoadResource("gcadpcm.fsb");

            var fsb = FsbLoader.LoadFsbFromByteArray(rawData);
            
            Assert.Equal(FmodAudioType.GCADPCM, fsb.Header.AudioType);
        }
        
        [Fact]
        public void GcadPcmBanksCanBeRebuilt()
        {
            var rawData = this.LoadResource("gcadpcm.fsb");

            var fsb = FsbLoader.LoadFsbFromByteArray(rawData);

            var bytes = FmodGcadPcmRebuilder.Rebuild(fsb.Samples[0]);
            
            Assert.NotEmpty(bytes);
        }
    }
}