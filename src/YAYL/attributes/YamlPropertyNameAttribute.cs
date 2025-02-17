using System;

namespace YAYL.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public class YamlPropertyNameAttribute : Attribute
{
    public string Name { get; }

    public YamlPropertyNameAttribute(string name)
    {
        Name = name;
    }
}
