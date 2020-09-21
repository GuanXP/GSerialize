/*
 * Copyright 2020, Guan Xiaopeng
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
 
using System;

namespace GSerialize
{
    [AttributeUsage(AttributeTargets.Class)]
    /// <summary>
    /// Mark a class serializable for GSerialize
    /// </summary>
    public sealed class GSerializableAttribute: Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    /// <summary>
    /// Mark a reference type class member optional for GSerialize. It can be null or not.
    /// </summary>
    public sealed class OptionalAttribute: Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    /// <summary>
    /// Mark a class member optional for GSerialize. It will be ignored while serializing.
    /// </summary>
    public sealed class IgnoredAttribute : Attribute
    {

    }
}
