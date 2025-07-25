using System;

namespace YAYL.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class YamlPolymorphicAttribute(string typeDiscriminatorPropertyName) : Attribute
{
    public string TypeDiscriminatorPropertyName { get; } = typeDiscriminatorPropertyName;

    public void Deconstruct(out string typeDiscriminatorPropertyName)
    {
        typeDiscriminatorPropertyName = TypeDiscriminatorPropertyName;
    }
}
