using System;
using System.IO;
using System.Reflection;
using Fmod5Sharp.FmodVorbis;
using Xunit;

namespace Fmod5Sharp.Tests
{

    public class Fmod5SharpTests
    {
        private static byte[] LoadResource(string filename)
        {
            using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Fmod5Sharp.Tests.TestResources.{filename}") ?? throw new Exception($"File {filename} not found.");
            using BinaryReader reader = new BinaryReader(stream);

            return reader.ReadBytes((int)stream.Length);
        }
        
        [Fact]
        public void SoundBanksCanBeLoaded()
        {
            var rawData = LoadResource("short_vorbis.fsb");

            var samples = FsbLoader.LoadFsbFromByteArray(rawData).Samples;

            Assert.Single(samples, s => !s.Metadata.IsStereo && s.SampleBytes.Length > 0);
        }

        [Fact]
        public void VorbisAudioCanBeRestoredWithoutExceptions()
        {
            var rawData = LoadResource("short_vorbis.fsb");

            var samples = FsbLoader.LoadFsbFromByteArray(rawData).Samples;

            var sample = samples[0];
            
            var oggBytes = FmodVorbisRebuilder.RebuildOggFile(sample);
            
            Assert.NotEmpty(oggBytes);
            
            //Cannot assert on length output bytes because it changes with the version of libvorbis you use.
        }

        [Fact]
        public void LongerFilesWorkToo()
        {
            var rawData = LoadResource("long_vorbis.fsb");

            var samples = FsbLoader.LoadFsbFromByteArray(rawData).Samples;

            var sample = samples[0];
            
            var oggBytes = FmodVorbisRebuilder.RebuildOggFile(sample);
            
            Assert.NotEmpty(oggBytes);
        }
    }
}