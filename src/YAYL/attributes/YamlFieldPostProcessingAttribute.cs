using System;

namespace YAYL.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public abstract class YamlFieldPostProcessingAttribute : Attribute
{
    internal abstract object? Process(object? value, Type fieldType, YamlContext? context);
}