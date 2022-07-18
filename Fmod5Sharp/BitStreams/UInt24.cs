using System;

namespace BitStreams
{
	/// <summary>
	/// Represents a 24-bit unsigned integer
	/// </summary>
	[Serializable]
    internal struct UInt24
    {
        private byte b0, b1, b2;

        private UInt24(uint value)
        {
            this.b0 = (byte)(value & 0xFF);
            this.b1 = (byte)((value >> 8) & 0xFF);
            this.b2 = (byte)((value >> 16) & 0xFF);
        }

        public static implicit operator UInt24(uint value)
        {
            return new UInt24(value);
        }

        public static implicit operator uint (UInt24 i)
        {
            return (uint)(i.b0 | (i.b1 << 8) | (i.b2 << 16));
        }

        public Bit GetBit(int index)
        {
            return (byte)(this >> index);
        }
    }
}
