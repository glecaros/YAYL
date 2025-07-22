using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using YamlDotNet.RepresentationModel;
using YAYL.Attributes;
using YAYL.Reflection;

namespace YAYL;

internal class Visitor : IYamlVisitor
{
    private readonly StringBuilder _stringBuilder = new();
    private int _indentationLevel = -1;
    private string _indentation = string.Empty;

    public void Visit(YamlStream stream)
    {
        foreach (var document in stream.Documents)
        {
            document.Accept(this);
        }
    }

    public void Visit(YamlDocument document)
    {
        document.RootNode.Accept(this);
    }

    public void Visit(YamlScalarNode scalar)
    {
        _stringBuilder.Append(scalar.Value);
    }

    public void Visit(YamlSequenceNode sequence)
    {
        if (_indentationLevel == -1)
        {
            _indentationLevel = 0;
        }

        foreach (var child in sequence.Children)
        {
            _stringBuilder.Append(_indentation);
            _stringBuilder.Append("- ");
            child.Accept(this);
            if (child is YamlScalarNode)
            {
                _stringBuilder.AppendLine();
            }
        }
    }

    public void Visit(YamlMappingNode mapping)
    {
        var originalIndentation = _indentation;
        _indentationLevel += 1;
        _indentation = new string(' ', _indentationLevel * 2);
        bool first = true;
        foreach (var child in mapping.Children)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                _stringBuilder.Append(_indentation);
            }

            _stringBuilder.Append(child.Key.ToString());
            _stringBuilder.Append(":");
            switch (child.Value)
            {
                case YamlScalarNode scalar:
                    _stringBuilder.Append(" ");
                    child.Value.Accept(this);
                    _stringBuilder.AppendLine();
                    break;
                case YamlMappingNode mappingNode:
                    _stringBuilder.AppendLine();
                    _stringBuilder.Append(_indentation);
                    _stringBuilder.Append("  ");
                    child.Value.Accept(this);
                    break;
                case YamlSequenceNode sequenceNode:
                    _stringBuilder.AppendLine();
                    child.Value.Accept(this);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(child.Value), child.Value, null);
            }
        }
        _indentation = originalIndentation;
        _indentationLevel -= 1;
    }

    public override string ToString()
    {
        return _stringBuilder.ToString();
    }
}

public class YamlSerializer(YamlNamingPolicy namingPolicy = YamlNamingPolicy.KebabCaseLower)
{
    private readonly YamlNamingPolicy _namingPolicy = namingPolicy;

    public string Serialize<T>(T obj) where T : notnull
    {
        var rootNode = Serialize(obj: obj, type: typeof(T));
        var document = new YamlDocument(rootNode: rootNode);
        Visitor visitor = new();
        visitor.Visit(document);
        return visitor.ToString();
    }

    private YamlNode SerializeDictionary<TKey, TValue>(Dictionary<TKey, TValue> dict) where TKey : notnull
    {
        var dictionaryNode = new YamlMappingNode();
        foreach (var item in dict)
        {
            var key = item.Key;
            var value = item.Value;
            if (key == null || value == null)
            {
                continue;
            }

            dictionaryNode.Add(
                new YamlScalarNode(key.ToString()),
                Serialize(value, typeof(TValue)));
        }
        return dictionaryNode;
    }

    private YamlNode Serialize(object obj, Type type)
    {
        return obj switch
        {
            null => throw new ArgumentNullException(nameof(obj)),
            _ when type == null => throw new ArgumentNullException(nameof(type)),
            _ when type.IsDictionary() => this.CallGenericMethod<YamlNode>(
                methodName: nameof(SerializeDictionary),
                flags: BindingFlags.NonPublic | BindingFlags.Instance,
                typeArguments: type.GenericTypeArguments,
                parameters: [obj]),
            Guid guid => new YamlScalarNode(guid.ToString()),
            Uri uri => new YamlScalarNode(uri.ToString()),
            TimeSpan timeSpan => new YamlScalarNode(timeSpan.ToString()),
            DateTime dateTime => new YamlScalarNode(dateTime.ToString("o")),
            DateTimeOffset dateTime => new YamlScalarNode(dateTime.ToString("o")),
            string str => new YamlScalarNode(str),
            decimal dec => new YamlScalarNode(dec.ToString()),
            bool boolean => new YamlScalarNode(boolean.ToString().ToLowerInvariant()),
            _ when type.IsPrimitive => new YamlScalarNode(obj.ToString()),
            Enum enumValue => new YamlScalarNode(_namingPolicy.GetEnumName(enumValue)),
            _ when typeof(IEnumerable).IsAssignableFrom(type) => SerializeEnumerable(
                obj: obj,
                type: type),
            _ => SerializeObject(
                obj: obj,
                type: obj.GetType()),
        };
    }

    private YamlNode SerializeEnumerable(object obj, Type type)
    {
        var sequenceNode = new YamlSequenceNode();
        foreach (var item in (IEnumerable)obj)
        {
            if (item == null)
            {
                continue;
            }
            sequenceNode.Add(Serialize(item, item.GetType()));
        }
        return sequenceNode;
    }

    private YamlNode SerializeObject(object obj, Type type)
    {
        var mappingNode = new YamlMappingNode();
        string? discriminatorPropertyName = null;
        if (type.GetCustomAttribute<YamlPolymorphicAttribute>() is YamlPolymorphicAttribute(string discriminatorProperty))
        {
            if (type.GetDerivedTypeAttribute() is YamlDerivedTypeAttribute derivedTypeAttribute)
            {
                var serializedTypeName = derivedTypeAttribute.GetSerializedName(_namingPolicy);
                discriminatorPropertyName = discriminatorProperty;
                mappingNode.Add(
                    new YamlScalarNode(discriminatorPropertyName),
                    new YamlScalarNode(serializedTypeName));
            }
        }

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (Attribute.IsDefined(property, typeof(YamlIgnoreAttribute)))
            {
                continue;
            }

            var yamlPropertyName = _namingPolicy.GetPropertyName(property);
            if (yamlPropertyName == discriminatorPropertyName)
            {
                continue;
            }
            var value = property.GetValue(obj);
            if (value == null)
            {
                continue;
            }
            if (Nullable.GetUnderlyingType(property.PropertyType) is Type underlyingType)
            {
                var hasValue = (bool)property.PropertyType.GetProperty("HasValue")?.GetValue(value)!;
                if (hasValue)
                {
                    var valueProperty = property.PropertyType.GetProperty("Value");
                    if (valueProperty != null)
                    {
                        value = valueProperty.GetValue(value)!;
                        mappingNode.Add(new YamlScalarNode(yamlPropertyName), Serialize(value, underlyingType));
                    }
                }
                else
                {
                    continue;

                }
            }
            else
            {
                mappingNode.Add(new YamlScalarNode(yamlPropertyName), Serialize(value, property.PropertyType));
            }
        }

        return mappingNode;
    }
}
