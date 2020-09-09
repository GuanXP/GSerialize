using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GSerialize
{
    public sealed class Packer
    {
        private readonly Stream _stream;
        private readonly byte[] _16BytesBuffer = new byte[16];
        private readonly byte[] _guidBuffer = new byte[16];
        public Packer(Stream stream)
        {
            this._stream = stream;
        }

        public Guid ReadGuid()
        {
            return new Guid(ReadNBytes(_guidBuffer, 16));
        }

        public void WriteGuid(Guid value)
        {
            _stream.Write(value.ToByteArray(), 0, 16);
        }

        public void WriteBool(bool value)
        {
            _stream.WriteByte(value ? (byte)1 : (byte)0);
        }

        public bool ReadBool()
        {
            return ReadNBytes(_16BytesBuffer, 1)[0] == 1;
        }

        public void WriteString(String value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            WriteInt32(bytes.Length);
            _stream.Write(bytes, 0, bytes.Length);
        }

        public String ReadString()
        {
            var len = ReadInt32();
            if (len > 0)
            {
                var bytes = new byte[len];
                return Encoding.UTF8.GetString(ReadNBytes(bytes, len), 0, len);
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

        public void WriteInt32(int value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            _stream.Write(_16BytesBuffer, 0, 4);
        }

        public UInt32 ReadUInt32()
        {
            return ByteConverter.ToUInt32(ReadNBytes(_16BytesBuffer, 4));
        }

        public void WriteUInt32(UInt32 value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            _stream.Write(_16BytesBuffer, 0, 4);
        }

        public void WriteInt64(Int64 value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            _stream.Write(_16BytesBuffer, 0, 8);
        }

        public Int64 ReadInt64()
        {
            return ByteConverter.ToInt64(ReadNBytes(_16BytesBuffer, 8));
        }

        public UInt64 ReadUInt64()
        {
            return ByteConverter.ToUInt64(ReadNBytes(_16BytesBuffer, 8));
        }

        public void WriteUInt64(UInt64 value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            _stream.Write(_16BytesBuffer, 0, 8);
        }

        public void WriteInt16(Int16 value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            _stream.Write(_16BytesBuffer, 0, 2);
        }

        public Int16 ReadInt16()
        {
            return ByteConverter.ToInt16(ReadNBytes(_16BytesBuffer, 2));
        }

        public void WriteUInt16(UInt16 value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            _stream.Write(_16BytesBuffer, 0, 2);
        }

        public UInt16 ReadUInt16()
        {
            return ByteConverter.ToUInt16(ReadNBytes(_16BytesBuffer, 2));
        }

        public void WriteDouble(double value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            _stream.Write(_16BytesBuffer, 0, 8);
        }

        public double ReadDouble()
        {
            return ByteConverter.ToDouble(ReadNBytes(_16BytesBuffer, 8));
        }

        public void WriteFloat(float value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            _stream.Write(_16BytesBuffer, 0, 4);
        }

        public float ReadFloat()
        {
            return ByteConverter.ToFloat(ReadNBytes(_16BytesBuffer, 4));
        }

        public void WriteDateTime(DateTime value)
        {
            WriteInt64(value.Ticks);
        }

        public DateTime ReadDateTime()
        {
            return new DateTime(ticks: ReadInt64());
        }

        public void WriteChar(Char value)
        {
            ByteConverter.GetBytes((UInt16)value, _16BytesBuffer);
            _stream.Write(_16BytesBuffer, 0, 2);
        }

        public Char ReadChar()
        {
            return (Char)ByteConverter.ToUInt16(ReadNBytes(_16BytesBuffer, 2));
        }

        public void WriteDecimal(Decimal value)
        {
            ByteConverter.GetBytes(value, _16BytesBuffer);
            _stream.Write(_16BytesBuffer, 0, 16);
        }

        public Decimal ReadDecimal()
        {
            return ByteConverter.ToDecimal(ReadNBytes(_16BytesBuffer, 16));
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
    }
}
