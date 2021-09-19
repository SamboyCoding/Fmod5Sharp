using System.Collections.Generic;

namespace Fmod5Sharp
{
    public class FmodSoundBank
    {
        public FmodAudioHeader Header;
        public List<FmodSample> Samples;

        internal FmodSoundBank(FmodAudioHeader header, List<FmodSample> samples)
        {
            Header = header;
            Samples = samples;
            Samples.ForEach(s => s.MyBank = this);
        }
    }
}