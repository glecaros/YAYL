using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;
using YAYL.Attributes;
using YAYL.Reflection;

namespace YAYL;

public class YamlParser(YamlNamingPolicy namingPolicy = YamlNamingPolicy.KebabCaseLower)
{
    private readonly YamlNamingPolicy _namingPolicy = namingPolicy;
    private readonly List<YamlVariableResolver> _variableResolvers = [];

    public void AddVariableResolver(Regex expression, Func<string, CancellationToken, Task<string>> resolver)
    {
        _variableResolvers.Add(new YamlVariableResolver(expression, resolver));
    }

    public void AddVariableResolver(Regex expression, Func<string, string> resolver) => AddVariableResolver(expression, (v, _) => Task.FromResult(resolver(v)));

    private string? NormalizeEnumValueName(string? value) => value?.Replace("-", "").Replace("_", "").ToLowerInvariant();

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
            if (baseType.GetCustomAttribute<YamlDerivedTypeDefaultAttribute>() is {DerivedType: var defaultDerivedType})
            {
                if (defaultDerivedType.IsAbstract)
                {
                    return GetPolymorphicType(node, defaultDerivedType);
                }
                return defaultDerivedType;
            }
            throw new YamlParseException($"Type discriminator '{polymorphicAttribute.TypeDiscriminatorPropertyName}' not found or invalid");
        }

        var typeName = NormalizeEnumValueName(typeNode.Value);
        var derivedTypeAttribute = baseType.GetCustomAttributes<YamlDerivedTypeAttribute>()
            .FirstOrDefault(x => NormalizeEnumValueName(x.TypeName) == typeName);

        if (derivedTypeAttribute is not (var _, var derivedType))
        {
            throw new YamlParseException($"No derived type found for discriminator value '{typeName}'");
        }

        if (derivedType.IsAbstract)
        {
            return GetPolymorphicType(node, derivedType);
        }

        return derivedType;
    }

    private async Task<Dictionary<string, (PropertyInfo PropertyInfo, object? Value)>> GetPropertyValuesAsync(Type type, YamlMappingNode mappingNode, YamlContext? context, CancellationToken cancellationToken)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Dictionary<string, (PropertyInfo PropertyInfo, object? Value)> propertyValues = [];
        foreach (var property in properties)
        {
            var yamlPropertyName = _namingPolicy.GetPropertyName(property);
            var propertyNode = mappingNode.Children
                .FirstOrDefault(x => ((YamlScalarNode)x.Key).Value == yamlPropertyName);

            if (Attribute.IsDefined(property, typeof(YamlIgnoreAttribute)))
            {
                propertyValues[property.Name] = (property, null);
            }
            else if (propertyNode.Value != null)
            {
                var value = await ConvertYamlNodeAsync(propertyNode.Value, property.PropertyType, context, cancellationToken)
                    .ConfigureAwait(false);

                propertyValues[property.Name] = (property, PostProcess(property, value, context));
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

    private static object? PostProcess(PropertyInfo property, object? value, YamlContext? context)
    {
        var attributes = property.GetCustomAttributes<YamlFieldPostProcessingAttribute>();
        object? processedValue = value;
        foreach (var attribute in attributes)
        {
            switch (attribute)
            {
                case YamlPathFieldAttribute pathFieldAttribute:
                {
                    processedValue = pathFieldAttribute.Process(processedValue, property.PropertyType, context);
                    break;
                }

            }
        }
        return processedValue;
    }

    private static T SetPropertyValues<T>(T target, Dictionary<string, (PropertyInfo PropertyInfo, object? Value)> propertyValues)
    {
        foreach (var (_, (property, value)) in propertyValues)
        {
            property.SetValue(target, value);
        }
        return target;
    }

    private async Task<T?> ParseNodeWithDefaultConstructorAsync<T>(YamlNode node, Type type, YamlContext? context, CancellationToken cancellationToken) where T : class
    {
        if (node is not YamlMappingNode mappingNode)
        {
            throw new YamlParseException($"Expected mapping node for type {type.Name}");
        }

        return SetPropertyValues((T?)Activator.CreateInstance(type), await GetPropertyValuesAsync(type, mappingNode, context, cancellationToken).ConfigureAwait(false));
    }

    private async Task<T?> ParseNodeWithoutDefaultConstructorAsync<T>(YamlNode node, Type type, ConstructorInfo constructorInfo, YamlContext? context, CancellationToken cancellationToken) where T : class
    {
        if (node is not YamlMappingNode mappingNode)
        {
            throw new YamlParseException($"Expected mapping node for type {type.Name}");
        }

        var parameters = constructorInfo.GetParameters();
        var args = new object?[parameters.Length];
        var missingRequired = new List<string>();

        var propertyValues = await GetPropertyValuesAsync(type, mappingNode, context, cancellationToken).ConfigureAwait(false);

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

        if (missingRequired.Count != 0)
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

    public async Task<T?> ParseFileAsync<T>(string yamlFilePath, YamlContext? context = null, CancellationToken cancellationToken = default) where T : class
    {
        if (!File.Exists(yamlFilePath))
        {
            throw new YamlParseException($"File not found: {yamlFilePath}");
        }

        if (context == null || string.IsNullOrWhiteSpace(context.FilePath))
        {
            context = new()
            {
                FilePath = yamlFilePath,
                WorkingDirectory = context?.WorkingDirectory,
            };
        }

        using var reader = new StreamReader(yamlFilePath);
        var yaml = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        return await ParseAsync<T>(yaml, context, cancellationToken).ConfigureAwait(false);
    }

    public T? ParseFile<T>(string yamlFilePath, YamlContext? context = null) where T : class
    {
        return ParseFileAsync<T>(yamlFilePath, context).GetAwaiter().GetResult();
    }

    public Task<T?> ParseAsync<T>(Stream yamlStream, YamlContext? context = null, CancellationToken cancellationToken = default) where T : class
    {
        if (yamlStream == null)
        {
            return Task.FromResult<T?>(null);
        }

        using var reader = new StreamReader(yamlStream);
        return ParseAsync<T>(reader, context, cancellationToken);
    }

    public T? Parse<T>(Stream yamlStream, YamlContext? context = null) where T : class
    {
        return ParseAsync<T>(yamlStream, context).GetAwaiter().GetResult();
    }

    public Task<T?> ParseAsync<T>(string yaml, YamlContext? context = null, CancellationToken cancellationToken = default) where T : class
    {
        if (string.IsNullOrWhiteSpace(yaml))
        {
            return Task.FromResult<T?>(null);
        }

        using var input = new StringReader(yaml);
        return ParseAsync<T>(input, context, cancellationToken);
    }

    private async Task<T?> ParseAsync<T>(TextReader reader, YamlContext? context, CancellationToken cancellationToken) where T : class
    {
        try
        {
            var yamlStream = new YamlStream();
            yamlStream.Load(reader);

            if (!yamlStream.Documents.Any())
            {
                return null;
            }

            var rootNode = yamlStream.Documents[0].RootNode;
            return (T?)await ConvertYamlNodeAsync(rootNode, typeof(T), context, cancellationToken).ConfigureAwait(false);
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

    public T? Parse<T>(string yaml, YamlContext? context = null) where T : class
    {
        return ParseAsync<T>(yaml, context).GetAwaiter().GetResult();
    }

    private async Task<T?> ParseNodeAsync<T>(YamlNode node, YamlContext? context, CancellationToken cancellationToken) where T : class
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
            return await ParseNodeWithoutDefaultConstructorAsync<T>(node, baseType, constructors[0], context, cancellationToken).ConfigureAwait(false);
        }

        return await ParseNodeWithDefaultConstructorAsync<T>(node, baseType, context, cancellationToken).ConfigureAwait(false);
    }

    private async Task<object?> ConvertYamlNodeAsync(YamlNode node, Type targetType, YamlContext? context, CancellationToken cancellationToken)
    {
        switch (node)
        {
            case YamlScalarNode scalarNode:
                {
                    return await ConvertScalarNodeAsync(scalarNode, targetType, cancellationToken).ConfigureAwait(false);
                }
            case YamlSequenceNode sequenceNode:
                {
                    return await ConvertSequenceNodeAsync(sequenceNode, targetType, context, cancellationToken).ConfigureAwait(false);
                }
            case YamlMappingNode mappingNode:
                {
                    if (targetType.IsDictionary())
                    {
                        return await ConvertToDictionaryAsync(mappingNode, targetType, context, cancellationToken).ConfigureAwait(false);
                    }
                    else if (targetType.IsClass)
                    {
                        var task = this.CallGenericMethod<Task>(
                            methodName: nameof(ParseNodeAsync),
                            flags: BindingFlags.NonPublic | BindingFlags.Instance,
                            typeArguments: [targetType],
                            parameters: [mappingNode, context, cancellationToken]
                        );
                        await task.ConfigureAwait(false);

                        var resultProperty = task.GetType().GetProperty("Result");
                        var result = resultProperty?.GetValue(task);
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

    private async Task<string?> ResolveVariablesInStringAsync(string value, CancellationToken cancellationToken)
    {
        foreach (var (expression, resolver) in _variableResolvers)
        {
            var matches = expression.Matches(value);
            foreach (Match match in matches)
            {
                var variable = match.Groups[1].Value;
                var resolvedValue = await resolver(variable, cancellationToken).ConfigureAwait(false);
                value = value.Replace(match.Value, resolvedValue);
            }
        }

        return value;
    }

    private async Task<object?> ConvertScalarNodeAsync(YamlScalarNode node, Type targetType, CancellationToken cancellationToken)
    {
        var value = node.Value switch
        {
            string v => await ResolveVariablesInStringAsync(v, cancellationToken).ConfigureAwait(false),
            _ => node.Value,
        };

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

    private async Task<object> ConvertToArrayAsync(YamlSequenceNode node, Type targetType, YamlContext? context, CancellationToken cancellationToken)
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
                var value = await ConvertYamlNodeAsync(node.Children[i], elementType, context, cancellationToken).ConfigureAwait(false);
                array.SetValue(value, i);
            }
            catch (Exception ex)
            {
                throw new YamlParseException($"Failed to convert array element at index {i}", ex);
            }
        }

        return array;
    }

    private async Task<object> ConvertSequenceNodeAsync(YamlSequenceNode node, Type targetType, YamlContext? context, CancellationToken cancellationToken)
    {
        if (targetType.IsArray)
        {
            return await ConvertToArrayAsync(node, targetType, context, cancellationToken).ConfigureAwait(false);
        }

        if (targetType.GetGenericCollection() is Type genericType)
        {
            return await ConvertToGenericCollectionAsync(node, genericType, targetType, context, cancellationToken).ConfigureAwait(false);
        }

        throw new YamlParseException($"Cannot convert sequence to type: {targetType.Name}");
    }

    private static bool AddToGenericCollection<T>(ICollection<T> collection, T value)
    {
        collection.Add(value);
        return true;
    }

    private async Task<object> ConvertToGenericCollectionAsync(YamlSequenceNode node, Type genericCollectionType, Type targetType, YamlContext? context, CancellationToken cancellationToken)
    {
        var elementType = targetType.GetGenericArguments()[0];
        var collectionType = genericCollectionType.MakeGenericType(elementType);
        var collection = Activator.CreateInstance(collectionType)!;
        foreach (var child in node.Children)
        {
            var value = await ConvertYamlNodeAsync(child, elementType, context, cancellationToken).ConfigureAwait(false);
            this.CallGenericMethod<bool>(
                methodName: nameof(AddToGenericCollection),
                flags: BindingFlags.NonPublic | BindingFlags.Static,
                typeArguments: [elementType],
                parameters: [collection, value]
            );
        }
        return collection;
    }

    private async Task<object> ConvertToDictionaryAsync(YamlMappingNode node, Type type, YamlContext? context, CancellationToken cancellationToken)
    {
        var dict = Activator.CreateInstance(type)!;
        var valueType = type.GenericTypeArguments[1];
        foreach (var child in node.Children)
        {
            if (child.Key is YamlScalarNode keyNode && child.Value is YamlNode valueNode)
            {
                var value = await ConvertYamlNodeAsync(valueNode, valueType, context, cancellationToken).ConfigureAwait(false);
                type.InvokeMember("Add", BindingFlags.InvokeMethod, null, dict, [keyNode.Value!, value]);
            }
            else
            {
                throw new YamlParseException("Dictionary keys must be scalar values");
            }
        }

        return dict;
    }
}
