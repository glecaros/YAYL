using System;
using YamlDotNet.RepresentationModel;
using YAYL.Util;

namespace YAYL;

public class YamlParseException : Exception
{
    internal YamlParseException(string message, YamlNode node)
        : base($"{message} (at {node.Location()})") { }

    internal YamlParseException(string message, YamlNode node, Exception inner)
        : base($"{message} (at {node.Location()})", inner) { }

    public YamlParseException(string message) : base(message) { }
    public YamlParseException(string message, Exception inner) : base(message, inner) { }
}
