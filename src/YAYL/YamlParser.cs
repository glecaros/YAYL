using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;
using YAYL.Attributes;
using YAYL.Conversion;
using YAYL.Reflection;
using YAYL.Util;

namespace YAYL;

class PropertyProxy : PropertyInfo
{
    private readonly PropertyInfo _propertyInfo;
    private readonly Type _type;

    public PropertyProxy(PropertyInfo propertyInfo, Type type)
    {
        _propertyInfo = propertyInfo;
        _type = type;
    }

    public override PropertyAttributes Attributes => _propertyInfo.Attributes;

    public override bool CanRead => throw new NotImplementedException();

    public override bool CanWrite => throw new NotImplementedException();

    public override Type PropertyType => _type;

    public override Type? DeclaringType => _propertyInfo.DeclaringType;

    public override string Name => _propertyInfo.Name;

    public override Type? ReflectedType => throw new NotImplementedException();

    public override MethodInfo[] GetAccessors(bool nonPublic) => throw new NotImplementedException();

    public override object[] GetCustomAttributes(bool inherit) => _propertyInfo.GetCustomAttributes(inherit);

    public override object[] GetCustomAttributes(Type attributeType, bool inherit) => _propertyInfo.GetCustomAttributes(attributeType, inherit);

    public override MethodInfo? GetGetMethod(bool nonPublic) => _propertyInfo.GetGetMethod(nonPublic);

    public override ParameterInfo[] GetIndexParameters() => _propertyInfo.GetIndexParameters();

    public override MethodInfo? GetSetMethod(bool nonPublic) => throw new NotImplementedException();

    public override object? GetValue(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture) => throw new NotImplementedException();

    public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();

    public override void SetValue(object? obj, object? value, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture) => throw new NotImplementedException();
}

public class YamlParser(YamlNamingPolicy namingPolicy = YamlNamingPolicy.KebabCaseLower)
{
    private readonly YamlNamingPolicy _namingPolicy = namingPolicy;
    private readonly List<YamlVariableResolver> _variableResolvers = [];
    private readonly TypeConverterFactory _typeConverterFactory = new();

    public void AddVariableResolver(Regex expression, Func<string, CancellationToken, Task<string>> resolver)
    {
        _variableResolvers.Add(new YamlVariableResolver(expression, resolver));
    }

    public void AddVariableResolver(Regex expression, Func<string, string> resolver) => AddVariableResolver(expression, (v, _) => Task.FromResult(resolver(v)));

    private static Type? GetPolymorphicType(YamlMappingNode node, Type baseType)
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
            if (baseType.GetCustomAttributes<YamlDerivedTypeDefaultAttribute>().FirstOrDefault(x => x.FieldToTest is null || node.ContainsField(x.FieldToTest)) is { DerivedType: var defaultDerivedType })
            {
                if (defaultDerivedType.IsAbstract)
                {
                    return GetPolymorphicType(node, defaultDerivedType);
                }
                return defaultDerivedType;
            }
            /* If no discriminator is found, but the base type is one of the target types, return it */
            if (baseType.GetCustomAttributes<YamlDerivedTypeAttribute>().Any(a => a.DerivedType == baseType))
            {
                return baseType;
            }
            throw new YamlParseException($"Type discriminator '{polymorphicAttribute.TypeDiscriminatorPropertyName}' not found or invalid.", node);
        }

        var typeName = typeNode.Value?.Replace("-", "").Replace("_", "").ToLowerInvariant();
        var derivedTypeAttribute = baseType.GetCustomAttributes<YamlDerivedTypeAttribute>()
            .FirstOrDefault(x => x.TypeName?.Replace("-", "").Replace("_", "").ToLowerInvariant() == typeName);

        if (derivedTypeAttribute is not (var _, var derivedType))
        {
            throw new YamlParseException($"No derived type found for discriminator value '{typeName}'", node);
        }

        if (derivedType.IsAbstract)
        {
            return GetPolymorphicType(node, derivedType);
        }

        return derivedType;
    }

    private static PropertyInfo? FindExtraFieldsProperty(PropertyInfo[] properties, YamlNode node)
    {
        var foundProperties = properties.Where(p => Attribute.IsDefined(p, typeof(YamlExtraAttribute)));
        return foundProperties.Count() switch
        {
            0 => null,
            1 => foundProperties.Single() switch
            {
                var p when p.PropertyType == typeof(Dictionary<string, object>) || p.PropertyType == typeof(Dictionary<string, object?>) => p,
                _ => throw new YamlParseException($"Property '{foundProperties.Single().Name}' decorated with YamlExtra attribute must be of type Dictionary<string, object> or Dictionary<string, object?>", node),
            },
            _ => throw new YamlParseException($"Only one property can be decorated with YamlExtraAttribute in type {properties[0].DeclaringType?.Name}", node),
        };
    }

    private async Task<object?> GetVariantValueFromScalarAsync(PropertyInfo property, YamlScalarNode scalarNode, CancellationToken cancellationToken)
    {
        var allowedTypes = Attribute.GetCustomAttributes(property)
                                    .OfType<YamlVariantTypeScalarAttribute>()
                                    .Select(attr => attr.Type)
                                    .ToHashSet();
        return await ConvertScalarToObjectAsync(scalarNode, allowedTypes, cancellationToken).ConfigureAwait(false);
    }

    private async Task<object?> GetVariantValueFromMappingAsync(PropertyInfo property, YamlMappingNode mappingNode, YamlContext? context, CancellationToken cancellationToken)
    {
        if (property.PropertyType.IsDictionary())
        {
            var dict = Activator.CreateInstance(property.PropertyType)!;
            var valueType = property.PropertyType.GenericTypeArguments[1];
            if (valueType != typeof(object))
            {
                throw new YamlParseException($"For variant dictionaries, the value type must be 'object', but was '{valueType.Name}'.", mappingNode);
            }
            foreach (var (node, valueNode) in mappingNode.Children)
            {
                if (node is YamlScalarNode keyNode)
                {
                    PropertyProxy propertyProxy = new(
                        property,
                        valueType
                    );
                    var value = await GetVariantPropertyValueAsync(propertyProxy, valueNode, context, cancellationToken).ConfigureAwait(false);
                    property.PropertyType.InvokeMember("Add", BindingFlags.InvokeMethod, null, dict, [keyNode!.Value, value]);
                }
                else
                {
                    throw new YamlParseException("Dictionary keys must be scalar values", mappingNode);
                }
            }

            return dict;
        }
        foreach (var (type, fieldToTest) in Attribute.GetCustomAttributes(property).OfType<YamlVariantTypeObjectAttribute>())
        {
            if (mappingNode.Children.ContainsKey(new YamlScalarNode(fieldToTest)))
            {
                return await ConvertYamlNodeAsync(mappingNode, type, context, cancellationToken).ConfigureAwait(false);
            }
        }
        if (Attribute.GetCustomAttributes(property).OfType<YamlVariantTypeDefaultAttribute>().FirstOrDefault() is { Type: Type defaultType })
        {
            return await ConvertYamlNodeAsync(mappingNode, defaultType, context, cancellationToken).ConfigureAwait(false);
        }
        throw new YamlParseException($"Mapping node for property '{property.Name}' does not contain any of the known variant types.", mappingNode);
    }

    private async Task<object?> GetVariantValueFromSequenceAsync(PropertyInfo property, YamlSequenceNode sequenceNode, YamlContext? context, CancellationToken cancellationToken)
    {
        if (property.PropertyType.IsArray)
        {
            var elementType = property.PropertyType.GetElementType()!;
            if (elementType != typeof(object))
            {
                throw new YamlParseException($"For variant arrays, the element type must be 'object', but was '{elementType.Name}'.", sequenceNode);
            }
            var array = Array.CreateInstance(elementType, sequenceNode.Children.Count);
            foreach (var (childNode, i) in sequenceNode.Children.Select((n, i) => (n, i)))
            {
                array.SetValue(await GetVariantPropertyValueAsync(property, childNode, context, cancellationToken).ConfigureAwait(false), i);
            }
            return array;
        }
        else if (property.PropertyType.GetGenericCollection() is Type genericType)
        {
            var elementType = property.PropertyType.GetGenericArguments()[0];
            if (elementType != typeof(object))
            {
                throw new YamlParseException($"For variant collections, the element type must be 'object', but was '{elementType.Name}'.", sequenceNode);
            }
            var collectionType = genericType.MakeGenericType(typeof(object));
            var collection = Activator.CreateInstance(collectionType)!;
            foreach (var child in sequenceNode.Children)
            {
                var value = await GetVariantPropertyValueAsync(property, child, context, cancellationToken).ConfigureAwait(false);
                this.CallGenericMethod<bool>(
                    methodName: nameof(AddToGenericCollection),
                    flags: BindingFlags.NonPublic | BindingFlags.Static,
                    typeArguments: [typeof(object)],
                    parameters: [collection, value]
                );
            }
            return collection;
        }
        throw new YamlParseException($"Property '{property.Name}' is not a valid collection type for sequence node", sequenceNode);
    }

    private async Task<object?> GetVariantPropertyValueAsync(PropertyInfo property, YamlNode node, YamlContext? context, CancellationToken cancellationToken)
    {
        var variantAttribute = property.GetCustomAttribute<YamlVariantAttribute>();
        if (variantAttribute == null)
        {
            throw new YamlParseException($"Property '{property.Name}' is not decorated with YamlVariantAttribute", node);
        }

        return node switch
        {
            YamlScalarNode scalarNode => await GetVariantValueFromScalarAsync(property, scalarNode, cancellationToken).ConfigureAwait(false),
            YamlMappingNode mappingNode => await GetVariantValueFromMappingAsync(property, mappingNode, context, cancellationToken).ConfigureAwait(false),
            YamlSequenceNode sequenceNode => await GetVariantValueFromSequenceAsync(property, sequenceNode, context, cancellationToken).ConfigureAwait(false),
            _ => throw new YamlParseException($"Unsupported YAML node type for variant property '{property.Name}'", node)
        };
    }

    private async Task<Dictionary<string, (PropertyInfo PropertyInfo, object? Value)>> GetPropertyValuesAsync(Type type, YamlMappingNode mappingNode, YamlContext? context, CancellationToken cancellationToken)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        PropertyInfo? extraProperty = FindExtraFieldsProperty(properties, mappingNode);

        Dictionary<string, (PropertyInfo PropertyInfo, object? Value)> propertyValues = [];
        HashSet<string> processedYamlProperties = [];
        Dictionary<string, object> extraValues = [];

        /* If the type is polymorphic, add the discriminator property to the processed list, as it can be implicit */
        if (type.GetCustomAttribute<YamlPolymorphicAttribute>() is { TypeDiscriminatorPropertyName: string discriminatorPropertyName })
        {
            processedYamlProperties.Add(discriminatorPropertyName);
        }

        foreach (var property in properties)
        {
            var yamlPropertyName = _namingPolicy.GetPropertyName(property);
            var propertyNode = mappingNode.Children
                .FirstOrDefault(x => ((YamlScalarNode)x.Key).Value == yamlPropertyName);

            if (Attribute.IsDefined(property, typeof(YamlIgnoreAttribute)))
            {
                propertyValues[property.Name] = (property, null);
                processedYamlProperties.Add(yamlPropertyName);
            }
            else if (Attribute.IsDefined(property, typeof(YamlExtraAttribute)))
            {
                propertyValues[property.Name] = (property, extraValues);
                processedYamlProperties.Add(yamlPropertyName);
            }
            else if (propertyNode.Value != null)
            {
                if (Attribute.IsDefined(property, typeof(YamlVariantAttribute)))
                {
                    var value = await GetVariantPropertyValueAsync(property, propertyNode.Value, context, cancellationToken).ConfigureAwait(false);
                    propertyValues[property.Name] = (property, value);
                    processedYamlProperties.Add(yamlPropertyName);
                }
                else
                {
                    var value = await ConvertYamlNodeAsync(propertyNode.Value, property.PropertyType, context, cancellationToken)
                        .ConfigureAwait(false);
                    propertyValues[property.Name] = (property, PostProcess(property, value, context));
                    processedYamlProperties.Add(yamlPropertyName);
                }
            }
            else if ((Nullable.GetUnderlyingType(property.PropertyType) != null) || property.IsNullableReferenceType())
            {
                propertyValues[property.Name] = (property, null);
                processedYamlProperties.Add(yamlPropertyName);
            }
            else
            {
                throw new YamlParseException($"Non-nullable property '{yamlPropertyName}' is missing.", mappingNode);
            }
        }

        if (extraProperty != null)
        {
            foreach (var (key, value) in mappingNode.Children)
            {
                if (key is YamlScalarNode { Value: string yamlKey } && value != null)
                {
                    if (!processedYamlProperties.Contains(yamlKey))
                    {
                        var extraValue = await ConvertYamlNodeAsync(value, typeof(object), context, cancellationToken).ConfigureAwait(false);
                        extraValues[yamlKey] = extraValue!;
                    }
                }
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
            throw new YamlParseException($"Expected mapping node for type {type.Name}", node);
        }

        return SetPropertyValues((T?)Activator.CreateInstance(type), await GetPropertyValuesAsync(type, mappingNode, context, cancellationToken).ConfigureAwait(false));
    }

    private async Task<T?> ParseNodeWithoutDefaultConstructorAsync<T>(YamlNode node, Type type, ConstructorInfo constructorInfo, YamlContext? context, CancellationToken cancellationToken) where T : class
    {
        if (node is not YamlMappingNode mappingNode)
        {
            throw new YamlParseException($"Expected mapping node for type {type.Name}", node);
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
                throw new YamlParseException($"Parameter name is null for constructor of type {type.Name}", node);
            }

            var matchingProperty = propertyValues.Keys
                .FirstOrDefault(key => string.Equals(key, parameter.Name, StringComparison.OrdinalIgnoreCase));

            if (matchingProperty != null)
            {
                var value = propertyValues[matchingProperty];
                propertyValues.Remove(matchingProperty);
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
                $"Missing required parameters for type {type.Name}: {string.Join(", ", missingRequired)}", node);
        }

        try
        {
            return SetPropertyValues((T?)Activator.CreateInstance(type, args), propertyValues);
        }
        catch (Exception ex)
        {
            throw new YamlParseException($"Failed to create instance of type {type.Name}", node, ex);
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
            throw new YamlParseException($"Type {baseType.Name} does not have a public constructor.", node);
        }

        var defaultConstructor = constructors.FirstOrDefault(c => c.GetParameters().Length == 0);

        if (defaultConstructor is null)
        {
            if (constructors.Length != 1)
            {
                throw new YamlParseException(
                    $"Only types with a single constructor are supported. {baseType.Name} has {constructors.Length} constructors.", node);
            }
            return await ParseNodeWithoutDefaultConstructorAsync<T>(node, baseType, constructors[0], context, cancellationToken).ConfigureAwait(false);
        }

        return await ParseNodeWithDefaultConstructorAsync<T>(node, baseType, context, cancellationToken).ConfigureAwait(false);
    }

    private async Task<object?> ConvertYamlNodeAsync(YamlNode node, Type targetType, YamlContext? context, CancellationToken cancellationToken)
    {
        if (targetType == typeof(object))
        {
            return node switch
            {
                YamlScalarNode scalarNode => await ConvertScalarToObjectAsync(scalarNode, null, cancellationToken).ConfigureAwait(false),
                YamlSequenceNode sequenceNode => await ConvertSequenceToObjectAsync(sequenceNode, context, cancellationToken).ConfigureAwait(false),
                YamlMappingNode mappingNode => await ConvertMappingToObjectAsync(mappingNode, context, cancellationToken).ConfigureAwait(false),
                _ => throw new YamlParseException($"Unsupported YAML node type: {node.GetType().Name}", node)
            };
        }

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
                            throw new YamlParseException($"Failed to parse nested object of type {targetType.Name}", mappingNode);
                        }

                        return result;
                    }
                }
                break;
        }

        throw new YamlParseException($"Unsupported YAML node type for target type {targetType.Name}", node);
    }

    private async Task<string> ResolveVariablesInStringAsync(string value, CancellationToken cancellationToken)
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

    private async Task<object?> ConvertScalarToObjectAsync(YamlScalarNode node, HashSet<Type>? allowedTypes, CancellationToken cancellationToken)
    {

        if (node.GetValue() is not string value)
        {
            return null;
        }

        value = await ResolveVariablesInStringAsync(value, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return _typeConverterFactory.ConvertWithTypeInferenceAsync(value, allowedTypes, node, cancellationToken);
    }

    private async Task<object> ConvertSequenceToObjectAsync(YamlSequenceNode node, YamlContext? context, CancellationToken cancellationToken)
    {
        var list = new List<object?>();
        foreach (var child in node.Children)
        {
            var value = await ConvertYamlNodeAsync(child, typeof(object), context, cancellationToken).ConfigureAwait(false);
            list.Add(value);
        }
        return list;
    }

    private async Task<object> ConvertMappingToObjectAsync(YamlMappingNode node, YamlContext? context, CancellationToken cancellationToken)
    {
        var dictionary = new Dictionary<string, object?>();
        foreach (var (key, value) in node.Children)
        {
            if (key is YamlScalarNode { Value: string keyValue })
            {
                dictionary[keyValue] = await ConvertYamlNodeAsync(value, typeof(object), context, cancellationToken).ConfigureAwait(false);
            }
        }
        return dictionary;
    }

    private async Task<object?> ConvertScalarNodeAsync(YamlScalarNode node, Type targetType, CancellationToken cancellationToken)
    {
        var value = node.GetValue() switch
        {
            string v => await ResolveVariablesInStringAsync(v, cancellationToken).ConfigureAwait(false),
            null => null,
        };

        try
        {
            if (targetType == typeof(string))
            {
                return value;
            }

            if (Nullable.GetUnderlyingType(targetType) is Type underlyingType)
            {
                if (value is null)
                {
                    return null;
                }
                targetType = underlyingType;
            }

            if (string.IsNullOrEmpty(value))
            {
                throw new YamlParseException($"Cannot convert empty string not-nullable type {targetType.Name}", node);
            }

            return _typeConverterFactory.Convert(value, targetType, node, cancellationToken);
        }
        catch (Exception ex) when (ex is not YamlParseException)
        {
            throw new YamlParseException($"Failed to convert '{value}' to type {targetType.Name} ({node.Start.Line}:{node.Start.Column})", node, ex);
        }
    }

    private async Task<object> ConvertToArrayAsync(YamlSequenceNode node, Type targetType, YamlContext? context, CancellationToken cancellationToken)
    {
        if (!targetType.IsArray)
        {
            throw new YamlParseException($"Cannot convert sequence to non-array type: {targetType.Name}", node);
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
                throw new YamlParseException($"Failed to convert array element at index {i}", node, ex);
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

        throw new YamlParseException($"Cannot convert sequence to type: {targetType.Name}", node);
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
                throw new YamlParseException("Dictionary keys must be scalar values", node);
            }
        }

        return dict;
    }
}
