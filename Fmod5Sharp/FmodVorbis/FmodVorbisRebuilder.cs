using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using Fmod5Sharp.ChunkData;
using OggVorbisSharp;

namespace Fmod5Sharp.FmodVorbis
{
	public class FmodVorbisRebuilder
	{
		private static Dictionary<uint, byte[]>? headers;

		private static void LoadVorbisHeaders()
		{
			using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Fmod5Sharp.FmodVorbis.vorbis_headers.json") 
			                      ?? throw new Exception($"Embedded resources for vorbis header data not found, has the assembly been tampered with?");
			using StreamReader reader = new(stream);

			var jsonString = reader.ReadToEnd();
			headers = JsonSerializer.Deserialize<Dictionary<uint, byte[]>>(jsonString);
		}
		
		public static unsafe byte[] RebuildOggFile(FmodSample sample)
		{
			var dataChunk = sample.Metadata.Chunks.FirstOrDefault(f => f.ChunkType == FmodSampleChunkType.VORBISDATA);

			if (dataChunk == null)
			{
				throw new Exception("Rebuilding Vorbis data requires a VORBISDATA chunk, which wasn't found");
			}

			var chunkData = (VorbisChunkData)dataChunk.ChunkData;
			var crc32 = chunkData.Crc32;

			if(headers == null)
				LoadVorbisHeaders();
			var vorbisHeader = headers![crc32];

			var info = new vorbis_info();
			
			Vorbis.vorbis_info_init(&info);
			
			var comment = new vorbis_comment();
			var state = new ogg_stream_state();

			Checked(Ogg.ogg_stream_init(&state, 1), nameof(Ogg.ogg_stream_init));

			var idHeader = RebuildIdHeader(sample.Metadata.Channels, (uint)sample.Metadata.Frequency, 0x100, 0x800);
			var commentHeader = RebuildCommentHeader();
			var setupHeader = RebuildSetupHeader(vorbisHeader);

			Checked(Vorbis.vorbis_synthesis_headerin(&info, &comment, &idHeader), nameof(Vorbis.vorbis_synthesis_headerin));
			Checked(Vorbis.vorbis_synthesis_headerin(&info, &comment, &commentHeader), nameof(Vorbis.vorbis_synthesis_headerin));
			Checked(Vorbis.vorbis_synthesis_headerin(&info, &comment, &setupHeader), nameof(Vorbis.vorbis_synthesis_headerin));

			var resultStream = new MemoryStream();
			
			//Write id header
			Checked(Ogg.ogg_stream_packetin(&state, &idHeader), nameof(Ogg.ogg_stream_packetin));
			WritePackets(&state, resultStream );
			
			//Write comment header
			Checked(Ogg.ogg_stream_packetin(&state, &commentHeader), nameof(Ogg.ogg_stream_packetin));
			WritePackets(&state, resultStream);
			
			//Write setup header
			Checked(Ogg.ogg_stream_packetin(&state, &setupHeader), nameof(Ogg.ogg_stream_packetin));
			WritePackets(&state, resultStream);
			
			//Flush stream
			FlushPackets(&state, resultStream);

			var packetNo = setupHeader.packetno;
			long granulePos = 0;
			long prevBlockSize = 0;

			using var inStream = new MemoryStream(sample.SampleBytes);
			using var inReader = new BinaryReader(inStream);

			var packetSize = inReader.ReadUInt16();
			while (packetSize != 0)
			{
				packetNo += 1;

				//Copy packet over from fmod data to output stream
				var packet = new ogg_packet();
				var packetBytes = inReader.ReadBytes(packetSize);

				fixed (byte* ptr = packetBytes) 
					packet.packet = ptr;

				packet.bytes = new CLong(packetSize);
				packet.packetno = packetNo;

				try
				{
					packetSize = inReader.ReadUInt16();
				}
				catch (EndOfStreamException)
				{
					packetSize = 0;
				}

				//End of stream if packet size is now 0
				packet.e_o_s = new CLong(packetSize == 0 ? 1 : 0);

				var blockSize = Vorbis.vorbis_packet_blocksize(&info, &packet);
				if (blockSize == 0)
					throw new Exception("vorbis_packet_blocksize returned size = 0");

				if (prevBlockSize == 0)
					granulePos = 0;
				else
					granulePos += (blockSize + prevBlockSize) / 4;

				packet.granulepos = granulePos;
				prevBlockSize = blockSize;

				//Write packet to output stream
				Checked(Ogg.ogg_stream_packetin(&state, &packet), nameof(Ogg.ogg_stream_packetin));
				WritePackets(&state, resultStream);
			}

			return resultStream.ToArray();

		}

		private static unsafe ogg_packet RebuildIdHeader(uint channels, uint frequency, uint blocksizeShort, uint blocksizeLong)
		{
			var packet = new ogg_packet();
			var buf = new oggpack_buffer();
			
			Ogg.oggpack_writeinit(&buf);
			
			oggpack_write(&buf, 0x01, 8);
			
			foreach (var c in "vorbis".ToCharArray().ToList())
				oggpack_write(&buf, (byte) c, 8);
			
			oggpack_write(&buf, 0, 32);
			oggpack_write(&buf, channels, 8);
			oggpack_write(&buf, frequency, 32);
			oggpack_write(&buf, 0, 32);
			oggpack_write(&buf, 0, 32);
			oggpack_write(&buf, 0, 32);
			
			//Write length of binary representation of number - 1.
			oggpack_write(&buf, (uint)(Convert.ToString(blocksizeShort, 2).Length - 1), 4);
			oggpack_write(&buf, (uint)(Convert.ToString(blocksizeLong, 2).Length - 1), 4);
			
			oggpack_write(&buf, 1, 1);

			//Set number of bytes in packet
			packet.bytes = Ogg.oggpack_bytes(&buf);

			//Obtain the bytes
			var byteBuffer = Bytes(buf.buffer, packet.bytes.Value);

			//Set bytes in packet
			fixed (byte* ptr = byteBuffer) 
				packet.packet = ptr;

			packet.b_o_s = new CLong(1);
			packet.e_o_s = new CLong(0);
			packet.granulepos = 0;
			packet.packetno = 0;
			
			Ogg.oggpack_writeclear(&buf);

			return packet;
		}

		private static unsafe ogg_packet RebuildCommentHeader()
		{
			var packet = new ogg_packet();
			Ogg.ogg_packet_clear(&packet);

			var comment = new vorbis_comment();
			Vorbis.vorbis_comment_init(&comment);
			
			Checked(Vorbis.vorbis_commentheader_out(&comment, &packet), nameof(Vorbis.vorbis_commentheader_out));

			return packet;
		}

		private static unsafe ogg_packet RebuildSetupHeader(byte[] buffer)
		{
			var packet = new ogg_packet();
			
			//Set bytes in packet
			fixed (byte* ptr = buffer) 
				packet.packet = ptr;

			packet.bytes = new CLong(buffer.Length);
			packet.b_o_s = new CLong(0);
			packet.e_o_s = new CLong(0);
			packet.granulepos = 0;
			packet.packetno = 2;

			return packet;
		}

		private static unsafe void WritePackets(ogg_stream_state* state, Stream stream)
		{
			var page = new ogg_page();
			while (Ogg.ogg_stream_pageout(state, &page) != 0)
			{
				stream.Write(Bytes(page.header, page.header_len.Value));
				stream.Write(Bytes(page.body, page.body_len.Value));
			}
		}
		
		private static unsafe void FlushPackets(ogg_stream_state* state, Stream stream)
		{
			var page = new ogg_page();
			while (Ogg.ogg_stream_flush(state, &page) != 0)
			{
				stream.Write(Bytes(page.header, page.header_len.Value));
				stream.Write(Bytes(page.body, page.body_len.Value));
			}
		}

		private static unsafe byte[] Bytes(byte* ptr, nint count)
		{
			var ret = new byte[count];
			Marshal.Copy(new IntPtr(ptr), ret, 0, (int) count);
			return ret;
		}

		private static void Checked(int result, string name)
		{
			if (result != 0)
				throw new Exception($"{name} threw error code {result}");
		}

		private static unsafe void oggpack_write(oggpack_buffer* buf, uint value, int bits) => Ogg.oggpack_write(buf, new CULong(value), bits);
	}
}