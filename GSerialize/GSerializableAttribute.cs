using System;
using System.Collections.Generic;
using System.Text;

namespace GSerialize
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class GSerializableAttribute: Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class OptionalAttribute: Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class IgnoredAttribute : Attribute
    {

    }
}
