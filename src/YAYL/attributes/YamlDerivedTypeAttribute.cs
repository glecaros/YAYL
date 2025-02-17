using System;

namespace YAYL.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class YamlDerivedTypeAttribute : Attribute
{
    public string TypeName { get; }
    public Type DerivedType { get; }

    public YamlDerivedTypeAttribute(string typeName, Type derivedType)
    {
        TypeName = typeName;
        DerivedType = derivedType;
    }
}
