# Fmod5Sharp
## Managed decoder for FMOD 5 sound banks (FSB files).

[![NuGet](https://img.shields.io/nuget/v/Fmod5Sharp?)](https://www.nuget.org/packages/Fmod5Sharp/)

This library allows you to read FMOD 5 sound bank files (they start with the characters FSB5) into their contained samples,
and then export those samples to standard file formats (assuming the contained data format is supported).

Support for more encodings can be added as requested.

## Usage

The Fmod file can be read like this
```c#
//Will throw if the bank is not valid.
FmodSoundBank bank = FsbLoader.LoadFsbFromByteArray(rawData);
```

Or if you don't want it to throw if the file is invalid, you can use
```c#
bool success = FsbLoader.TryLoadFsbFromByteArray(rawData, out FmodSoundBank bank);
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
uint numChannels = samples[0].Metadata.Channels; //2 for stereo, 1 for mono.

string name = samples[0].Name; //Null if not present in the bank file (which is usually the case).
```

And, you can convert the audio data back to a standard format.
```c#
var success = samples[0].RebuildAsStandardFileFormat(out var dataBytes, out var fileExtension);
//Assuming success == true, then this file format was supported and you should have some data and an extension (without the leading .).
//Now you can save dataBytes to an file with the given extension on your disk and play it using your favourite audio player.
//Or you can use any standard library to convert the byte array to a different format, if you so desire.
```

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
| PCM24 | ❌ | | No games have ever been observed in the wild using this format. |
| PCM32 | ✔️ | wav | Supported in theory. No games have ever been observed in the wild using this format. |
| PCMFLOAT | ❌ | | Seen in at least one JRPG. |
| GCADPCM | ✔️ | wav | Tested with single-channel files. Not tested with stereo, but should work in theory. Seen in Unity games. |
| IMAADPCM | ✔️ | wav | Seen in Unity games. |
| VAG | ❌ | | No games have ever been observed in the wild using this format. |
| HEVAG | ❌ | | Very rarely used - only example I know of is a game for the PS Vita. |
| XMA | ❌ | | Mostly used on Xbox 360. |
| MPEG | ❌ | | Used in some older games. |
| CELT | ❌ | | Used in some older indie games. |
| AT9 | ❌ | | Native format for PlayStation Audio, including in Unity games. | 
| XWMA | ❌ | | No games have ever been observed in the wild using this format. |
| VORBIS | ✔️ | ogg | Very commonly used in Unity games. |

# Acknowledgements

This project uses:
- [OggVorbisEncoder](https://github.com/SteveLillis/.NET-Ogg-Vorbis-Encoder) to build Ogg Vorbis output streams.
- [NAudio.Core](https://github.com/naudio/NAudio) to do the same thing but for WAV files.
- [BitStreams](https://github.com/rubendal/BitStream) for parsing vorbis header data.
- [IndexRange](https://github.com/bgrainger/IndexRange) to make my life easier when supporting .NET Standard 2.0.

It also uses System.Text.Json.
