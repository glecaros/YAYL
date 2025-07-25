using System;

namespace YAYL.Conversion;

internal class EnumConverter : TypeConverter<Enum>
{
    public EnumConverter() : base((s, t) => (Enum.TryParse(t, NormalizeEnumValueName(s), true, out var result), (Enum?)result))
    {
    }

    public override bool CanConvert(Type targetType) => targetType.IsEnum;

    private static string NormalizeEnumValueName(string value) =>
        value.Replace("-", "").Replace("_", "").ToLowerInvariant();
}
