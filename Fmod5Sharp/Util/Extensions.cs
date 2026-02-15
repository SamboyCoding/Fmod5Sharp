using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Fmod5Sharp.Util
{
    internal static class Extensions
    {
        internal static T ReadEndian<T>(this BinaryReader reader) where T : IBinaryReadable, new()
        {
            var t = new T();
            t.Read(reader);

            return t;
        }

        internal static long Position(this BinaryReader reader) => reader.BaseStream.Position;

        internal static string ReadString(this BinaryReader reader, int length, Encoding? encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;
            
            var bytes = reader.ReadBytes(length);

            return encoding.GetString(bytes);
        }
        
        internal static ulong Bits(this uint raw, int lowestBit, int numBits) => ((ulong)raw).Bits(lowestBit, numBits);

        internal static ulong Bits(this ulong raw, int lowestBit, int numBits)
        {
            ulong mask = 1;
            for (var i = 1; i < numBits; i++)
            {
                mask = (mask << 1) | 1;
            }

            mask <<= lowestBit;

            return (raw & mask) >> lowestBit;
        }

        internal static string ReadNullTerminatedString(this Stream stream)
        {
            List<byte> bytes = new(16);
            int b;
            while ((b = stream.ReadByte()) > 0)
                bytes.Add((byte)b);

            return Encoding.UTF8.GetString(bytes.ToArray());
        }
    }
}