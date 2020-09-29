
using System;

namespace GSerialize
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited=false)]
    public sealed class NonNullAttribute: Attribute
    {
        
    }
}