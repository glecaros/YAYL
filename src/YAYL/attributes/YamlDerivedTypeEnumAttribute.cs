using System;

namespace YAYL.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class YamlDerivedTypeEnumAttribute<T>(T typeName, Type derivedType) : YamlDerivedTypeAttribute(typeName.ToString(), derivedType) where T : Enum
{
}
