using System.ComponentModel;
using System.Reflection;
using YAYL;
using YAYL.Attributes;

// const string path = "/workspaces/sorento_sdk/openai-in-typespec/external-specs/latest.yaml";
// const string path = "/workspaces/sorento_sdk/tools/OpenApiToTsp/OpenApiTinyDOM/test.yaml";
const string path = "/workspaces/YAYL/src/OpenAPI/openapi.documented.yml";
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


[YamlPolymorphic("type")]
[YamlDerivedType("integer", typeof(OpenAPISchemaInteger))]
[YamlDerivedType("number", typeof(OpenAPISchemaNumber))]
[YamlDerivedType("string", typeof(OpenAPISchemaString))]
[YamlDerivedType("object", typeof(OpenAPISchemaObject))]
[YamlDerivedType("array", typeof(OpenAPISchemaArray))]
[YamlDerivedType("boolean", typeof(OpenAPISchemaBoolean))]
[YamlDerivedType("null", typeof(OpenAPISchemaNull))]
public record OpenAPISchema();

public record OpenAPISchemaNull(
    [property: YamlExtra] Dictionary<string, object?>? Extra
) : OpenAPISchema;

public record OpenAPISchemaInteger(
    int? Default,
    string? Description,
    // [property: YamlPropertyName("format")] string? Format,
    // [property: YamlPropertyName("minimum")] int? Minimum,
    // [property: YamlPropertyName("maximum")] int? Maximum,
    [property: YamlExtra] Dictionary<string, object?>? Extra
) : OpenAPISchema;

public record OpenAPISchemaNumber(
    [property: YamlExtra] Dictionary<string, object?>? Extra
) : OpenAPISchema;

public record OpenAPISchemaString(
    /* TODO: Add support for string and List<string> as variant alternatives */
    object? Default,
    List<string>? Enum,
    string? Description,
    bool? Nullable,
    string? Example,
    string? Format,
    // [property: YamlPropertyName("enum")] List<string>? Enum,
    // [property: YamlPropertyName("pattern")] string? Pattern,
    [property: YamlExtra] Dictionary<string, object?>? Extra
) : OpenAPISchema;

public record OpenAPISchemaBoolean : OpenAPISchema;


public record OpenAPIAdditionalPropertiesEmpty(
    [property: YamlExtra] Dictionary<string, object>? Extra
);

public record OpenAPISchemaObject(
    [property:
        YamlVariant,
        YamlVariantTypeObject(typeof(OpenAPISchema), "type"),
        YamlVariantTypeObject(typeof(OpenAPIRef), "$ref"),
        YamlVariantTypeObject(typeof(OpenAPIOneOf), "oneOf"),
        YamlVariantTypeObject(typeof(OpenAPIAnyOf), "anyOf"),
        YamlVariantTypeObject(typeof(OpenAPIAllOf), "allOf"),
        YamlVariantTypeObject(typeof(OpenAPISchemaArray), "items"),] // TODO: This seems to be a spec bug.
    Dictionary<string, object>? Properties,
    // [property: YamlPropertyName("required")] List<string>? Required

    [property:
        YamlVariant,
        YamlVariantTypeScalar(typeof(bool)),
        YamlVariantTypeObject(typeof(OpenAPISchema), "type"),
        YamlVariantTypeObject(typeof(OpenAPIOneOf), "oneOf"),
        YamlVariantTypeObject(typeof(OpenAPIRef), "$ref"),
        YamlVariantTypeDefault(typeof(OpenAPIAdditionalPropertiesEmpty))]
    object? AdditionalProperties,
    List<string>? Required,
    bool? Nullable,
    [property: YamlExtra] Dictionary<string, object?>? Extra
) : OpenAPISchema;

public record OpenAPISchemaArray(
    [property:
        YamlVariant,
        YamlVariantTypeObject(typeof(OpenAPISchema), "type"),
        YamlVariantTypeObject(typeof(OpenAPIRef), "$ref"),
        YamlVariantTypeObject(typeof(OpenAPIOneOf), "oneOf"),
        YamlVariantTypeObject(typeof(OpenAPIAllOf), "allOf")]
    object Items,
    // [property: YamlPropertyName("minItems")] int? MinItems = null,
    // [property: YamlPropertyName("maxItems")] int? MaxItems = null
    [property: YamlExtra] Dictionary<string, object?>? Extra
) : OpenAPISchema;

public record OpenAPIRef(
    [property: YamlPropertyName("$ref")] string Ref
);

public record OpenAPIRecursiveRef(
    [property: YamlPropertyName("$recursiveRef")] string RecursiveRef
);

public record OpenAPIOneOf(
    [property:
        YamlVariant,
        YamlVariantTypeObject(typeof(OpenAPISchema), "type"),
        YamlVariantTypeObject(typeof(OpenAPIRef), "$ref"),
        YamlVariantTypeObject(typeof(OpenAPIRecursiveRef), "$recursiveRef")]
    List<object> OneOf,
    [property: YamlExtra] Dictionary<string, object?> Extra
);

public record OpenAPIAllOfExtensions(
    [property:YamlExtra] Dictionary<string, object?> Extra
);

public record OpenAPIAllOf(
    [property:
        YamlVariant,
        YamlVariantTypeObject(typeof(OpenAPISchema), "type"),
        YamlVariantTypeObject(typeof(OpenAPIRef), "$ref"),
        YamlVariantTypeDefault(typeof(OpenAPIAllOfExtensions))]
    List<object> AllOf,
    [property: YamlExtra] Dictionary<string, object?> Extra
);

public record OpenAPIAnyOf(
    [property:
        YamlVariant,
        YamlVariantTypeObject(typeof(OpenAPISchema), "type"),
        YamlVariantTypeObject(typeof(OpenAPIRef), "$ref"),]
    List<object> AnyOf,
    [property: YamlExtra] Dictionary<string, object?> Extra
);

public record OpenAPIMethodParameters(
    string Name,
    ParameterLocation In,
    string? Description,
    bool? Required,
    [property: YamlVariant, YamlVariantTypeObject(typeof(OpenAPISchema), "type"), YamlVariantTypeObject(typeof(OpenAPIRef), "$ref")] object? Schema,
    string? Style,
    bool? Explode,
    bool? AllowReserved,
    [property: YamlExtra] Dictionary<string, object?> Extra
);

public record OpenAPIMediaTypeObject(
    [property:
        YamlVariant,
        YamlVariantTypeObject(typeof(OpenAPISchema), "type"),
        YamlVariantTypeObject(typeof(OpenAPIRef), "$ref"),
        YamlVariantTypeObject(typeof(OpenAPIOneOf), "oneOf")]
    object? Schema,
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
    [property:
        YamlVariant,
        YamlVariantTypeObject(typeof(OpenAPISchema), "type"),
        YamlVariantTypeObject(typeof(OpenAPIRef), "$ref")]
    object? Schema,
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
    [property:
        YamlVariant,
        YamlVariantTypeObject(typeof(OpenAPISchema), "type"),
        YamlVariantTypeObject(typeof(OpenAPIOneOf), "oneOf"),
        YamlVariantTypeObject(typeof(OpenAPIAllOf), "allOf"),
        YamlVariantTypeObject(typeof(OpenAPIAnyOf), "anyOf"),
        YamlVariantTypeObject(typeof(OpenAPISchemaObject), "properties"),] // TODO: This seems to be a spec bug.
    Dictionary<string, object>? Schemas,
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
                typeof(Extension).GetMethod("Validate")?.MakeGenericMethod(value.GetType()).Invoke(null, [value]);

            }
        }
    }

    public static void ValidateImplDict<T>(this Dictionary<string, T> obj) where T : class
    {
        Console.WriteLine($"Validating dictionary with {obj.Count} entries.");
        foreach (var (key, value) in obj)
        {
            // if (value is OpenAPIPath)
            // {
            //     Console.WriteLine($"Extra: Validating path {key}");
            // }
            value.Validate();
        }
    }

    public static void ValidateImplCollection<T>(this List<T> obj) where T : class
    {
        Console.WriteLine($"Validating collection with {obj.Count} items.");
        foreach (var item in obj)
        {
            item.Validate();
        }
    }
}
