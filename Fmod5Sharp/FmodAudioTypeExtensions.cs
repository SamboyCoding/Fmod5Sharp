namespace Fmod5Sharp
{
    public static class FmodAudioTypeExtensions
    {
        public static bool IsSupported(this FmodAudioType @this) =>
            @this switch
            {
                FmodAudioType.VORBIS => true,
                FmodAudioType.PCM8 => true,
                FmodAudioType.PCM16 => true,
                FmodAudioType.PCM32 => true,
                FmodAudioType.GCADPCM => true,
                _ => false
            };

        public static string? FileExtension(this FmodAudioType @this) =>
            @this switch
            {
                FmodAudioType.VORBIS => "ogg",
                FmodAudioType.PCM8 => "wav",
                FmodAudioType.PCM16 => "wav",
                FmodAudioType.PCM32 => "wav",
                FmodAudioType.GCADPCM => "wav",
                _ => null
            };
    }
}