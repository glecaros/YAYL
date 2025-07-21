using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using YAYL.Reflection;

namespace YAYL.Attributes;

public class YamlPathFieldAttribute(YamlFilePathType pathType = YamlFilePathType.RelativeToFile) : YamlFieldPostProcessingAttribute
{
    public readonly YamlFilePathType PathType = pathType;

    private string ProcessStringField(string value, YamlContext? context)
    {
        switch (PathType)
        {
            case YamlFilePathType.RelativeToCurrentDirectory:
                {
                    return Path.GetFullPath(Path.Combine(context?.WorkingDirectory ?? Environment.CurrentDirectory, value));
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
                    return Path.GetFullPath(Path.Combine(fileDirectory, value));
                }
            default:
                throw new YamlParseException($"Unknown path type: {PathType}.");
        }
    }

    private object ProcessStringEnumerable(IEnumerable<string> values, Type fieldType, YamlContext? context)
    {
        var genericCollection = fieldType.GetGenericCollection() ??
            throw new YamlParseException($"The PathFieldAttribute can only be applied to string and string collection fields. Found {fieldType.Name}.");

        var collectionType = genericCollection.MakeGenericType(typeof(string));
        var collection = (ICollection<string>?)Activator.CreateInstance(collectionType) ??
            throw new YamlParseException($"The PathFieldAttribute can only be applied to string and string collection fields. Found {fieldType.Name}.");
        foreach (var value in values)
        {
            var processedValue = ProcessStringField(value, context);
            collection.Add(processedValue);
        }
        return collection;
    }

    internal override object? Process(object? value, Type fieldType, YamlContext? context) => value switch
    {
        null => null,
        string stringValue => ProcessStringField(stringValue, context),
        IEnumerable<string> enumerable => ProcessStringEnumerable(enumerable, fieldType, context),
        _ => throw new YamlParseException($"The PathFieldAttribute can only be applied to string and string collection fields. Found {fieldType.Name}.")
    };
}
