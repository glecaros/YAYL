using System;

namespace YAYL.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class YamlPolymorphicAttribute : Attribute
{
    public string TypeDiscriminatorPropertyName { get; }

    public YamlPolymorphicAttribute(string typeDiscriminatorPropertyName)
    {
        TypeDiscriminatorPropertyName = typeDiscriminatorPropertyName;
    }
}