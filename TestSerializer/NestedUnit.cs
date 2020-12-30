/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System;
using System.Collections.Generic;
using GSerialize;

namespace Test
{
    [Serializable]
    public class NestedUnit
    {
        public Int16 Int16Field = -123;
        public Int32 Int32Field = -123;
        public Int64 Int364Field = -123;
        public UInt16 UInt16Field = 123;
        public UInt32 UInt32Field = 123;
        public UInt64 UInt364Field = 123;
        public Decimal DecimalField = 3.14159265897M;
        public SByte SByteField = -123;
        public Byte ByteField = 123;
        
        public Guid Guid { get; set; } = Guid.NewGuid();
        public double DoubleField = 1.00;
        public float FloatField = 0.57f;
        public List<Int32> ListField = new List<int> { 1, 2, 3, 4 };
        public DateTime DateTimeField = DateTime.Now;
        public Dictionary<int, string> DictField = new Dictionary<int, string>
        {
            { 123, "good" },
            { 456, "hello" }
        };
    }
}