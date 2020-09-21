using System;
using System.Collections.Generic;
using System.Text;

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
