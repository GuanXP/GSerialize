/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using GSerialize;

namespace Test
{
    [GSerializable]
    public class RefUnit
    {
        public Dictionary<int, RefUnit> Dict1 = new Dictionary<int, RefUnit>();
        public List<RefUnit> List1 = new List<RefUnit>();
        public RefUnit[] Array1 = new RefUnit[3];
        public RefUnit[] Array2;

        public static void UnitTest()
        {
            Console.WriteLine($"{DateTime.Now} Serializer2: start reference testing");
            using var mem = new MemoryStream();
            var serializer = new Serializer2(mem);

            var item1 = new RefUnit();
            item1.Dict1[1] = item1;
            item1.List1.Add(item1);
            item1.Array1[0] = item1;
            item1.Array2 = item1.Array1;

            mem.Seek(0, SeekOrigin.Begin);
            serializer.Serialize(item1);
            mem.Seek(0, SeekOrigin.Begin);
            var item2 = serializer.Deserialize<RefUnit>();

            Debug.Assert(Object.ReferenceEquals(item2.Dict1[1], item2));
            Debug.Assert(Object.ReferenceEquals(item2.List1[0], item2));
            Debug.Assert(Object.ReferenceEquals(item2.Array1[0], item2));
            Debug.Assert(item2.Array1[1] == null);
            Debug.Assert(item2.Array1[2] == null);
            Debug.Assert(Object.ReferenceEquals(item2.Array1, item2.Array2));

            Console.WriteLine($"{DateTime.Now} Serializer2: end reference testing");
        }
    }
}