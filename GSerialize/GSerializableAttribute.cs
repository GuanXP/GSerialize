using System;
using System.Collections.Generic;
using System.Text;

namespace GSerialize
{
    [AttributeUsage(AttributeTargets.Class)]
    public class GSerializableAttribute: Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class OptionalAttribute: Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class IgnoredAttribute : Attribute
    {

    }
}
