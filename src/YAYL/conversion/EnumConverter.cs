using System;

namespace YAYL.Conversion;

internal class EnumConverter : TypeConverter<Enum>
{
    public EnumConverter() : base((s, t) => (Enum)Enum.Parse(t, NormalizeEnumValueName(s), true))
    {
    }
    public override bool CanConvert(Type targetType) => targetType.IsEnum;

    private static string NormalizeEnumValueName(string value) =>
        value.Replace("-", "").Replace("_", "").ToLowerInvariant();
}
