# Fmod5Sharp
## Managed decoder for FMOD 5 sound banks (FSB files).

[![NuGet](https://img.shields.io/nuget/v/Fmod5Sharp?)](https://www.nuget.org/packages/Fmod5Sharp/)

This library allows you to read FMOD 5 sound bank files (they start with the characters FSB5) into their contained samples,
and then export those samples to standard file formats (assuming the contained data format is supported).

Support for more encodings can be added as requested.

## Usage

The Fmod file can be read like this
```c#
FmodSoundBank bank = FsbLoader.LoadFsbFromByteArray(rawData);
```

You can then query some properties about the bank:
```c#
FmodAudioType type = bank.Header.AudioType;
uint fmodSubVersion = bank.Header.Version; //0 or 1 have been observed
```

And get the samples stored inside it:
```c#
List<FmodSample> samples = bank.Samples;
int frequency = samples[0].Metadata.Frequency; //E.g. 44100
uint numChannels = samples[0].Channels; //2 for stereo, 1 for mono.
```

And, you can convert the audio data back to a standard format.
```c#
var success = samples[0].RebuildAsStandardFileFormat(out var dataBytes, out var fileExtension);
//Assuming success == true, then this file format was supported and you should have some data and an extension (without the leading .).
//Now you can save dataBytes to an file with the given extension on your disk and play it using your favourite audio player.
//Or you can use any standard library to convert the byte array to a different format, if you so desire.
```

If the user's system does not have libopus or libvorbis, and the data is vorbis-encoded, this will throw a `DllNotFoundException`.

You can also check if a given format type is supported and, if so, what extension it will result in, like so:
```c#
bool isSupported = bank.Header.AudioType.IsSupported();

//Null if not supported
string? extension = bank.Header.AudioType.FileExtension();
```

Alternatively, you can consult the table below:

| Format | Supported? | Extension | Notes |
| :-----: | :--------------: | :---------: | :----------: |
| PCM8 | ✔️ | wav | |
| PCM16 | ✔️ | wav | |
| PCM24 | ❌ | | |
| PCM32 | ✔️ | wav | |
| PCMFLOAT | ❌ | | |
| GCADPCM | ✔️ | wav | Tested with single-channel files. Not tested with stereo, but should work in theory. |
| IMAADPCM | ✔️ | wav | |
| VAG | ❌ | | |
| HEVAG | ❌ | | |
| XMA | ❌ | | |
| MPEG | ❌ | | |
| CELT | ❌ | | |
| AT9 | ❌ | | | 
| XWMA | ❌ | | |
| VORBIS | ✔️ | ogg | Requires native libraries on user's system. |

# Acknowledgements

This project uses:
- [OggVorbisEncoder](https://github.com/SteveLillis/.NET-Ogg-Vorbis-Encoder) to build Ogg Vorbis output streams.
- [NAudio.Core](https://github.com/naudio/NAudio) to do the same thing but for WAV files.
- [BitStreams](https://github.com/rubendal/BitStream) for parsing vorbis header data.
It also uses System.Text.Json.