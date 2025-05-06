using System;

namespace YAYL.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class YamlDerivedTypeDefaultAttribute(Type derivedType) : Attribute
{
    public Type DerivedType { get; } = derivedType;

    public void Deconstruct(out Type derivedType)
    {
        derivedType = DerivedType;
    }
}
