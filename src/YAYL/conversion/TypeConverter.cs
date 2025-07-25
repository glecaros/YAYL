using System;
using System.Threading;
using YamlDotNet.RepresentationModel;

namespace YAYL.Conversion;

public class TypeConverter<T>(Func<string, Type, (bool, T?)> converter) : ITypeConverter
{

    public Type TargetType => typeof(T);

    public virtual bool CanConvert(Type targetType) => targetType == TargetType;

    private readonly Func<string, Type, (bool, T?)> _converter = converter;

    public bool TryConvert(string value, Type targetType, YamlNode node, out object? result)
    {
        var (success, typedResult) = _converter(value, targetType);
        if (success)
        {
            result = typedResult;
            return true;
        }
        result = null;
        return false;
    }
}
