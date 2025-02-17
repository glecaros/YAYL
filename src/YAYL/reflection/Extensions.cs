using System;
using System.Collections.Generic;
using System.Reflection;

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
}