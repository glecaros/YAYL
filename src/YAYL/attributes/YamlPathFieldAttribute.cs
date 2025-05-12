using System;
using System.IO;

namespace YAYL.Attributes;

public class YamlPathFieldAttribute(YamlFilePathType pathType = YamlFilePathType.RelativeToFile) : YamlFieldPostProcessingAttribute
{
    public readonly YamlFilePathType PathType = pathType;

    internal override object? Process(object? value, Type fieldType, YamlContext? context)
    {
        if (value == null)
        {
            return null;
        }
        if (value is not string stringValue)
        {
            throw new YamlParseException($"The PathFieldAttribute can only be applied to string fields. Found {fieldType.Name}.");
        }
        switch (PathType)
        {
            case YamlFilePathType.RelativeToCurrentDirectory:
            {
                return Path.GetFullPath(Path.Combine(context?.WorkingDirectory ?? Environment.CurrentDirectory, stringValue));

            }
            case YamlFilePathType.RelativeToFile:
            {
                if (context?.FilePath == null)
                {
                    throw new YamlParseException($"The RelativeToFile path type can not be used when FilePath is not set in the context.");
                }
                var fileDirectory = Path.GetDirectoryName(context.FilePath);
                if (string.IsNullOrWhiteSpace(fileDirectory))
                {
                    throw new YamlParseException($"The file path {context.FilePath} is invalid.");
                }
                return Path.GetFullPath(Path.Combine(fileDirectory, stringValue));
            }
            default:
                throw new YamlParseException($"Unknown path type: {PathType}.");
        }
    }
}
