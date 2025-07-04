using System;

namespace YAYL.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public class YamlVariantTypeScalarAttribute(Type type) : Attribute
{
    public Type Type { get; } = type;
}