using System;

namespace YAYL.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public class YamlPropertyNameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
