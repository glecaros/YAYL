using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using YAYL.Attributes;

namespace YAYL.Reflection;

internal static class Extensions
{
    public static bool IsDictionary(this Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);

    private static NullabilityInfoContext _nullabilityInfoContext = new();

    public static bool IsNullableReferenceType(this PropertyInfo property)
    {
        var nullabilityInfo = _nullabilityInfoContext.Create(property);
        return nullabilityInfo.WriteState == NullabilityState.Nullable;
    }

    public static T CallGenericMethod<T>(this object obj, string methodName, BindingFlags flags, Type[] typeArguments, object?[]? parameters)
    {
        var genericMethod = obj.GetType()
            .GetMethod(methodName, flags);
        if (genericMethod == null)
        {
            throw new InvalidOperationException($"Method '{methodName}' not found on type '{obj.GetType().FullName}'.");
        }
        var constructedMethod = genericMethod.MakeGenericMethod(typeArguments);
        var result = constructedMethod.Invoke(obj, parameters);
        if (result is T typedResult)
        {
            return typedResult;
        }
        throw new InvalidOperationException($"Method '{methodName}' did not return the expected type '{typeof(T).FullName}'.");
    }

    public static YamlDerivedTypeAttribute? GetDerivedTypeAttribute(this Type type)
    {
        return type.GetCustomAttributes<YamlDerivedTypeAttribute>(inherit: true)
            .FirstOrDefault(x => x.DerivedType == type);
    }

    public static string GetSerializedName(this YamlDerivedTypeAttribute attribute, YamlNamingPolicy namingPolicy)
    {
        var attributeType = attribute.GetType();
        if (attributeType.IsGenericType && attributeType.GetGenericTypeDefinition() == typeof(YamlDerivedTypeEnumAttribute<>))
        {
            var enumValue = Enum.Parse(attributeType.GenericTypeArguments[0], attribute.TypeName);
            return namingPolicy.GetEnumName((Enum)enumValue);
        }
        return attribute.TypeName;
    }

    public static bool IsEnumDiscriminator(this YamlDerivedTypeAttribute attribute)
    {
        var attributeType = attribute.GetType();
        return attributeType.IsGenericType && attributeType.GetGenericTypeDefinition() == typeof(YamlDerivedTypeEnumAttribute<>);
    }

    public static Type? GetGenericCollection(this Type type) => type.IsGenericType switch
    {
        true => type.GetGenericTypeDefinition() switch
        {
            var t when t == typeof(List<>) => t,
            var t when t == typeof(IList<>) => typeof(List<>),
            var t when t == typeof(ICollection<>) => typeof(List<>),
            var t when t == typeof(IEnumerable<>) => typeof(List<>),
            var t when t == typeof(ISet<>) => typeof(HashSet<>),
            var t when t == typeof(HashSet<>) => typeof(HashSet<>),
            var t when t == typeof(SortedSet<>) => typeof(SortedSet<>),
            _ => null
        },
        false => null,
    };
}