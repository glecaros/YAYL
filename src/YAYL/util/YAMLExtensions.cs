using System;
using YamlDotNet.RepresentationModel;

namespace YAYL.Util;

internal static class YAMLExtensions
{
    public static string Location(this YamlNode node)
    {
        return $"{node.Start.Line}:{node.Start.Column}";
    }

    public static bool ContainsField(this YamlMappingNode mapping, string fieldName)
    {
        return mapping.Children.ContainsKey(new YamlScalarNode(fieldName));
    }
    private static readonly string[] ValidNullValues = ["~", "null", "NULL", "Null", "", " "];

    public static string? GetValue(this YamlScalarNode node)
    {
        if (node.Value is not null)
        {
            return Array.Exists(ValidNullValues, v => v == node.Value) ? null : node.Value;
        }
        return node.Value;
    }
}