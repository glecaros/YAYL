using System;
using System.Reflection;
using System.Text.Json;
using YAYL.Attributes;

namespace YAYL;

public enum YamlNamingPolicy
{
    KebabCaseLower,
    KebabCaseUpper,
    CamelCase,
    SnakeCaseLower,
    SnakeCaseUpper
}

internal static class YamlNamingPolicyExtensions
{
    public static JsonNamingPolicy ToJsonNamingPolicy(this YamlNamingPolicy namingPolicy) => namingPolicy switch
    {
        YamlNamingPolicy.KebabCaseLower => JsonNamingPolicy.KebabCaseLower,
        YamlNamingPolicy.KebabCaseUpper => JsonNamingPolicy.KebabCaseUpper,
        YamlNamingPolicy.CamelCase => JsonNamingPolicy.CamelCase,
        YamlNamingPolicy.SnakeCaseLower => JsonNamingPolicy.SnakeCaseLower,
        YamlNamingPolicy.SnakeCaseUpper => JsonNamingPolicy.SnakeCaseUpper,
        _ => throw new ArgumentOutOfRangeException(nameof(namingPolicy), namingPolicy, null),
    };

    public static string GetPropertyName(this YamlNamingPolicy namingPolicy, MemberInfo member)
    {
        var jsonPolicy = namingPolicy.ToJsonNamingPolicy();
        var attribute = member.GetCustomAttribute<YamlPropertyNameAttribute>();
        return attribute?.Name ?? jsonPolicy.ConvertName(member.Name);
    }

    public static string GetEnumName(this YamlNamingPolicy namingPolicy, Enum value)
    {
        var jsonPolicy = namingPolicy.ToJsonNamingPolicy();
        return jsonPolicy.ConvertName(value.ToString());
    }
}