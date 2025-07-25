using System;

namespace YAYL.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
public class YamlVariantTypeObjectAttribute(Type type, string fieldToTest) : Attribute
{
    public Type Type { get; } = type;
    public string FieldToTest { get; } = fieldToTest;

    public void Deconstruct(out Type type, out string fieldToTest)
    {
        type = Type;
        fieldToTest = FieldToTest;
    }
}
