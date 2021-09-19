using System;
using System.IO;
using System.Reflection;

namespace Fmod5Sharp.Tests
{
    public static class Extensions
    {
        public static byte[] LoadResource(this object _, string filename)
        {
            using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Fmod5Sharp.Tests.TestResources.{filename}") ?? throw new Exception($"File {filename} not found.");
            using BinaryReader reader = new BinaryReader(stream);

            return reader.ReadBytes((int)stream.Length);
        }
    }
}