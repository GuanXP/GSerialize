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
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited=false)]
    public sealed class NonNullAttribute: Attribute
    {
        
    }
}