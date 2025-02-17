using System;

namespace YAYL;

public class YamlParseException : Exception
{
    public YamlParseException(string message) : base(message) { }
    public YamlParseException(string message, Exception inner) : base(message, inner) { }
}
