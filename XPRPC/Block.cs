/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using GSerialize;
using System;
using System.IO;
using System.Threading.Tasks;

namespace XPRPC
{
    public enum BlockKey
    {
        ObjectCall = 3,
        ObjectReply = 4,
        ObjectEvent = 5
    }

    public abstract class InteractiveBlock
    {
        public Stream DataStream { get; set; }
        public abstract Task CallChannel(DataChannel channel);        
        protected abstract void WriteHeader(Packer packer);
        public abstract BlockKey Key { get; }
        public abstract InteractiveBlock ReadFromStream(Stream stream);

        public void WriteToStream(Stream stream)
        {
            var packer = new Packer(stream);
            WriteHeader(packer);
            DataStream.Seek(0, SeekOrigin.Begin);
            DataStream.CopyTo(stream);
        }
    }

    public class BlockObjectCall : InteractiveBlock
    {
        public Int32 RequestID;
        public Int16 ObjectID; // -1 means an global call without object
        public Int16 MethodID;

        public override BlockKey Key => BlockKey.ObjectCall;

        public override Task CallChannel(DataChannel channel)
        {
            return channel.OnObjectCall(this);
        }

        protected override void WriteHeader(Packer packer)
        {
            packer.WriteInt32(RequestID);
            packer.WriteInt16(ObjectID);
            packer.WriteInt16(MethodID);
        }

        public override InteractiveBlock ReadFromStream(Stream stream)
        {
            var packer = new Packer(stream);
            return new BlockObjectCall
            {
                RequestID = packer.ReadInt32(),
                ObjectID = packer.ReadInt16(),
                MethodID = packer.ReadInt16(),
                DataStream = stream
            };
        }
    }

    class BlockObjectReply : InteractiveBlock
    {
        public Int32 RequestID;
        public bool Success;

        public override BlockKey Key => BlockKey.ObjectReply;

        public override Task CallChannel(DataChannel channel)
        {
            return channel.OnObjectReply(this);
        }

        protected override void WriteHeader(Packer packer)
        {
            packer.WriteInt32(RequestID);
            packer.WriteBool(Success);
        }

        public override InteractiveBlock ReadFromStream(Stream stream)
        {
            var packer = new Packer(stream);
            return new BlockObjectReply
            {
                RequestID = packer.ReadInt32(),
                Success = packer.ReadBool(),
                DataStream = stream
            };
        }

        public static BlockObjectReply BuildFromException(Exception exception, Int32 requestID)
        {
            var memStream = new MemoryStream();
            var packer = new Packer(memStream);
            packer.WriteString(exception.Message);
            memStream.Seek(0, SeekOrigin.Begin);
            return new BlockObjectReply
            {
                RequestID = requestID,
                Success = false,
                DataStream = memStream
            };
        }
    }

    public class BlockObjectEvent : InteractiveBlock
    {
        public Int16 ObjectID;
        public Int16 EventID;

        public override BlockKey Key => BlockKey.ObjectEvent;

        public override Task CallChannel(DataChannel channel)
        {
            return channel.OnObjectEvent(this);
        }

        protected override void WriteHeader(Packer packer)
        {
            packer.WriteInt16(ObjectID);
            packer.WriteInt16(EventID);
        }

        public override InteractiveBlock ReadFromStream(Stream stream)
        {
            var packer = new Packer(stream);
            return new BlockObjectEvent
            {
                ObjectID = packer.ReadInt16(),
                EventID = packer.ReadInt16(),
                DataStream = stream
            };
        }
    }
}