using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Fmod5Sharp.ChunkData;
using Fmod5Sharp.FmodTypes;
using Fmod5Sharp.Util;
using OggVorbisEncoder;

namespace Fmod5Sharp.CodecRebuilders
{
    public static class FmodVorbisRebuilder
    {
        private static Dictionary<uint, FmodVorbisData>? headers;

        private static void LoadVorbisHeaders()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Fmod5Sharp.Util.vorbis_headers_converted.json")
                               ?? throw new Exception($"Embedded resources for vorbis header data not found, has the assembly been tampered with?");
            using StreamReader reader = new(stream);

            var jsonString = reader.ReadToEnd();
            headers = JsonSerializer.Deserialize(jsonString, Fmod5SharpJsonContext.Default.DictionaryUInt32FmodVorbisData);
        }

        public static byte[] RebuildOggFile(FmodSample sample)
        {
            //Need to rebuild the vorbis header, which requires reading the known blobs from the json file.
            //This requires knowing the crc32 of the data, which is in a VORBISDATA chunk.
            var dataChunk = sample.Metadata.Chunks.FirstOrDefault(f => f.ChunkType == FmodSampleChunkType.VORBISDATA);

            if (dataChunk == null)
            {
                throw new Exception("Rebuilding Vorbis data requires a VORBISDATA chunk, which wasn't found");
            }

            var chunkData = (VorbisChunkData)dataChunk.ChunkData;
            var crc32 = chunkData.Crc32;

            //Ok, we have the crc32, now we need to find the header data.
            if (headers == null)
                LoadVorbisHeaders();
            var vorbisData = headers![crc32];
            
            vorbisData.InitBlockFlags();
            
            var infoPacket = BuildInfoPacket((byte)sample.Metadata.Channels, sample.Metadata.Frequency);
            var commentPacket = BuildCommentPacket("Fmod5Sharp (Samboy063)");
            var setupPacket = new OggPacket(vorbisData.HeaderBytes, false, 0, 2);
            
            //Begin building the final stream
            var oggStream = new OggStream(1);
            using var outputStream = new MemoryStream();
            
            oggStream.PacketIn(infoPacket);
            oggStream.PacketIn(commentPacket);
            oggStream.PacketIn(setupPacket);
            
            oggStream.FlushAndCopyTo(outputStream, true);

            CopySampleData(vorbisData, sample.SampleBytes, oggStream, outputStream);

            return outputStream.ToArray();
        }

        private static void FlushAndCopyTo(this OggStream stream, Stream other, bool force = false)
        {
            while (stream.PageOut(out var page, force))
            {
                other.Write(page.Header, 0, page.Header.Length);
                other.Write(page.Body, 0, page.Body.Length);
            }
        }

        private static OggPacket BuildInfoPacket(byte channels, int frequency)
        {
            using var memStream = new MemoryStream(30);
            using var writer = new BinaryWriter(memStream);
            
            // Packet Type (1)
            writer.Write((byte)1);
            // Codec (vorbis)
            writer.Write(System.Text.Encoding.UTF8.GetBytes("vorbis"));
            // Version (0)
            writer.Write(0);
            // Num channels
            writer.Write(channels);
            // Frequency
            writer.Write(frequency);
            
            //Leave max, nominal, and min bitrate at 0 (to be auto calculated)
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            // Block Size
            // 4 bits - Short Blocksize [-3]
            // 4 bits - Long Blocksize [-3]
            writer.Write((byte)0b1011_1000);
            // Framing Bit (1)
            writer.Write((byte)1);
            
            return new(memStream.ToArray(), false, 0, 0);
        }
      
        private static OggPacket BuildCommentPacket(string vendor)
        {
            using MemoryStream memoryStream = new();
            using BinaryReader binaryReader = new(memoryStream);

            using MemoryStream oggMs = new();
            using BinaryWriter fm = new(oggMs);

            fm.Seek(0, SeekOrigin.Begin);

            // Packet Type (3)
            fm.Write((byte)3);
            fm.Write(System.Text.Encoding.UTF8.GetBytes("vorbis")); // Codec (vorbis)

            //Length-prefixed vendor string
            fm.Write(vendor.Length);
            fm.Write(System.Text.Encoding.UTF8.GetBytes(vendor));
            
            // Number of comments (0)
            fm.Write(0);
            // Framing Bit (1)
            fm.Write((byte)1);

            return new(oggMs.ToArray(), false, 0, 1);
        }

        private static void CopySampleData(FmodVorbisData vorbisData, byte[] sampleBytes, OggStream oggStream, Stream outputStream)
        {
            using var inputStream = new MemoryStream(sampleBytes);
            using var inputReader = new BinaryReader(inputStream);

            ReadSamplePackets(inputReader, out var packetLength, out var packets);

            var packetNum = 1;
            var granulePos = 0;
            var previousBlockSize = 0;
            
            var finalPacketNum = packetLength.Count - 1;
            
            for (var i = 0; i < packets.Count; i++)
            {
                var isLast = i == finalPacketNum;
                var packet = packets[i];
                packetNum++;

                //If the input packet is empty, so is the output block, otherwise calculate based on the vorbis data.
                var blockSize = packetLength[i] == 0 ? 0 : vorbisData.GetPacketBlockSize(packet);

                //Calculate next granule position
                if (previousBlockSize == 0)
                    granulePos = 0;
                else
                    granulePos += (blockSize + previousBlockSize) / 4;
                
                //Set previous block size
                previousBlockSize = blockSize;

                //Write the packet to the stream
                oggStream.PacketIn(new(packet, isLast, granulePos,  packetNum));
                oggStream.FlushAndCopyTo(outputStream, isLast);
            }
        }

        private static void ReadSamplePackets(BinaryReader inputReader, out List<int> packetLengths, out List<byte[]> packets)
        {
            packetLengths = new();
            packets = new();

            while (inputReader.BaseStream.Position + sizeof(ushort) < inputReader.BaseStream.Length)
            {
                var packetSize = inputReader.ReadUInt16();

                if (packetSize == 0)
                    break; //EOS

                packetLengths.Add(packetSize);
                packets.Add(inputReader.ReadBytes(packetSize));
            }
        }
    }
}