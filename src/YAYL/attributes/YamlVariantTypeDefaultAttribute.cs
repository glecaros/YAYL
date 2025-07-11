using System;

namespace YAYL.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public class YamlVariantTypeDefaultAttribute(Type type) : Attribute
{
    public Type Type { get; } = type;

    public void Deconstruct(out Type type) => type = Type;
}
