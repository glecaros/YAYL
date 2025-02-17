using System;

namespace YAYL.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class YamlDerivedTypeEnumAttribute<T> : YamlDerivedTypeAttribute where T : Enum
{
    public YamlDerivedTypeEnumAttribute(T typeName, Type derivedType) : base(typeName.ToString().ToLowerInvariant(), derivedType)
    {
    }
}
