/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System;
using System.Collections.Generic;
using System.Net;

namespace GSerialize
{    
    sealed class PrimitiveMethod
    {
        private static Dictionary<Type, SerialMethods> PrimitiveTypeMethodsMap = new Dictionary<Type, SerialMethods>();

        static PrimitiveMethod()
        {
            PrimitiveTypeMethodsMap[typeof(Boolean)] = new SerialMethods
            {
                Read = typeof(Packer).GetMethod("ReadBool"),
                Write = typeof(Packer).GetMethod("WriteBool"),
                ReadAsync = typeof(Packer).GetMethod("ReadBoolAsync"),
                WriteAsync = typeof(Packer).GetMethod("WriteBoolAsync"),
            };

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

            PrimitiveTypeMethodsMap[typeof(IPEndPoint)] = new SerialMethods
            {
                Read = typeof(Packer).GetMethod("ReadIPEndPoint"),
                Write = typeof(Packer).GetMethod("WriteIPEndPoint"),
                ReadAsync = typeof(Packer).GetMethod("ReadIPEndPointAsync"),
                WriteAsync = typeof(Packer).GetMethod("WriteIPEndPointAsync"),
            };
        }

        internal static SerialMethods TryGetMethods(Type key)
        {
            PrimitiveTypeMethodsMap.TryGetValue(key, out SerialMethods packerMethods);
            return packerMethods;
        }
    }
}