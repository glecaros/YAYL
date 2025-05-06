using System;

namespace YAYL.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class YamlDerivedTypeAttribute(string typeName, Type derivedType) : Attribute
{
    public string TypeName { get; } = typeName;
    public Type DerivedType { get; } = derivedType;

    public void Deconstruct(out string typeName, out Type derivedType)
    {
        typeName = TypeName;
        derivedType = DerivedType;
    }
}
