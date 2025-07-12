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
}