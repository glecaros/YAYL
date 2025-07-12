using System;

namespace YAYL.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class YamlDerivedTypeDefaultAttribute(Type derivedType) : Attribute
{
    public Type DerivedType { get; } = derivedType;
    public string? FieldToTest { get; init; }

    public void Deconstruct(out Type derivedType)
    {
        derivedType = DerivedType;
    }
}
