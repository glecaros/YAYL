using System;

namespace YAYL.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public class YamlIgnoreAttribute : Attribute
{
}
