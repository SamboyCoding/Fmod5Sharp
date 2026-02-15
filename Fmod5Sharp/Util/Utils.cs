using System;
using System.Runtime.CompilerServices;

namespace Fmod5Sharp.Util
{
    internal static class Utils
    {
        private static readonly sbyte[] SignedNibbles = { 0, 1, 2, 3, 4, 5, 6, 7, -8, -7, -6, -5, -4, -3, -2, -1 };
        internal static sbyte GetHighNibbleSigned(byte value) => SignedNibbles[(value >> 4) & 0xF];
        internal static sbyte GetLowNibbleSigned(byte value) => SignedNibbles[value & 0xF];
        internal static short Clamp(short val, short min, short max) => Math.Max(Math.Min(val, max), min);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static short ClampToShort(int value)
        {
            if (value < short.MinValue) return short.MinValue;
            if (value > short.MaxValue) return short.MaxValue;
            return (short)value;
        }
    }
}