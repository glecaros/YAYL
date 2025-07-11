using YamlDotNet.RepresentationModel;

namespace YAYL.Util;

internal static class YAMLExtensions
{
    public static string Location(this YamlNode node)
    {
        return $"{node.Start.Line}:{node.Start.Column}";
    }
}