using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading;
using YamlDotNet.RepresentationModel;

namespace YAYL.Conversion;

internal class TypeConverterFactory
{
    private readonly List<ITypeConverter> _converters =
    [
        new TypeConverter<string>((s, _) => (true, s)),
        new TypeConverter<bool>((s, _) => (bool.TryParse(s, out var v), v)),
        new TypeConverter<int>((s, _) => (int.TryParse(s, out var v), v)),
        new TypeConverter<long>((s, _) => (long.TryParse(s, out var v), v)),
        new TypeConverter<double>((s, _) => (double.TryParse(s, out var v), v)),
        new TypeConverter<float>((s, _) => (float.TryParse(s, out var v), v)),
        new TypeConverter<decimal>((s, _) => (decimal.TryParse(s, out var v), v)),
        new TypeConverter<Guid>((s, _) => (Guid.TryParse(s, out var v), v)),
        new TypeConverter<BigInteger>((s, _) => (BigInteger.TryParse(s, out var v), v)),
        new TypeConverter<DateTime>((s, _) => (DateTime.TryParse(s, out var v), v)),
        new TypeConverter<DateTimeOffset>((s, _) => (DateTimeOffset.TryParse(s, out var v), v)),
        new TypeConverter<TimeSpan>((s, _) => (TimeSpan.TryParse(s, out var v), v)),
        new TypeConverter<Regex>((s, _) => (true, new Regex(s))),
        new EnumConverter(),
    ];

    public object? Convert(string value, Type targetType, YamlNode node)
    {
        Type actualTargetType = targetType;
        if (Nullable.GetUnderlyingType(targetType) is Type underlyingType)
        {
            actualTargetType = underlyingType;
        }

        var converter = _converters.FirstOrDefault(c => c.CanConvert(actualTargetType));
        if (converter != null)
        {
            if (converter.TryConvert(value, actualTargetType, node, out var result))
            {
                return result;
            }
            else
            {
                throw new YamlParseException($"Failed to convert '{value}' to type {targetType.Name} ({node.Start.Line}:{node.Start.Column})", node);
            }
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
                    if (converter.TryConvert(value, type, node, out var result))
                    {
                        return result;
                    }
                }
            }
        }
        throw new YamlParseException($"Cannot convert scalar value '{value}' to any of the allowed types: {string.Join(", ", allowedTypes?.Select(t => t.Name) ?? [])}", node);
    }
}
