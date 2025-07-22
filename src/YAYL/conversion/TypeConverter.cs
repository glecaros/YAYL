using System;
using System.Threading;
using YamlDotNet.RepresentationModel;

namespace YAYL.Conversion;

public class TypeConverter<T>(Func<string, Type, T> converter) : ITypeConverter
{
    public Type TargetType => typeof(T);

    public virtual bool CanConvert(Type targetType) => targetType == TargetType;

    private readonly Func<string, Type, T> _converter = converter;

    public object? Convert(string value, Type targetType, YamlNode node, CancellationToken cancellationToken)
    {
        try
        {
            return _converter(value, targetType);
        }
        catch (Exception ex)
        {
            throw new YamlParseException($"Failed to convert '{value}' to type {targetType.Name} ({node.Start.Line}:{node.Start.Column})", node, ex);
        }
    }

}