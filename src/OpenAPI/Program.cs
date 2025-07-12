using System.ComponentModel;
using System.Numerics;
using System.Reflection;
using YAYL;
using YAYL.Attributes;

// const string path = "/workspaces/sorento_sdk/openai-in-typespec/external-specs/latest.yaml";
// const string path = "/workspaces/sorento_sdk/tools/OpenApiToTsp/OpenApiTinyDOM/test.yaml";
const string path = "/workspaces/YAYL/src/OpenAPI/lala.yaml";
// const string path = "/workspaces/sorento_sdk/tools/OpenApiToTsp/OpenApiTinyDOM/openapi.documented.yml";

YamlParser parser = new YamlParser(YamlNamingPolicy.CamelCase);

var doc = parser.ParseFile<OpenAPIDocument>(path);

if (doc == null)
{
    Console.WriteLine("Failed to parse the OpenAPI document.");
    return;
}

doc.Validate();

// foreach (var (pathStr, pathEntry) in doc.Paths)
// {
//     Console.WriteLine($"Path: {pathStr}");
//     if (pathEntry.Get is OpenAPIMethod getMethod)
//     {
//         foreach (var parameter in getMethod.Parameters ?? [])
//         {
//             Console.WriteLine($"  Parameter: {parameter.Name}, Location: {parameter.In}, Required: {parameter.Required}");
//                 Console.WriteLine($"    Schema Type: {parameter.Schema?.GetType().Name}");
//         }
//     }
// }

public record OpenAPIContact(
    string? Name,
    string? Url,
    string? Email,
    [property: YamlExtra] Dictionary<string, object?>? Extra
);

public record OpenAPILicense(
    string Name,
    string? Url,
    string? Identifier,
    [property: YamlExtra] Dictionary<string, object?>? Extra
);

public record OpenAPIInfo(
    string Title,
    string Version,
    string? Description,
    string? TermsOfService,
    OpenAPIContact? Contact,
    OpenAPILicense? License,
    [property: YamlExtra] Dictionary<string, object?>? Extra
);

public enum ParameterLocation
{
    Query,
    Header,
    Path,
    Cookie
}

public record OpenAPISchemaDiscriminator(
    string PropertyName,
    [property: YamlExtra] Dictionary<string, object?>? Extra
);


[YamlPolymorphic("type")]
[YamlDerivedType("integer", typeof(OpenAPISchemaInteger))]
[YamlDerivedType("number", typeof(OpenAPISchemaNumber))]
[YamlDerivedType("string", typeof(OpenAPISchemaString))]
[YamlDerivedType("object", typeof(OpenAPISchemaObject))]
[YamlDerivedType("array", typeof(OpenAPISchemaArray))]
[YamlDerivedType("boolean", typeof(OpenAPISchemaBoolean))]
[YamlDerivedType("null", typeof(OpenAPISchemaNull))]
[YamlDerivedTypeDefault(typeof(OpenAPISchemaObject), FieldToTest = "properties")]
[YamlDerivedTypeDefault(typeof(OpenAPISchemaArray), FieldToTest = "items")]
[YamlDerivedTypeDefault(typeof(OpenAPISchema))]
public record OpenAPISchema()
{
    public string? Title { get; init; }
    public string? Description { get; init; }

    [YamlPropertyName("$ref")]
    public string? Ref { get; init; }

    [YamlPropertyName("$recursiveRef")]
    public string? RecursiveRef { get; init; }

    public List<OpenAPISchema>? OneOf { get; init; }

    public List<OpenAPISchema>? AllOf { get; init; }

    public List<OpenAPISchema>? AnyOf { get; init; }

    public bool? Nullable { get; init; }

    [YamlExtra]
    public Dictionary<string, object>? Extra { get; init; }

    public bool? Deprecated { get; init; }

    public OpenAPISchemaDiscriminator? Discriminator { get; init; }

    public object? Example { get; init; }

    public int? MinItems { get; init; }
    public int? MaxItems { get; init; }

    public object? Default { get; init; }
}

public record OpenAPISchemaNull() : OpenAPISchema{};

public record OpenAPISchemaInteger(
// [property: YamlPropertyName("format")] string? Format,
// [property: YamlPropertyName("minimum")] int? Minimum,
// [property: YamlPropertyName("maximum")] int? Maximum,
// [property: YamlExtra] Dictionary<string, object?>? Extra
) : OpenAPISchema
{
    public string? Format { get; init; }
    public BigInteger? Minimum { get; init; }
    public BigInteger? Maximum { get; init; }
    // [property: YamlExtra] Dictionary<string, object?>? Extra
};

public record OpenAPISchemaNumber(

    float? Minimum,
    float? Maximum

) : OpenAPISchema
{
    public string? Format { get; init; }
    public bool? ExclusiveMinimum { get; init; }

}

public record OpenAPISchemaString(
    List<string>? Enum,
    string? Format,
    int? MinLength,
    int? MaxLength
// [property: YamlPropertyName("minLength")] int? MinLength = null,
// [property: YamlPropertyName("enum")] List<string>? Enum,
// [property: YamlPropertyName("pattern")] string? Pattern,
// [property: YamlExtra] Dictionary<string, object>? Extra
) : OpenAPISchema
{
}

public record OpenAPISchemaBoolean : OpenAPISchema
{
}


public record OpenAPIAdditionalPropertiesEmpty(
    [property: YamlExtra] Dictionary<string, object>? Extra
);

public record OpenAPISchemaObject(
    Dictionary<string, OpenAPISchema>? Properties,
    [property:
        YamlVariant,
        YamlVariantTypeScalar(typeof(bool)),
        YamlVariantTypeDefault(typeof(OpenAPISchema))
    ]
    object? AdditionalProperties,

    List<string>? Required,
    int? MaxProperties

) : OpenAPISchema
{
    public OpenAPISchemaString? PropertyNames { get; init; }
    public bool? UnevaluatedProperties { get; init; }
    [YamlPropertyName("$recursiveAnchor")]
    public bool? RecursiveAnchor { get; init; }
}

public record OpenAPISchemaArray(
    OpenAPISchema Items
// [property: YamlPropertyName("minItems")] int? MinItems = null,
// [property: YamlPropertyName("maxItems")] int? MaxItems = null
// [property: YamlExtra] Dictionary<string, object?>? Extra
) : OpenAPISchema
{
    public bool? Optional { get; init; }
}




public record OpenAPIMethodParameters(
    string Name,
    ParameterLocation In,
    string? Description,
    bool? Required,
    OpenAPISchema? Schema,
    string? Style,
    bool? Explode,
    bool? AllowReserved,
    [property: YamlExtra] Dictionary<string, object?> Extra
);

public record OpenAPIMediaTypeObject(

    OpenAPISchema? Schema,
    [property: YamlExtra] Dictionary<string, object?> Extra
);

public record OpenAPIMethodRequestBody(
    string? Description,
    bool? Required,
    Dictionary<string, OpenAPIMediaTypeObject> Content,
    [property: YamlExtra] Dictionary<string, object?> Extra
);

public record OpenAPIHeader(
    string? Description,
    bool? Required,
    OpenAPISchema? Schema,
    [property: YamlExtra] Dictionary<string, object?> Extra
);

public record OpenAPIMethodResponse(
    string? Description,
    Dictionary<string, OpenAPIMediaTypeObject>? Content,
    Dictionary<string, OpenAPIHeader>? Headers,
    [property: YamlExtra] Dictionary<string, object?> Extra
);

public record OpenAPIMethod(
    string? Summary,
    string? Description,
    string? OperationId,
    List<string>? Tags,
    List<OpenAPIMethodParameters>? Parameters,
    OpenAPIMethodRequestBody? RequestBody,
    Dictionary<string, OpenAPIMethodResponse> Responses,
    [property: YamlExtra] Dictionary<string, object?> Extra
);

public record OpenAPIPath(
    string? Summary,
    string? Description,
    OpenAPIMethod? Get,
    OpenAPIMethod? Post,
    OpenAPIMethod? Put,
    OpenAPIMethod? Delete,
    OpenAPIMethod? Options,
    OpenAPIMethod? Head,
    OpenAPIMethod? Patch,
    OpenAPIMethod? Trace,
    [property: YamlExtra] Dictionary<string, object?> Extra
);

public record OpenAPIServer(
    string Url,
    [property: YamlExtra] Dictionary<string, object?>? Extra
);

public record OpenAPITag(
    string Name,
    string? Description,
    [property: YamlExtra] Dictionary<string, object?> Extra
);

public record OpenAPIComponents(
    Dictionary<string, OpenAPISchema>? Schemas,
    [property: YamlExtra] Dictionary<string, object?> Extra
);

public record OpenAPIDocument(
    [property: YamlPropertyName("openapi")] string OpenApiVersion,
    OpenAPIInfo? Info,
    List<OpenAPIServer>? Servers,
    List<OpenAPITag>? Tags,
    Dictionary<string, OpenAPIPath> Paths,
    OpenAPIComponents? Components,
    [property: YamlExtra] Dictionary<string, object?> Extra);


public static class Extension
{

    public static void Validate<T>(this T obj) where T : class
    {
        if (obj == null)
        {
            return;
        }
        if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            var dictType = typeof(T).GetGenericArguments()[1];
            if (dictType.IsClass)
            {
                typeof(Extension).GetMethod("ValidateImplDict")?.MakeGenericMethod(dictType).Invoke(null, new[] { obj });
                return;
            }
        }
        else if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>))
        {
            typeof(Extension).GetMethod("ValidateImplCollection")?.MakeGenericMethod(obj.GetType().GetGenericArguments()[0]).Invoke(null, new[] { obj });
            return;
        }

        obj.ValidateImpl();
    }

    public static void ValidateImpl<T>(this T obj) where T : class
    {
        var extraProperty = typeof(T).GetProperties()
            .Where(prop => prop.Name == "Extra" && prop.PropertyType == typeof(Dictionary<string, object?>))
            .FirstOrDefault();

        if (extraProperty is PropertyInfo extraPropertyInfo)
        {
            if (extraPropertyInfo.GetValue(obj) is not Dictionary<string, object?> extraDict)
            {
                return;
            }
            foreach (var (key, _) in extraDict)
            {
                Console.WriteLine($"Extra property: {typeof(T).Name} {key}");
            }
        }

        var classProperties = typeof(T).GetProperties()
            .Where(prop => prop.PropertyType.IsClass && prop.Name != "Extra");

        foreach (var property in classProperties)
        {
            var value = property.GetValue(obj);
            if (value is not null)
            {
                Console.WriteLine($"Validating property:  {typeof(T).Name} {property.Name} ({value.GetType().Name})");
                if (value.GetType().IsClass)
                {
                    typeof(Extension).GetMethod("Validate")?.MakeGenericMethod(value.GetType()).Invoke(null, [value]);

                }

            }
        }
    }

    public static void ValidateImplDict<T>(this Dictionary<string, T> obj) where T : class
    {
        Console.WriteLine($"Validating dictionary with {obj.Count} entries.");
        foreach (var (key, value) in obj)
        {
            if (value == null)
            {
                Console.WriteLine($"Skipping null value for key: {key}");
                continue;
            }
            Console.WriteLine($"Validating key: {key} ({value.GetType().Name})");
            // if (value is OpenAPIPath)
            // {
            //     Console.WriteLine($"Extra: Validating path {key}");
            // }
            if (value.GetType().IsClass)
            {
                typeof(Extension).GetMethod("Validate")?.MakeGenericMethod(value.GetType()).Invoke(null, [value]);
            }
        }
    }

    public static void ValidateImplCollection<T>(this List<T> obj) where T : class
    {
        Console.WriteLine($"Validating collection with {obj.Count} items.");
        foreach (var item in obj)
        {
            if (item.GetType().IsClass)
            {
                typeof(Extension).GetMethod("Validate")?.MakeGenericMethod(item.GetType()).Invoke(null, [item]);

            }

        }
    }
}
