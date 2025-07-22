using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Threading;
using YamlDotNet.RepresentationModel;

namespace YAYL.Conversion;

internal class TypeConverterFactory
{
    private readonly List<ITypeConverter> _converters =
    [
        new TypeConverter<string>((s, _) => s),
        new TypeConverter<bool>((s, _) => bool.Parse(s)),
        new TypeConverter<int>((s, _) => int.Parse(s)),
        new TypeConverter<long>((s, _) => long.Parse(s)),
        new TypeConverter<double>((s, _) => double.Parse(s)),
        new TypeConverter<float>((s, _) => float.Parse(s)),
        new TypeConverter<decimal>((s, _) => decimal.Parse(s)),
        new TypeConverter<Guid>((s, _) => Guid.Parse(s)),
        new TypeConverter<BigInteger>((s, _) => BigInteger.Parse(s)),
        new TypeConverter<DateTime>((s, _) => DateTime.Parse(s)),
        new TypeConverter<DateTimeOffset>((s, _) => DateTimeOffset.Parse(s)),
        new TypeConverter<TimeSpan>((s, _) => TimeSpan.Parse(s)),
        new EnumConverter(),
    ];

    public object? Convert(string value, Type targetType, YamlNode node, CancellationToken cancellationToken)
    {
        Type actualTargetType = targetType;
        if (Nullable.GetUnderlyingType(targetType) is Type underlyingType)
        {
            actualTargetType = underlyingType;
        }

        var converter = _converters.FirstOrDefault(c => c.CanConvert(actualTargetType));
        if (converter != null)
        {
            return converter.Convert(value, actualTargetType, node, cancellationToken);
        }

        if (TypeDescriptor.GetConverter(actualTargetType).CanConvertFrom(typeof(string)))
        {
            try
            {
                return TypeDescriptor.GetConverter(actualTargetType).ConvertFrom(value);
            }
            catch (Exception ex)
            {
                throw new YamlParseException($"Failed to convert '{value}' to type {targetType.Name} ({node.Start.Line}:{node.Start.Column})", node, ex);
            }
        }

        throw new YamlParseException($"Unsupported scalar type: {targetType.Name}", node);
    }

    public object? ConvertWithTypeInferenceAsync(string value, HashSet<Type>? allowedTypes, YamlNode node, CancellationToken cancellationToken)
    {
        Type[] typesToTry =
        [
            typeof(bool),
            typeof(int),
            typeof(long),
            typeof(double),
            typeof(DateTimeOffset),
            typeof(Guid),
            typeof(string),
        ];

        foreach (var type in typesToTry)
        {
            if (allowedTypes?.Contains(type) ?? true)
            {
                var converter = _converters.FirstOrDefault(c => c.CanConvert(type));
                if (converter is not null)
                {
                    try
                    {
                        return converter.Convert(value, type, node, cancellationToken);
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
        }
        throw new YamlParseException($"Cannot convert scalar value '{value}' to any of the allowed types: {string.Join(", ", allowedTypes?.Select(t => t.Name) ?? [])}", node);
    }
}
