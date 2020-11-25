/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GSerialize
{
    public sealed class Packer
    {
        private readonly Stream _stream;
        private readonly byte[] _16BytesBuffer = new byte[16];
        private readonly UTF8Encoding _encoding = Encoding.UTF8 as UTF8Encoding;
        private readonly byte[] _stringBuffer = new byte[1024];
        public Packer(Stream stream)
        {
            this._stream = stream;
        }

        public Guid ReadGuid()
        {
            return ByteConverter.ToGuid(ReadNBytes(_16BytesBuffer, 16));
        }

        public async Task<Guid> ReadGuidAsync(CancellationToken cancellation)
        {
            return ByteConverter.ToGuid(await ReadNBytesAsync(_16BytesBuffer, 16, cancellation));
        }

        public void WriteGuid(Guid value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            _stream.Write(_16BytesBuffer, 0, 16);
        }

        public Task WriteGuidAsync(Guid value, CancellationToken cancellation)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            return _stream.WriteAsync(_16BytesBuffer, 0, 16, cancellation);
        }

        public void WriteBool(bool value)
        {
            _stream.WriteByte(value ? (byte)1 : (byte)0);
        }

        public Task WriteBoolAsync(bool value, CancellationToken cancellation)
        {
            _16BytesBuffer[0] = value ? (byte)1 : (byte)0;
            return _stream.WriteAsync(_16BytesBuffer, 0, 1, cancellation);
        }

        public bool ReadBool()
        {
            return ReadNBytes(_16BytesBuffer, 1)[0] == 1;
        }

        public async Task<bool> ReadBoolAsync(CancellationToken cancellation)
        {
            return (await ReadNBytesAsync(_16BytesBuffer, 1, cancellation))[0] == 1;
        }

        public void WriteByte(byte value)
        {
            _16BytesBuffer[0] = value;
            _stream.Write(_16BytesBuffer, 0, 1);
        }

        public Task WriteByteAsync(byte value, CancellationToken cancellation)
        {
            _16BytesBuffer[0] = value;
            return _stream.WriteAsync(_16BytesBuffer, 0, 1, cancellation);
        }

        public byte ReadByte()
        {
            ReadNBytes(_16BytesBuffer, 1);
            return _16BytesBuffer[0];
        }

        public async Task<Byte> ReadByteAsync(CancellationToken cancellation)
        {
            await ReadNBytesAsync(_16BytesBuffer, 1, cancellation);
            return _16BytesBuffer[0];
        }

        public void WriteSByte(SByte value)
        {
            _16BytesBuffer[0] = (byte)value;
            _stream.Write(_16BytesBuffer, 0, 1);
        }

        public Task WriteSByteAsync(SByte value, CancellationToken cancellation)
        {
            _16BytesBuffer[0] = (byte)value;
            return _stream.WriteAsync(_16BytesBuffer, 0, 1, cancellation);
        }

        public SByte ReadSByte()
        {
            ReadNBytes(_16BytesBuffer, 1);
            return (SByte)_16BytesBuffer[0];
        }

        public async Task<SByte> ReadSByteAsync(CancellationToken cancellation)
        {
            await ReadNBytesAsync(_16BytesBuffer, 1, cancellation);
            return (SByte)_16BytesBuffer[0];
        }

        public void WriteString(String value)
        {
            var len = _encoding.GetByteCount(value);
            var bytes = len <= _stringBuffer.Length ? _stringBuffer : new byte[len];
            _encoding.GetBytes(value, 0, value.Length, bytes, 0);
            WriteInt32(len);
            _stream.Write(bytes, 0, len);
        }

        public async Task WriteStringAsync(String value, CancellationToken cancellation)
        {
            var len = _encoding.GetByteCount(value);
            var bytes = len <= _stringBuffer.Length ? _stringBuffer : new byte[len];
            _encoding.GetBytes(value, 0, value.Length, bytes, 0);
            await WriteInt32Async(len, cancellation);
            await _stream.WriteAsync(bytes, 0, len, cancellation);
        }

        public String ReadString()
        {
            var len = ReadInt32();
            if (len > 0)
            {
                var bytes = len <= _stringBuffer.Length ? _stringBuffer : new byte[len];
                return Encoding.UTF8.GetString(ReadNBytes(bytes, len), 0, len);
            }
            else
            {
                return "";
            }
        }

        public async Task<String> ReadStringAsync(CancellationToken cancellation)
        {
            var len = await ReadInt32Async(cancellation);
            if (len > 0)
            {
                var bytes = len <= _stringBuffer.Length ? _stringBuffer : new byte[len];
                return Encoding.UTF8.GetString(await ReadNBytesAsync(bytes, len, cancellation), 0, len);
            }
            else
            {
                return "";
            }
        }

        public int ReadInt32()
        {
            return ByteConverter.ToInt32(ReadNBytes(_16BytesBuffer, 4));
        }

        public async Task<int> ReadInt32Async(CancellationToken cancellation)
        {
            return ByteConverter.ToInt32(await ReadNBytesAsync(_16BytesBuffer, 4, cancellation));
        }

        public void WriteInt32(int value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            _stream.Write(_16BytesBuffer, 0, 4);
        }

        public Task WriteInt32Async(int value, CancellationToken cancellation)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            return _stream.WriteAsync(_16BytesBuffer, 0, 4, cancellation);
        }

        public UInt32 ReadUInt32()
        {
            return ByteConverter.ToUInt32(ReadNBytes(_16BytesBuffer, 4));
        }

        public async Task<UInt32> ReadUInt32Async(CancellationToken cancellation)
        {
            return ByteConverter.ToUInt32(await ReadNBytesAsync(_16BytesBuffer, 4, cancellation));
        }

        public void WriteUInt32(UInt32 value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            _stream.Write(_16BytesBuffer, 0, 4);
        }

        public Task WriteUInt32Async(UInt32 value, CancellationToken cancellation)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            return _stream.WriteAsync(_16BytesBuffer, 0, 4, cancellation);
        }

        public void WriteInt64(Int64 value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            _stream.Write(_16BytesBuffer, 0, 8);
        }

        public Task WriteInt64Async(Int64 value, CancellationToken cancellation)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            return _stream.WriteAsync(_16BytesBuffer, 0, 8, cancellation);
        }

        public Int64 ReadInt64()
        {
            return ByteConverter.ToInt64(ReadNBytes(_16BytesBuffer, 8));
        }

        public async Task<Int64> ReadInt64Async(CancellationToken cancellation)
        {
            return ByteConverter.ToInt64(await ReadNBytesAsync(_16BytesBuffer, 8, cancellation));
        }

        public UInt64 ReadUInt64()
        {
            return ByteConverter.ToUInt64(ReadNBytes(_16BytesBuffer, 8));
        }

        public async Task<UInt64> ReadUInt64Async(CancellationToken cancellation)
        {
            return ByteConverter.ToUInt64(await ReadNBytesAsync(_16BytesBuffer, 8, cancellation));
        }

        public void WriteUInt64(UInt64 value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            _stream.Write(_16BytesBuffer, 0, 8);
        }

        public Task WriteUInt64Async(UInt64 value, CancellationToken cancellation)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            return _stream.WriteAsync(_16BytesBuffer, 0, 8, cancellation);
        }

        public void WriteInt16(Int16 value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            _stream.Write(_16BytesBuffer, 0, 2);
        }

        public Task WriteInt16Async(Int16 value, CancellationToken cancellation)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            return _stream.WriteAsync(_16BytesBuffer, 0, 2, cancellation);
        }


        public Int16 ReadInt16()
        {
            return ByteConverter.ToInt16(ReadNBytes(_16BytesBuffer, 2));
        }

        public async Task<Int16> ReadInt16Async(CancellationToken cancellation)
        {
            return ByteConverter.ToInt16(await ReadNBytesAsync(_16BytesBuffer, 2, cancellation));
        }

        public void WriteUInt16(UInt16 value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            _stream.Write(_16BytesBuffer, 0, 2);
        }

        public Task WriteUInt16Async(UInt16 value, CancellationToken cancellation)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            return _stream.WriteAsync(_16BytesBuffer, 0, 2, cancellation);
        }

        public UInt16 ReadUInt16()
        {
            return ByteConverter.ToUInt16(ReadNBytes(_16BytesBuffer, 2));
        }

        public async Task<UInt16> ReadUInt16Async(CancellationToken cancellation)
        {
            return ByteConverter.ToUInt16(await ReadNBytesAsync(_16BytesBuffer, 2, cancellation));
        }

        public void WriteDouble(double value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            _stream.Write(_16BytesBuffer, 0, 8);
        }

        public Task WriteDoubleAsync(double value, CancellationToken cancellation)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            return _stream.WriteAsync(_16BytesBuffer, 0, 8, cancellation);
        }

        public double ReadDouble()
        {
            return ByteConverter.ToDouble(ReadNBytes(_16BytesBuffer, 8));
        }

        public async Task<double> ReadDoubleAsync(CancellationToken cancellation)
        {
            return ByteConverter.ToDouble(await ReadNBytesAsync(_16BytesBuffer, 8, cancellation));
        }

        public void WriteFloat(float value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            _stream.Write(_16BytesBuffer, 0, 4);
        }

        public Task WriteFloatAsync(float value, CancellationToken cancellation)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            return _stream.WriteAsync(_16BytesBuffer, 0, 4, cancellation);
        }

        public float ReadFloat()
        {
            return ByteConverter.ToFloat(ReadNBytes(_16BytesBuffer, 4));
        }

        public async Task<float> ReadFloatAsync(CancellationToken cancellation)
        {
            return ByteConverter.ToFloat(await ReadNBytesAsync(_16BytesBuffer, 4, cancellation));
        }

        public void WriteDateTime(DateTime value)
        {
            WriteInt64(value.Ticks);
        }

        public Task WriteDateTimeAsync(DateTime value, CancellationToken cancellation)
        {
            return WriteInt64Async(value.Ticks, cancellation);
        }

        public DateTime ReadDateTime()
        {
            return new DateTime(ticks: ReadInt64());
        }

        public async Task<DateTime> ReadDateTimeAsync(CancellationToken cancellation)
        {
            return new DateTime(ticks: await ReadInt64Async(cancellation));
        }

        public void WriteTimeSpan(TimeSpan value)
        {
            WriteInt64(value.Ticks);
        }

        public Task WriteTimeSpanAsync(TimeSpan value, CancellationToken cancellation)
        {
            return WriteInt64Async(value.Ticks, cancellation);
        }

        public TimeSpan ReadTimeSpan()
        {
            return new TimeSpan(ticks: ReadInt64());
        }

        public async Task<TimeSpan> ReadTimeSpanAsync(CancellationToken cancellation)
        {
            return new TimeSpan(ticks: await ReadInt64Async(cancellation));
        }

        public void WriteChar(Char value)
        {
            ByteConverter.GetBytes((UInt16)value, _16BytesBuffer);
            _stream.Write(_16BytesBuffer, 0, 2);
        }

        public Task WriteCharAsync(Char value, CancellationToken cancellation)
        {
            ByteConverter.GetBytes((UInt16)value, _16BytesBuffer);
            return _stream.WriteAsync(_16BytesBuffer, 0, 2, cancellation);
        }

        public Char ReadChar()
        {
            return (Char)ByteConverter.ToUInt16(ReadNBytes(_16BytesBuffer, 2));
        }

        public async Task<Char> ReadCharAsync(CancellationToken cancellation)
        {
            return (Char)ByteConverter.ToUInt16(await ReadNBytesAsync(_16BytesBuffer, 2, cancellation));
        }

        public void WriteDecimal(Decimal value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            _stream.Write(_16BytesBuffer, 0, 16);
        }

        public Task WriteDecimalAsync(Decimal value, CancellationToken cancellation)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            return _stream.WriteAsync(_16BytesBuffer, 0, 16, cancellation);
        }

        public Decimal ReadDecimal()
        {
            return ByteConverter.ToDecimal(ReadNBytes(_16BytesBuffer, 16));
        }

        public async Task<Decimal> ReadDecimalAsync(CancellationToken cancellation)
        {
            return ByteConverter.ToDecimal(await ReadNBytesAsync(_16BytesBuffer, 16, cancellation));
        }

        public void WriteIPEndPoint(IPEndPoint value)
        {
            var addressBytes = value.Address.GetAddressBytes();
            WriteInt32(addressBytes.Length);
            _stream.Write(addressBytes, 0, addressBytes.Length);
            WriteInt32(value.Port);
        }

        public async Task WriteIPEndPointAsync(IPEndPoint value, CancellationToken cancellation)
        {
            var addressBytes = value.Address.GetAddressBytes();
            await WriteInt32Async(addressBytes.Length, cancellation);
            await _stream.WriteAsync(addressBytes, 0, addressBytes.Length, cancellation);
            await WriteInt32Async(value.Port, cancellation);
        }

        public IPEndPoint ReadIPEndPoint()
        {
            var addressLen = ReadInt32();
            var addressBytes = new byte[addressLen];
            ReadNBytes(addressBytes, addressLen);
            var address = new IPAddress(addressBytes);
            var port = ReadInt32();
            return new IPEndPoint(address, port);
        }

        public async Task<IPEndPoint> ReadIPEndPointAsync(CancellationToken cancellation)
        {
            var addressLen = await ReadInt32Async(cancellation);
            var addressBytes = new byte[addressLen];
            await ReadNBytesAsync(addressBytes, addressLen, cancellation);
            var address = new IPAddress(addressBytes);
            var port = await ReadInt32Async(cancellation);
            return new IPEndPoint(address, port);
        }

        public byte[] ReadNBytes(byte[] bytes, int count)
        {
            int readBytes = 0;
            while (count > 0)
            {
                var n = _stream.Read(bytes, readBytes, count);
                if (n == 0) throw new EndOfStreamException();
                readBytes += n;
                count -= n;
            }
            return bytes;
        }

        public async Task<byte[]> ReadNBytesAsync(
            byte[] bytes, int count, 
            CancellationToken cancellation)
        {
            int readBytes = 0;
            while (count > 0)
            {
                var n = await _stream.ReadAsync(bytes, readBytes, count, cancellation);
                if (n == 0) throw new EndOfStreamException();
                readBytes += n;
                count -= n;
            }
            return bytes;
        }

        public void WriteNBytes(byte[] buffer, int offset, int count)
        {
            _stream.Write(buffer, offset, count);
        }

        public Task WriteNBytesAsync(byte[] buffer, int offset, int count, CancellationToken cancellation)
        {
            return _stream.WriteAsync(buffer, offset, count, cancellation);
        }
    }
}
