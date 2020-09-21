
using System;
using System.Collections.Generic;

namespace GSerialize
{    
    class PrimitiveMethod
    {
        private static Dictionary<Type, SerialMethods> PrimitiveTypeMethodsMap = new Dictionary<Type, SerialMethods>();

        static PrimitiveMethod()
        {
            PrimitiveTypeMethodsMap[typeof(Int32)] = new SerialMethods
            {
                Read = typeof(Packer).GetMethod("ReadInt32"),
                Write = typeof(Packer).GetMethod("WriteInt32"),
                ReadAsync = typeof(Packer).GetMethod("ReadInt32Async"),
                WriteAsync = typeof(Packer).GetMethod("WriteInt32Async"),
            };

            PrimitiveTypeMethodsMap[typeof(UInt32)] = new SerialMethods
            {
                Read = typeof(Packer).GetMethod("ReadUInt32"),
                Write = typeof(Packer).GetMethod("WriteUInt32"),
                ReadAsync = typeof(Packer).GetMethod("ReadUInt32Async"),
                WriteAsync = typeof(Packer).GetMethod("WriteUInt32Async"),
            };

            PrimitiveTypeMethodsMap[typeof(Int64)] = new SerialMethods
            {
                Read = typeof(Packer).GetMethod("ReadInt64"),
                Write = typeof(Packer).GetMethod("WriteInt64"),
                ReadAsync = typeof(Packer).GetMethod("ReadInt64Async"),
                WriteAsync = typeof(Packer).GetMethod("WriteInt64Async"),
            };

            PrimitiveTypeMethodsMap[typeof(UInt64)] = new SerialMethods
            {
                Read = typeof(Packer).GetMethod("ReadUInt64"),
                Write = typeof(Packer).GetMethod("WriteUInt64"),
                ReadAsync = typeof(Packer).GetMethod("ReadUInt64Async"),
                WriteAsync = typeof(Packer).GetMethod("WriteUInt64Async"),
            };

            PrimitiveTypeMethodsMap[typeof(Int16)] = new SerialMethods
            {
                Read = typeof(Packer).GetMethod("ReadInt16"),
                Write = typeof(Packer).GetMethod("WriteInt16"),
                ReadAsync = typeof(Packer).GetMethod("ReadInt16Async"),
                WriteAsync = typeof(Packer).GetMethod("WriteInt16Async"),
            };

            PrimitiveTypeMethodsMap[typeof(UInt16)] = new SerialMethods
            {
                Read = typeof(Packer).GetMethod("ReadUInt16"),
                Write = typeof(Packer).GetMethod("WriteUInt16"),
                ReadAsync = typeof(Packer).GetMethod("ReadUInt16Async"),
                WriteAsync = typeof(Packer).GetMethod("WriteUInt16Async"),
            };

            PrimitiveTypeMethodsMap[typeof(Byte)] = new SerialMethods
            {
                Read = typeof(Packer).GetMethod("ReadByte"),
                Write = typeof(Packer).GetMethod("WriteByte"),
                ReadAsync = typeof(Packer).GetMethod("ReadByteAsync"),
                WriteAsync = typeof(Packer).GetMethod("WriteByteAsync"),
            };

            PrimitiveTypeMethodsMap[typeof(SByte)] = new SerialMethods
            {
                Read = typeof(Packer).GetMethod("ReadSByte"),
                Write = typeof(Packer).GetMethod("WriteSByte"),
                ReadAsync = typeof(Packer).GetMethod("ReadSByteAsync"),
                WriteAsync = typeof(Packer).GetMethod("WriteSByteAsync"),
            };

            PrimitiveTypeMethodsMap[typeof(string)] = new SerialMethods
            {
                Read = typeof(Packer).GetMethod("ReadString"),
                Write = typeof(Packer).GetMethod("WriteString"),
                ReadAsync = typeof(Packer).GetMethod("ReadStringAsync"),
                WriteAsync = typeof(Packer).GetMethod("WriteStringAsync"),
            };

            PrimitiveTypeMethodsMap[typeof(Double)] = new SerialMethods
            {
                Read = typeof(Packer).GetMethod("ReadDouble"),
                Write = typeof(Packer).GetMethod("WriteDouble"),
                ReadAsync = typeof(Packer).GetMethod("ReadDoubleAsync"),
                WriteAsync = typeof(Packer).GetMethod("WriteDoubleAsync"),
            };

            PrimitiveTypeMethodsMap[typeof(float)] = new SerialMethods
            {
                Read = typeof(Packer).GetMethod("ReadFloat"),
                Write = typeof(Packer).GetMethod("WriteFloat"),
                ReadAsync = typeof(Packer).GetMethod("ReadFloatAsync"),
                WriteAsync = typeof(Packer).GetMethod("WriteFloatAsync"),
            };

            PrimitiveTypeMethodsMap[typeof(DateTime)] = new SerialMethods
            {
                Read = typeof(Packer).GetMethod("ReadDateTime"),
                Write = typeof(Packer).GetMethod("WriteDateTime"),
                ReadAsync = typeof(Packer).GetMethod("ReadDateTimeAsync"),
                WriteAsync = typeof(Packer).GetMethod("WriteDateTimeAsync"),
            };

            PrimitiveTypeMethodsMap[typeof(TimeSpan)] = new SerialMethods
            {
                Read = typeof(Packer).GetMethod("ReadTimeSpan"),
                Write = typeof(Packer).GetMethod("WriteTimeSpan"),
                ReadAsync = typeof(Packer).GetMethod("ReadTimeSpanAsync"),
                WriteAsync = typeof(Packer).GetMethod("WriteTimeSpanAsync"),
            };

            PrimitiveTypeMethodsMap[typeof(Guid)] = new SerialMethods
            {
                Read = typeof(Packer).GetMethod("ReadGuid"),
                Write = typeof(Packer).GetMethod("WriteGuid"),
                ReadAsync = typeof(Packer).GetMethod("ReadGuidAsync"),
                WriteAsync = typeof(Packer).GetMethod("WriteGuidAsync"),
            };

            PrimitiveTypeMethodsMap[typeof(Char)] = new SerialMethods
            {
                Read = typeof(Packer).GetMethod("ReadChar"),
                Write = typeof(Packer).GetMethod("WriteChar"),
                ReadAsync = typeof(Packer).GetMethod("ReadCharAsync"),
                WriteAsync = typeof(Packer).GetMethod("WriteCharAsync"),
            };

            PrimitiveTypeMethodsMap[typeof(Decimal)] = new SerialMethods
            {
                Read = typeof(Packer).GetMethod("ReadDecimal"),
                Write = typeof(Packer).GetMethod("WriteDecimal"),
                ReadAsync = typeof(Packer).GetMethod("ReadDecimalAsync"),
                WriteAsync = typeof(Packer).GetMethod("WriteDecimalAsync"),
            };
        }

        internal static SerialMethods TryGetMethods(Type key)
        {
            PrimitiveTypeMethodsMap.TryGetValue(key, out SerialMethods packerMethods);
            return packerMethods;
        }
    }
}