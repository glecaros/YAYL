using System;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace YAYL.Conversion;

public interface ITypeConverter
{
    Type TargetType { get; }

    bool CanConvert(Type targetType);

    object? Convert(string value, Type targetType, YamlNode node, CancellationToken cancellationToken);
}
