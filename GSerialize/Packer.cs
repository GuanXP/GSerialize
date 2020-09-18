using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace GSerialize
{
    public sealed class Packer
    {
        private readonly Stream _stream;
        private readonly byte[] _16BytesBuffer = new byte[16];
        private readonly UTF8Encoding _utf8 = Encoding.UTF8 as UTF8Encoding;
        private readonly byte[] _stringBuffer = new byte[1024];
        public Packer(Stream stream)
        {
            this._stream = stream;
        }

        public Guid ReadGuid()
        {
            return ByteConverter.ToGuid(ReadNBytes(_16BytesBuffer, 16));
        }

        public async Task<Guid> ReadGuidAsync()
        {
            return ByteConverter.ToGuid(await ReadNBytesAsync(_16BytesBuffer, 16));
        }

        public void WriteGuid(Guid value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            _stream.Write(_16BytesBuffer, 0, 16);
        }

        public Task WriteGuidAsync(Guid value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            return _stream.WriteAsync(_16BytesBuffer, 0, 16);
        }

        public void WriteBool(bool value)
        {
            _stream.WriteByte(value ? (byte)1 : (byte)0);
        }

        public Task WriteBoolAsync(bool value)
        {
            _16BytesBuffer[0] = value ? (byte)1 : (byte)0;
            return _stream.WriteAsync(_16BytesBuffer, 0, 1);
        }

        public bool ReadBool()
        {
            return ReadNBytes(_16BytesBuffer, 1)[0] == 1;
        }

        public async Task<bool> ReadBoolAsync()
        {
            return (await ReadNBytesAsync(_16BytesBuffer, 1))[0] == 1;
        }

        public void WriteByte(byte value)
        {
            _16BytesBuffer[0] = value;
            _stream.Write(_16BytesBuffer, 0, 1);
        }

        public Task WriteByteAsync(byte value)
        {
            _16BytesBuffer[0] = value;
            return _stream.WriteAsync(_16BytesBuffer, 0, 1);
        }

        public byte ReadByte()
        {
            ReadNBytes(_16BytesBuffer, 1);
            return _16BytesBuffer[0];
        }

        public async Task<Byte> ReadByteAsync()
        {
            await ReadNBytesAsync(_16BytesBuffer, 1);
            return _16BytesBuffer[0];
        }

        public void WriteSByte(SByte value)
        {
            _16BytesBuffer[0] = (byte)value;
            _stream.Write(_16BytesBuffer, 0, 1);
        }

        public Task WriteSByteAsync(SByte value)
        {
            _16BytesBuffer[0] = (byte)value;
            return _stream.WriteAsync(_16BytesBuffer, 0, 1);
        }

        public SByte ReadSByte()
        {
            ReadNBytes(_16BytesBuffer, 1);
            return (SByte)_16BytesBuffer[0];
        }

        public async Task<SByte> ReadSByteAsync()
        {
            await ReadNBytesAsync(_16BytesBuffer, 1);
            return (SByte)_16BytesBuffer[0];
        }

        public void WriteString(String value)
        {
            var len = _utf8.GetByteCount(value);
            var bytes = len <= _stringBuffer.Length ? _stringBuffer : new byte[len];
            _utf8.GetBytes(value, 0, value.Length, bytes, 0);
            WriteInt32(len);
            _stream.Write(bytes, 0, len);
        }

        public async Task WriteStringAsync(String value)
        {
            var len = _utf8.GetByteCount(value);
            var bytes = len <= _stringBuffer.Length ? _stringBuffer : new byte[len];
            _utf8.GetBytes(value, 0, value.Length, bytes, 0);
            await WriteInt32Async(len);
            await _stream.WriteAsync(bytes, 0, len);
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

        public async Task<String> ReadStringAsync()
        {
            var len = await ReadInt32Async();
            if (len > 0)
            {
                var bytes = len <= _stringBuffer.Length ? _stringBuffer : new byte[len];
                return Encoding.UTF8.GetString(await ReadNBytesAsync(bytes, len), 0, len);
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

        public async Task<int> ReadInt32Async()
        {
            return ByteConverter.ToInt32(await ReadNBytesAsync(_16BytesBuffer, 4));
        }

        public void WriteInt32(int value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            _stream.Write(_16BytesBuffer, 0, 4);
        }

        public Task WriteInt32Async(int value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            return _stream.WriteAsync(_16BytesBuffer, 0, 4);
        }

        public UInt32 ReadUInt32()
        {
            return ByteConverter.ToUInt32(ReadNBytes(_16BytesBuffer, 4));
        }

        public async Task<UInt32> ReadUInt32Async()
        {
            return ByteConverter.ToUInt32(await ReadNBytesAsync(_16BytesBuffer, 4));
        }

        public void WriteUInt32(UInt32 value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            _stream.Write(_16BytesBuffer, 0, 4);
        }

        public Task WriteUInt32Async(UInt32 value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            return _stream.WriteAsync(_16BytesBuffer, 0, 4);
        }

        public void WriteInt64(Int64 value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            _stream.Write(_16BytesBuffer, 0, 8);
        }

        public Task WriteInt64Async(Int64 value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            return _stream.WriteAsync(_16BytesBuffer, 0, 8);
        }

        public Int64 ReadInt64()
        {
            return ByteConverter.ToInt64(ReadNBytes(_16BytesBuffer, 8));
        }

        public async Task<Int64> ReadInt64Async()
        {
            return ByteConverter.ToInt64(await ReadNBytesAsync(_16BytesBuffer, 8));
        }

        public UInt64 ReadUInt64()
        {
            return ByteConverter.ToUInt64(ReadNBytes(_16BytesBuffer, 8));
        }

        public async Task<UInt64> ReadUInt64Async()
        {
            return ByteConverter.ToUInt64(await ReadNBytesAsync(_16BytesBuffer, 8));
        }

        public void WriteUInt64(UInt64 value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            _stream.Write(_16BytesBuffer, 0, 8);
        }

        public Task WriteUInt64Async(UInt64 value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            return _stream.WriteAsync(_16BytesBuffer, 0, 8);
        }

        public void WriteInt16(Int16 value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            _stream.Write(_16BytesBuffer, 0, 2);
        }

        public Task WriteInt16Async(Int16 value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            return _stream.WriteAsync(_16BytesBuffer, 0, 2);
        }


        public Int16 ReadInt16()
        {
            return ByteConverter.ToInt16(ReadNBytes(_16BytesBuffer, 2));
        }

        public async Task<Int16> ReadInt16Async()
        {
            return ByteConverter.ToInt16(await ReadNBytesAsync(_16BytesBuffer, 2));
        }

        public void WriteUInt16(UInt16 value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            _stream.Write(_16BytesBuffer, 0, 2);
        }

        public Task WriteUInt16Async(UInt16 value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            return _stream.WriteAsync(_16BytesBuffer, 0, 2);
        }

        public UInt16 ReadUInt16()
        {
            return ByteConverter.ToUInt16(ReadNBytes(_16BytesBuffer, 2));
        }

        public async Task<UInt16> ReadUInt16Async()
        {
            return ByteConverter.ToUInt16(await ReadNBytesAsync(_16BytesBuffer, 2));
        }

        public void WriteDouble(double value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            _stream.Write(_16BytesBuffer, 0, 8);
        }

        public Task WriteDoubleAsync(double value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            return _stream.WriteAsync(_16BytesBuffer, 0, 8);
        }

        public double ReadDouble()
        {
            return ByteConverter.ToDouble(ReadNBytes(_16BytesBuffer, 8));
        }

        public async Task<double> ReadDoubleAsync()
        {
            return ByteConverter.ToDouble(await ReadNBytesAsync(_16BytesBuffer, 8));
        }

        public void WriteFloat(float value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            _stream.Write(_16BytesBuffer, 0, 4);
        }

        public Task WriteFloatAsync(float value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            return _stream.WriteAsync(_16BytesBuffer, 0, 4);
        }

        public float ReadFloat()
        {
            return ByteConverter.ToFloat(ReadNBytes(_16BytesBuffer, 4));
        }

        public async Task<float> ReadFloatAsync()
        {
            return ByteConverter.ToFloat(await ReadNBytesAsync(_16BytesBuffer, 4));
        }

        public void WriteDateTime(DateTime value)
        {
            WriteInt64(value.Ticks);
        }

        public Task WriteDateTimeAsync(DateTime value)
        {
            return WriteInt64Async(value.Ticks);
        }

        public DateTime ReadDateTime()
        {
            return new DateTime(ticks: ReadInt64());
        }

        public async Task<DateTime> ReadDateTimeAsync()
        {
            return new DateTime(ticks: await ReadInt64Async());
        }

        public void WriteTimeSpan(TimeSpan value)
        {
            WriteInt64(value.Ticks);
        }

        public Task WriteTimeSpanAsync(TimeSpan value)
        {
            return WriteInt64Async(value.Ticks);
        }

        public TimeSpan ReadTimeSpan()
        {
            return new TimeSpan(ticks: ReadInt64());
        }

        public async Task<TimeSpan> ReadTimeSpanAsync()
        {
            return new TimeSpan(ticks: await ReadInt64Async());
        }

        public void WriteChar(Char value)
        {
            ByteConverter.GetBytes((UInt16)value, _16BytesBuffer);
            _stream.Write(_16BytesBuffer, 0, 2);
        }

        public Task WriteCharAsync(Char value)
        {
            ByteConverter.GetBytes((UInt16)value, _16BytesBuffer);
            return _stream.WriteAsync(_16BytesBuffer, 0, 2);
        }

        public Char ReadChar()
        {
            return (Char)ByteConverter.ToUInt16(ReadNBytes(_16BytesBuffer, 2));
        }

        public async Task<Char> ReadCharAsync()
        {
            return (Char)ByteConverter.ToUInt16(await ReadNBytesAsync(_16BytesBuffer, 2));
        }

        public void WriteDecimal(Decimal value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            _stream.Write(_16BytesBuffer, 0, 16);
        }

        public Task WriteDecimalAsync(Decimal value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            return _stream.WriteAsync(_16BytesBuffer, 0, 16);
        }

        public Decimal ReadDecimal()
        {
            return ByteConverter.ToDecimal(ReadNBytes(_16BytesBuffer, 16));
        }

        public async Task<Decimal> ReadDecimalAsync()
        {
            return ByteConverter.ToDecimal(await ReadNBytesAsync(_16BytesBuffer, 16));
        }

        private byte[] ReadNBytes(byte[] bytes, int count)
        {
            int readBytes = 0;
            while (count > 0)
            {
                var n = _stream.Read(bytes, readBytes, count);
                if (n == 0) throw new IOException("The input stream closed");
                readBytes += n;
                count -= n;
            }
            return bytes;
        }

        private async Task<byte[]> ReadNBytesAsync(byte[] bytes, int count)
        {
            int readBytes = 0;
            while (count > 0)
            {
                var n = await _stream.ReadAsync(bytes, readBytes, count);
                if (n == 0) throw new IOException("The input stream closed");
                readBytes += n;
                count -= n;
            }
            return bytes;
        }
    }
}
