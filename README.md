# Fmod5Sharp
## Managed decoder for FMOD 5 sound banks (FSB files).

[![NuGet](https://img.shields.io/nuget/v/Fmod5Sharp)](https://www.nuget.org/packages/Fmod5Sharp/)

This library allows you to read FMOD 5 sound bank files (they start with the characters FSB5) into their contained samples,
and then export those samples to ogg files (assuming the contained data is vorbis-encoded).

Support for more encodings can be added as requested.

## Notice (Additional Dependencies)
In order to restore ogg files from the Fmod sample data, this library uses libopus and libvorbis.
These are not provided, and must be installed separately or shipped with your application.
If on windows, they should be named `opus.dll` and `vorbis.dll`.
On other platforms, installing libopus or libvorbis should be sufficient, but I haven't tested this.

Regardless, the architecture of the native assemblies must match that of your application, or you will get a `BadImageFormatException` 
thrown by the system. 

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
For example if `bank.Header.AudioType == FmodAudioType.VORBIS`:
```c#
var oggFileBytes = FmodVorbisRebuilder.RebuildOggFile(samples[0]);
//Now you can save oggFileBytes to an .ogg file on your disk and play it using your favourite audio player.
//Or you can use any standard library to convert the byte array to a different format, if you so desire.
```