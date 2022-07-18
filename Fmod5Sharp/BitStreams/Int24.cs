using System;

namespace BitStreams
{
    /// <summary>
    /// Represents a 24-bit signed integer
    /// </summary>
    [Serializable]
    internal struct Int24
    {
        private byte b0, b1, b2;
        private Bit sign;

        private Int24(int value)
        {
            this.b0 = (byte)(value & 0xFF);
            this.b1 = (byte)((value >> 8) & 0xFF);
            this.b2 = (byte)((value >> 16) & 0x7F);
            this.sign = (byte)((value >> 23) & 1);
        }

        public static implicit operator Int24(int value)
        {
            return new Int24(value);
        }

        public static implicit operator int (Int24 i)
        {
            int value = (i.b0 | (i.b1 << 8) | (i.b2 << 16));
            return -(i.sign << 23) + value;
        }

        public Bit GetBit(int index)
        {
            return (this >> index);
        }
    }
}
