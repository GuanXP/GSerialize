using System;
using System.Collections.Generic;
using System.Text;

namespace GSerialize
{
    class ByteConverter
    {
        internal unsafe static void GetBytes(Int16 value, byte[] bytes)
        {
            fixed (byte* b = bytes)
                *((Int16*)b) = value;
        }

        internal unsafe static void GetBytes(UInt16 value, byte[] bytes)
        {
            fixed (byte* b = bytes)
                *((UInt16*)b) = value;
        }

        internal unsafe static void GetBytes(Int32 value, byte[] bytes)
        {
            fixed (byte* b = bytes)
                *((Int32*)b) = value;
        }

        internal unsafe static void GetBytes(UInt32 value, byte[] bytes)
        {
            fixed (byte* b = bytes)
                *((UInt32*)b) = value;
        }

        internal unsafe static void GetBytes(Int64 value, byte[] bytes)
        {
            fixed (byte* b = bytes)
                *((Int64*)b) = value;
        }

        internal unsafe static void GetBytes(UInt64 value, byte[] bytes)
        {
            fixed (byte* b = bytes)
                *((UInt64*)b) = value;
        }

        internal unsafe static void GetBytes(Double value, byte[] bytes)
        {
            fixed (byte* b = bytes)
                *((Double*)b) = value;
        }

        internal unsafe static void GetBytes(float value, byte[] bytes)
        {
            fixed (byte* b = bytes)
                *((float*)b) = value;
        }

        internal unsafe static void GetBytes(Decimal value, byte[] bytes)
        {
            fixed (byte* b = bytes)
                *((Decimal*)b) = value;
        }

        internal unsafe static Int16 ToInt16(byte[] bytes)
        {
            fixed (byte* b = bytes) return *((Int16*)b);
        }

        internal unsafe static UInt16 ToUInt16(byte[] bytes)
        {
            fixed (byte* b = bytes) return *((UInt16*)b);
        }

        internal unsafe static Int32 ToInt32(byte[] bytes)
        {
            fixed (byte* b = bytes) return *((Int32*)b);
        }

        internal unsafe static UInt32 ToUInt32(byte[] bytes)
        {
            fixed (byte* b = bytes) return *((UInt32*)b);
        }

        internal unsafe static Int64 ToInt64(byte[] bytes)
        {
            fixed (byte* b = bytes) return *((Int64*)b);
        }

        internal unsafe static UInt64 ToUInt64(byte[] bytes)
        {
            fixed (byte* b = bytes) return *((UInt64*)b);
        }

        internal unsafe static Double ToDouble(byte[] bytes)
        {
            fixed (byte* b = bytes) return *((Double*)b);
        }

        internal unsafe static float ToFloat(byte[] bytes)
        {
            fixed (byte* b = bytes) return *((float*)b);
        }

        internal unsafe static Decimal ToDecimal(byte[] bytes)
        {
            fixed (byte* b = bytes) return *((Decimal*)b);
        }
    }
}
