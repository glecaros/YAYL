using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using YamlDotNet.RepresentationModel;
using YAYL.Attributes;
using YAYL.Reflection;

namespace YAYL;

public class YamlParser
{
    private readonly JsonNamingPolicy _namingPolicy;

    public YamlParser(YamlNamingPolicy namingPolicy = YamlNamingPolicy.KebabCaseLower)
    {
        _namingPolicy = namingPolicy switch
        {
            YamlNamingPolicy.KebabCaseLower => JsonNamingPolicy.KebabCaseLower,
            YamlNamingPolicy.KebabCaseUpper => JsonNamingPolicy.KebabCaseUpper,
            YamlNamingPolicy.CamelCase => JsonNamingPolicy.CamelCase,
            YamlNamingPolicy.SnakeCaseLower => JsonNamingPolicy.SnakeCaseLower,
            YamlNamingPolicy.SnakeCaseUpper => JsonNamingPolicy.SnakeCaseUpper,
            _ => throw new ArgumentOutOfRangeException(nameof(namingPolicy), namingPolicy, null),
        };
    }

    private string GetYamlPropertyName(MemberInfo member)
    {
        var attribute = member.GetCustomAttribute<YamlPropertyNameAttribute>();
        return attribute?.Name ?? _namingPolicy.ConvertName(member.Name);
    }

    private string? NormalizeEnumValueName(string? value)
    {
        return value?.Replace("-", "").Replace("_", "").ToLowerInvariant();
    }

    private Type? GetPolymorphicType(YamlMappingNode node, Type baseType)
    {
        var polymorphicAttribute = baseType.GetCustomAttribute<YamlPolymorphicAttribute>();
        if (polymorphicAttribute == null)
        {
            return null;
        }

        var discriminatorNode = node.Children
            .FirstOrDefault(x => ((YamlScalarNode)x.Key).Value == polymorphicAttribute.TypeDiscriminatorPropertyName);

        if (discriminatorNode.Value is not YamlScalarNode typeNode)
        {
            throw new YamlParseException($"Type discriminator '{polymorphicAttribute.TypeDiscriminatorPropertyName}' not found or invalid");
        }

        var typeName = NormalizeEnumValueName(typeNode.Value);
        var derivedTypeAttribute = baseType.GetCustomAttributes<YamlDerivedTypeAttribute>()
            .FirstOrDefault(x => NormalizeEnumValueName(x.TypeName) == typeName);

        if (derivedTypeAttribute == null)
        {
            throw new YamlParseException($"No derived type found for discriminator value '{typeName}'");
        }

        return derivedTypeAttribute.DerivedType;
    }

    private Dictionary<string, (PropertyInfo PropertyInfo, object? Value)> GetPropertyValues(Type type, YamlMappingNode mappingNode)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Dictionary<string, (PropertyInfo PropertyInfo, object? Value)> propertyValues = new();
        foreach (var property in properties)
        {
            var yamlPropertyName = GetYamlPropertyName(property);
            var propertyNode = mappingNode.Children
                .FirstOrDefault(x => ((YamlScalarNode)x.Key).Value == yamlPropertyName);

            if (Attribute.IsDefined(property, typeof(YamlIgnoreAttribute)))
            {
                propertyValues[property.Name] = (property, null);
            }
            else if (propertyNode.Value != null)
            {
                var value = ConvertYamlNode(propertyNode.Value, property.PropertyType);
                propertyValues[property.Name] = (property, value);
            }
            else if ((Nullable.GetUnderlyingType(property.PropertyType) != null) || property.IsNullableReferenceType())
            {
                propertyValues[property.Name] = (property, null);
            }
            else
            {
                throw new YamlParseException($"Non-nullable property '{yamlPropertyName}' is missing");
            }
        }
        return propertyValues;
    }

    private T SetPropertyValues<T>(T target, Dictionary<string, (PropertyInfo PropertyInfo, object? Value)> propertyValues)
    {
        foreach (var (_, (property, value)) in propertyValues)
        {
            property.SetValue(target, value);
        }
        return target;
    }

    private T? ParseNodeWithDefaultConstructor<T>(YamlNode node, Type type) where T : class
    {
        if (node is not YamlMappingNode mappingNode)
        {
            throw new YamlParseException($"Expected mapping node for type {type.Name}");
        }

        return SetPropertyValues((T?)Activator.CreateInstance(type), GetPropertyValues(type, mappingNode));
    }

    private T? ParseNodeWithoutDefaultConstructor<T>(YamlNode node, Type type, ConstructorInfo constructorInfo) where T : class
    {
        if (node is not YamlMappingNode mappingNode)
        {
            throw new YamlParseException($"Expected mapping node for type {type.Name}");
        }

        var parameters = constructorInfo.GetParameters();
        var args = new object?[parameters.Length];
        var missingRequired = new List<string>();

        var propertyValues = GetPropertyValues(type, mappingNode);

        for (int i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            if (parameter.Name is null)
            {
                throw new YamlParseException($"Parameter name is null for constructor of type {type.Name}");
            }
            if (propertyValues.ContainsKey(parameter.Name))
            {
                var value = propertyValues.GetValueOrDefault(parameter.Name);
                propertyValues.Remove(parameter.Name);
                args[i] = value.Value;
            }
            else if (parameter.HasDefaultValue && parameter.DefaultValue is object defaultValue)
            {
                args[i] = defaultValue;
            }
            else if (Nullable.GetUnderlyingType(parameter.ParameterType) != null)
            {
                args[i] = null;
            }
            else
            {
                missingRequired.Add(parameter.Name);
            }
        }

        if (missingRequired.Any())
        {
            throw new YamlParseException(
                $"Missing required parameters for type {type.Name}: {string.Join(", ", missingRequired)}");
        }

        try
        {
            return SetPropertyValues((T?)Activator.CreateInstance(type, args), propertyValues);
        }
        catch (Exception ex)
        {
            throw new YamlParseException($"Failed to create instance of type {type.Name}", ex);
        }
    }

    public T? Parse<T>(string yaml) where T : class
    {
        if (string.IsNullOrWhiteSpace(yaml))
        {
            return null;
        }

        try
        {
            using var input = new StringReader(yaml);
            var yamlStream = new YamlStream();
            yamlStream.Load(input);

            if (!yamlStream.Documents.Any())
            {
                return null;
            }

            var rootNode = yamlStream.Documents[0].RootNode;
            return (T?)ConvertYamlNode(rootNode, typeof(T));
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
            throw new YamlParseException("Failed to parse YAML document", ex);
        }
        catch (TargetInvocationException ex)
        {
            return ex.InnerException switch
            {
                YamlParseException inner => throw inner,
                _ => throw new YamlParseException("Failed to parse YAML document", ex.InnerException ?? ex)
            };
        }
    }

    private T? ParseNode<T>(YamlNode node) where T : class
    {
        var baseType = typeof(T);

        if (node is YamlMappingNode mappingNode)
        {
            var derivedType = GetPolymorphicType(mappingNode, baseType);
            if (derivedType != null)
            {
                baseType = derivedType;
            }
        }

        var constructors = baseType.GetConstructors();

        if (constructors.Length == 0)
        {
            throw new YamlParseException($"Type {baseType.Name} does not have a public constructor.");
        }

        var defaultConstructor = constructors.FirstOrDefault(c => c.GetParameters().Length == 0);

        if (defaultConstructor is null)
        {
            if (constructors.Length != 1)
            {
                throw new YamlParseException(
                    $"Only types with a single constructor are supported. {baseType.Name} has {constructors.Length} constructors.");
            }
            return ParseNodeWithoutDefaultConstructor<T>(node, baseType, constructors[0]);
        }

        return ParseNodeWithDefaultConstructor<T>(node, baseType);
    }

    private object? ConvertYamlNode(YamlNode node, Type targetType)
    {
        switch (node)
        {
            case YamlScalarNode scalarNode:
                {
                    return ConvertScalarNode(scalarNode, targetType);
                }
            case YamlSequenceNode sequenceNode:
                {
                    return ConvertSequenceNode(sequenceNode, targetType);
                }
            case YamlMappingNode mappingNode:
                {
                    if (targetType.IsDictionary())
                    {
                        return ConvertToDictionary(mappingNode, targetType);
                    }
                    else if (targetType.IsClass)
                    {
                        var method = typeof(YamlParser).GetMethod(nameof(ParseNode),
                            BindingFlags.NonPublic | BindingFlags.Instance)!;
                        var genericMethod = method.MakeGenericMethod(targetType);
                        var result = genericMethod.Invoke(this, [mappingNode]);

                        if (result == null)
                        {
                            throw new YamlParseException($"Failed to parse nested object of type {targetType.Name}");
                        }

                        return result;
                    }
                }
                break;
        }

        throw new YamlParseException($"Unsupported YAML node type for target type {targetType.Name}");
    }

    private object? ConvertScalarNode(YamlScalarNode node, Type targetType)
    {
        var value = node.Value;

        try
        {
            if (targetType == typeof(string))
            {
                if (value == "~")
                {
                    return null;
                }
                return value;
            }

            if (Nullable.GetUnderlyingType(targetType) is Type underlyingType)
            {
                if (string.IsNullOrEmpty(value) || value == "~")
                {
                    return null;
                }
                targetType = underlyingType;
            }

            if (string.IsNullOrEmpty(value))
            {
                throw new YamlParseException($"Cannot convert empty string not-nullable type {targetType.Name}");
            }

            if (targetType.IsEnum)
            {
                return Enum.Parse(targetType, NormalizeEnumValueName(value)!, true);
            }

            return targetType switch
            {
                var t when t == typeof(int) => int.Parse(value),
                var t when t == typeof(long) => long.Parse(value),
                var t when t == typeof(double) => double.Parse(value),
                var t when t == typeof(float) => float.Parse(value),
                var t when t == typeof(decimal) => decimal.Parse(value),
                var t when t == typeof(bool) => bool.Parse(value),
                var t when t == typeof(Guid) => Guid.Parse(value),
                var t when t == typeof(DateTime) => DateTime.Parse(value),
                var t when t == typeof(DateTimeOffset) => DateTimeOffset.Parse(value),
                var t when t == typeof(TimeSpan) => TimeSpan.Parse(value),
                var t when TypeDescriptor.GetConverter(t).CanConvertFrom(typeof(string)) => TypeDescriptor.GetConverter(t).ConvertFrom(value),
                _ => new YamlParseException($"Unsupported scalar type: {targetType.Name}"),
            };
        }
        catch (Exception ex) when (ex is not YamlParseException)
        {
            throw new YamlParseException($"Failed to convert '{value}' to type {targetType.Name}", ex);
        }
    }

    private object ConvertToArray(YamlSequenceNode node, Type targetType)
    {
        if (!targetType.IsArray)
        {
            throw new YamlParseException($"Cannot convert sequence to non-array type: {targetType.Name}");
        }

        var elementType = targetType.GetElementType()!;
        var array = Array.CreateInstance(elementType, node.Children.Count);

        for (int i = 0; i < node.Children.Count; i++)
        {
            try
            {
                array.SetValue(ConvertYamlNode(node.Children[i], elementType), i);
            }
            catch (Exception ex)
            {
                throw new YamlParseException($"Failed to convert array element at index {i}", ex);
            }
        }

        return array;
    }

    private object ConvertSequenceNode(YamlSequenceNode node, Type targetType)
    {
        if (targetType.IsArray)
        {
            return ConvertToArray(node, targetType);
        }

        if (IsGenericCollection(targetType))
        {
            return ConvertToGenericCollection(node, targetType);
        }

        throw new YamlParseException($"Cannot convert sequence to type: {targetType.Name}");
    }

    private bool IsGenericCollection(Type type)
    {
        return type.IsGenericType && (
            type.GetGenericTypeDefinition() == typeof(List<>) ||
            type.GetGenericTypeDefinition() == typeof(IList<>) ||
            type.GetGenericTypeDefinition() == typeof(ICollection<>) ||
            type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
        );
    }

    private object ConvertToGenericCollection(YamlSequenceNode node, Type targetType)
    {
        var elementType = targetType.GetGenericArguments()[0];
        var listType = typeof(List<>).MakeGenericType(elementType);
        var list = (IList)Activator.CreateInstance(listType)!;

        foreach (var child in node.Children)
        {
            list.Add(ConvertYamlNode(child, elementType));
        }

        if (targetType.GetGenericTypeDefinition() == typeof(List<>) ||
            targetType.GetGenericTypeDefinition() == typeof(IList<>))
        {
            return list;
        }

        return list;
    }

    private object ConvertToDictionary(YamlMappingNode node, Type type)
    {
        var dict = Activator.CreateInstance(type)!;
        var valueType = type.GenericTypeArguments[1];
        foreach (var child in node.Children)
        {
            if (child.Key is YamlScalarNode keyNode && child.Value is YamlNode valueNode)
            {
                type.InvokeMember("Add", BindingFlags.InvokeMethod, null, dict, [keyNode.Value!, ConvertYamlNode(valueNode, valueType)]);
            }
            else
            {
                throw new YamlParseException("Dictionary values must be scalar values");
            }
        }

        return dict;
    }
}