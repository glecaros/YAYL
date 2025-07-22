using System.ComponentModel;
using System.Numerics;
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

// doc.Validate();

OpenAPIVisitor visitor = new();
var context = visitor.Visit(doc);

// Print the symbol table summary
context.PrintSummary();

Console.WriteLine("\n=== Most Referenced Schemas ===");
var topReferences = context.ReferenceCount
    .Where(kvp => kvp.Value > 0)
    .OrderByDescending(kvp => kvp.Value)
    .Take(10);

foreach (var (name, count) in topReferences)
{
    var definition = context.GetDefinition(name);
    if (definition != null)
    {
        Console.WriteLine($"{name}: {count} references ({definition.Type})");
    }
}

Console.WriteLine("\n=== Schema Type Breakdown ===");
var typeStats = context.Definitions.Values
    .GroupBy(d => d.Type)
    .OrderByDescending(g => g.Count())
    .ToDictionary(g => g.Key, g => g.Count());

foreach (var (type, count) in typeStats)
{
    Console.WriteLine($"{type}: {count} schemas");
}

// Show breakdown of schema sources
Console.WriteLine("\n=== Schema Sources ===");
var componentSchemas = context.Definitions.Values.Where(d => !d.Name.Contains("_")).Count();
var anonymousSchemas = context.Definitions.Values.Where(d => d.Name.Contains("_")).Count();

Console.WriteLine($"Component schemas: {componentSchemas}");
Console.WriteLine($"Anonymous/path schemas: {anonymousSchemas}");

// Show some examples of anonymous schemas
Console.WriteLine("\n=== Sample Anonymous Schemas ===");
var anonymousExamples = context.Definitions.Values
    .Where(d => d.Name.Contains("_") && (d.Name.Contains("_GET_") || d.Name.Contains("_POST_")))
    .Take(5);

foreach (var schema in anonymousExamples)
{
    Console.WriteLine($"{schema.Name} ({schema.Type})");
}

// Example: Show details for a specific highly-referenced schema
Console.WriteLine("\n=== Example: 'Metadata' Schema Details ===");
var metadataSchema = context.GetDefinition("Metadata");
if (metadataSchema is ObjectSchemaDefinition objDef)
{
    Console.WriteLine($"Type: {objDef.Type}");
    Console.WriteLine($"Properties: {objDef.Properties.Count}");
    foreach (var (propName, propDef) in objDef.Properties.Take(3))
    {
        Console.WriteLine($"  {propName}: {propDef.Type}");
    }
    if (objDef.Required != null)
        Console.WriteLine($"Required fields: [{string.Join(", ", objDef.Required)}]");
}// foreach (var (pathStr, pathEntry) in doc.Paths)
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

public abstract record SchemaDefinition(string Name, string Type);

public record StringSchemaDefinition(
    string Name,
    List<string>? Enum = null,
    string? Format = null,
    int? MinLength = null,
    int? MaxLength = null
) : SchemaDefinition(Name, "string");

public record IntegerSchemaDefinition(
    string Name,
    string? Format = null,
    BigInteger? Minimum = null,
    BigInteger? Maximum = null
) : SchemaDefinition(Name, "integer");

public record NumberSchemaDefinition(
    string Name,
    string? Format = null,
    float? Minimum = null,
    float? Maximum = null,
    bool? ExclusiveMinimum = null
) : SchemaDefinition(Name, "number");

public record BooleanSchemaDefinition(string Name) : SchemaDefinition(Name, "boolean");

public record NullSchemaDefinition(string Name) : SchemaDefinition(Name, "null");

public record ArraySchemaDefinition(
    string Name,
    SchemaDefinition ItemsSchema,
    int? MinItems = null,
    int? MaxItems = null,
    bool? Optional = null
) : SchemaDefinition(Name, "array");

public record ObjectSchemaDefinition(
    string Name,
    Dictionary<string, SchemaDefinition> Properties,
    List<string>? Required = null,
    object? AdditionalProperties = null,
    int? MaxProperties = null
) : SchemaDefinition(Name, "object");

public record ReferenceSchemaDefinition(
    string Name,
    string RefPath
) : SchemaDefinition(Name, "reference");

public record CompositeSchemaDefinition(
    string Name,
    string CompositionType, // "oneOf", "allOf", "anyOf"
    List<SchemaDefinition> Schemas
) : SchemaDefinition(Name, "composite");

// Context class acting as a symbol table
public class Context
{
    private readonly Dictionary<string, SchemaDefinition> _definitions = new();
    private readonly Dictionary<string, int> _referenceCount = new();

    public IReadOnlyDictionary<string, SchemaDefinition> Definitions => _definitions;
    public IReadOnlyDictionary<string, int> ReferenceCount => _referenceCount;

    public void AddDefinition(SchemaDefinition definition)
    {
        _definitions[definition.Name] = definition;
        _referenceCount[definition.Name] = 0;
    }

    public void AddReference(string name)
    {
        if (_referenceCount.ContainsKey(name))
        {
            _referenceCount[name]++;
        }
        else
        {
            _referenceCount[name] = 1;
        }
    }

    public SchemaDefinition? GetDefinition(string name)
    {
        return _definitions.TryGetValue(name, out var definition) ? definition : null;
    }

    public bool HasDefinition(string name)
    {
        return _definitions.ContainsKey(name);
    }

    public void PrintSummary()
    {
        Console.WriteLine($"\n=== Schema Definitions Summary ===");
        Console.WriteLine($"Total definitions: {_definitions.Count}");

        var typeGroups = _definitions.Values.GroupBy(d => d.Type);
        foreach (var group in typeGroups)
        {
            Console.WriteLine($"  {group.Key}: {group.Count()}");
        }

        Console.WriteLine("\n=== Reference Usage ===");
        foreach (var (name, count) in _referenceCount.OrderByDescending(kvp => kvp.Value))
        {
            Console.WriteLine($"  {name}: {count} references");
        }
    }
};

public class OpenAPIVisitor
{
    private int _anonymousSchemaCounter = 0;

    public Context Visit(OpenAPIDocument document)
    {
        Context context = new();

        // First pass: Visit all components to build the symbol table
        if (document.Components is not null)
        {
            context = Visit(context, document.Components);
        }

        // Second pass: Visit paths to find schema usage and anonymous schemas
        foreach (var (pathStr, pathEntry) in document.Paths)
        {
            context = Visit(context, pathStr, pathEntry);
        }

        return context;
    }

    public Context Visit(Context context, OpenAPIComponents components)
    {
        if (components.Schemas is not null)
        {
            foreach ((var name, var schema) in components.Schemas)
            {
                var definition = CreateSchemaDefinition(context, name, schema);
                if (definition is not null)
                {
                    context.AddDefinition(definition);
                }
            }
        }
        return context;
    }

    public Context Visit(Context context, string pathStr, OpenAPIPath path)
    {
        // Visit all HTTP methods in the path
        var methods = new[] {
            ("GET", path.Get),
            ("POST", path.Post),
            ("PUT", path.Put),
            ("DELETE", path.Delete),
            ("OPTIONS", path.Options),
            ("HEAD", path.Head),
            ("PATCH", path.Patch),
            ("TRACE", path.Trace)
        };

        foreach (var (methodName, method) in methods.Where(m => m.Item2 != null))
        {
            context = Visit(context, pathStr, methodName, method!);
        }

        return context;
    }

    public Context Visit(Context context, string pathStr, string methodName, OpenAPIMethod method)
    {
        var pathPrefix = $"{SanitizePathForName(pathStr)}_{methodName}";

        // Visit parameters and create definitions for anonymous schemas
        if (method.Parameters != null)
        {
            for (int i = 0; i < method.Parameters.Count; i++)
            {
                var parameter = method.Parameters[i];
                if (parameter.Schema != null)
                {
                    var paramName = $"{pathPrefix}_Param_{parameter.Name ?? i.ToString()}";
                    VisitSchemaAndCreateDefinitions(context, paramName, parameter.Schema);
                }
            }
        }

        // Visit request body and create definitions for anonymous schemas
        if (method.RequestBody?.Content != null)
        {
            foreach (var (contentType, mediaType) in method.RequestBody.Content)
            {
                if (mediaType.Schema != null)
                {
                    var requestBodyName = $"{pathPrefix}_RequestBody_{SanitizeContentType(contentType)}";
                    VisitSchemaAndCreateDefinitions(context, requestBodyName, mediaType.Schema);
                }
            }
        }

        // Visit responses and create definitions for anonymous schemas
        foreach (var (statusCode, response) in method.Responses)
        {
            if (response.Content != null)
            {
                foreach (var (contentType, mediaType) in response.Content)
                {
                    if (mediaType.Schema != null)
                    {
                        var responseName = $"{pathPrefix}_Response_{statusCode}_{SanitizeContentType(contentType)}";
                        VisitSchemaAndCreateDefinitions(context, responseName, mediaType.Schema);
                    }
                }
            }

            // Visit response headers
            if (response.Headers != null)
            {
                foreach (var (headerName, header) in response.Headers)
                {
                    if (header.Schema != null)
                    {
                        var headerSchemaName = $"{pathPrefix}_Response_{statusCode}_Header_{headerName}";
                        VisitSchemaAndCreateDefinitions(context, headerSchemaName, header.Schema);
                    }
                }
            }
        }

        return context;
    }

    private void VisitSchemaAndCreateDefinitions(Context context, string baseName, OpenAPISchema schema)
    {
        // If it's a reference, just track the reference
        if (!string.IsNullOrEmpty(schema.Ref))
        {
            var refName = ExtractRefName(schema.Ref);
            if (!string.IsNullOrEmpty(refName))
            {
                context.AddReference(refName);
            }
            return;
        }

        // If it's an anonymous schema (not a reference), create a definition for it
        var definition = CreateSchemaDefinition(context, baseName, schema);
        if (definition != null)
        {
            context.AddDefinition(definition);
        }

        // Continue recursively visiting for references
        VisitSchemaForReferences(context, schema);
    }

    private string SanitizePathForName(string path)
    {
        return path.Replace("/", "_")
                  .Replace("{", "")
                  .Replace("}", "")
                  .Replace("-", "_")
                  .TrimStart('_');
    }

    private string SanitizeContentType(string contentType)
    {
        return contentType.Replace("/", "_")
                         .Replace("+", "_")
                         .Replace("-", "_")
                         .Replace(".", "_");
    }

    private string GenerateAnonymousSchemaName(string context)
    {
        return $"Anonymous_{context}_{++_anonymousSchemaCounter}";
    }

    private void VisitSchemaForReferences(Context context, OpenAPISchema schema)
    {
        // Handle $ref
        if (!string.IsNullOrEmpty(schema.Ref))
        {
            var refName = ExtractRefName(schema.Ref);
            if (!string.IsNullOrEmpty(refName))
            {
                context.AddReference(refName);
            }
        }

        // Handle composition schemas
        if (schema.OneOf != null)
        {
            foreach (var subSchema in schema.OneOf)
            {
                VisitSchemaForReferences(context, subSchema);
            }
        }

        if (schema.AllOf != null)
        {
            foreach (var subSchema in schema.AllOf)
            {
                VisitSchemaForReferences(context, subSchema);
            }
        }

        if (schema.AnyOf != null)
        {
            foreach (var subSchema in schema.AnyOf)
            {
                VisitSchemaForReferences(context, subSchema);
            }
        }

        // Handle specific schema types
        switch (schema)
        {
            case OpenAPISchemaArray arraySchema:
                VisitSchemaForReferences(context, arraySchema.Items);
                break;

            case OpenAPISchemaObject objectSchema:
                if (objectSchema.Properties != null)
                {
                    foreach (var (_, propSchema) in objectSchema.Properties)
                    {
                        VisitSchemaForReferences(context, propSchema);
                    }
                }
                if (objectSchema.AdditionalProperties is OpenAPISchema additionalSchema)
                {
                    VisitSchemaForReferences(context, additionalSchema);
                }
                break;
        }
    }

    private string? ExtractRefName(string refPath)
    {
        // Extract the name from a reference like "#/components/schemas/MySchema"
        if (refPath.StartsWith("#/components/schemas/"))
        {
            return refPath.Substring("#/components/schemas/".Length);
        }
        return null;
    }

    private SchemaDefinition? CreateSchemaDefinition(Context context, string name, OpenAPISchema schema)
    {
        // Handle $ref first
        if (!string.IsNullOrEmpty(schema.Ref))
        {
            var refName = ExtractRefName(schema.Ref);
            if (!string.IsNullOrEmpty(refName))
            {
                context.AddReference(refName);
                return new ReferenceSchemaDefinition(name, refName);
            }
        }

        // Handle composition schemas
        if (schema.OneOf != null)
        {
            var subSchemas = new List<SchemaDefinition>();
            for (int i = 0; i < schema.OneOf.Count; i++)
            {
                var subSchema = schema.OneOf[i];
                var subName = $"{name}_OneOf_{i}";
                var subDefinition = CreateSchemaDefinition(context, subName, subSchema);
                if (subDefinition != null)
                {
                    subSchemas.Add(subDefinition);
                    // Add the sub-definition to the context if it's not a reference
                    if (!string.IsNullOrEmpty(subSchema.Ref) == false)
                    {
                        context.AddDefinition(subDefinition);
                    }
                }
            }
            return new CompositeSchemaDefinition(name, "oneOf", subSchemas);
        }

        if (schema.AllOf != null)
        {
            var subSchemas = new List<SchemaDefinition>();
            for (int i = 0; i < schema.AllOf.Count; i++)
            {
                var subSchema = schema.AllOf[i];
                var subName = $"{name}_AllOf_{i}";
                var subDefinition = CreateSchemaDefinition(context, subName, subSchema);
                if (subDefinition != null)
                {
                    subSchemas.Add(subDefinition);
                    // Add the sub-definition to the context if it's not a reference
                    if (!string.IsNullOrEmpty(subSchema.Ref) == false)
                    {
                        context.AddDefinition(subDefinition);
                    }
                }
            }
            return new CompositeSchemaDefinition(name, "allOf", subSchemas);
        }

        if (schema.AnyOf != null)
        {
            var subSchemas = new List<SchemaDefinition>();
            for (int i = 0; i < schema.AnyOf.Count; i++)
            {
                var subSchema = schema.AnyOf[i];
                var subName = $"{name}_AnyOf_{i}";
                var subDefinition = CreateSchemaDefinition(context, subName, subSchema);
                if (subDefinition != null)
                {
                    subSchemas.Add(subDefinition);
                    // Add the sub-definition to the context if it's not a reference
                    if (!string.IsNullOrEmpty(subSchema.Ref) == false)
                    {
                        context.AddDefinition(subDefinition);
                    }
                }
            }
            return new CompositeSchemaDefinition(name, "anyOf", subSchemas);
        }

        // Handle specific schema types
        return schema switch
        {
            OpenAPISchemaString stringSchema => new StringSchemaDefinition(
                name,
                stringSchema.Enum,
                stringSchema.Format,
                stringSchema.MinLength,
                stringSchema.MaxLength
            ),

            OpenAPISchemaInteger integerSchema => new IntegerSchemaDefinition(
                name,
                integerSchema.Format,
                integerSchema.Minimum,
                integerSchema.Maximum
            ),

            OpenAPISchemaNumber numberSchema => new NumberSchemaDefinition(
                name,
                numberSchema.Format,
                numberSchema.Minimum,
                numberSchema.Maximum,
                numberSchema.ExclusiveMinimum
            ),

            OpenAPISchemaBoolean => new BooleanSchemaDefinition(name),

            OpenAPISchemaNull => new NullSchemaDefinition(name),

            OpenAPISchemaArray arraySchema => CreateArraySchemaDefinition(context, name, arraySchema),

            OpenAPISchemaObject objectSchema => CreateObjectSchemaDefinition(context, name, objectSchema),

            _ => CreateGenericSchemaDefinition(name, schema)
        };
    }

    private ArraySchemaDefinition CreateArraySchemaDefinition(Context context, string name, OpenAPISchemaArray arraySchema)
    {
        var itemsName = $"{name}_Items";
        var itemsDefinition = CreateSchemaDefinition(context, itemsName, arraySchema.Items);

        // If the items schema is not a reference and not null, add it to the context
        if (itemsDefinition != null && string.IsNullOrEmpty(arraySchema.Items.Ref))
        {
            context.AddDefinition(itemsDefinition);
        }

        return new ArraySchemaDefinition(
            name,
            itemsDefinition ?? new ReferenceSchemaDefinition(itemsName, "unknown"),
            arraySchema.MinItems,
            arraySchema.MaxItems,
            arraySchema.Optional
        );
    }

    private ObjectSchemaDefinition CreateObjectSchemaDefinition(Context context, string name, OpenAPISchemaObject objectSchema)
    {
        var properties = new Dictionary<string, SchemaDefinition>();

        if (objectSchema.Properties != null)
        {
            foreach (var (propName, propSchema) in objectSchema.Properties)
            {
                var propDefinitionName = $"{name}_{propName}";
                var propDefinition = CreateSchemaDefinition(context, propDefinitionName, propSchema);
                if (propDefinition != null)
                {
                    properties[propName] = propDefinition;

                    // If the property schema is not a reference, add it to the context
                    if (string.IsNullOrEmpty(propSchema.Ref))
                    {
                        context.AddDefinition(propDefinition);
                    }
                }
            }
        }

        // Handle additionalProperties if it's a schema
        if (objectSchema.AdditionalProperties is OpenAPISchema additionalSchema)
        {
            var additionalName = $"{name}_AdditionalProperties";
            var additionalDefinition = CreateSchemaDefinition(context, additionalName, additionalSchema);
            if (additionalDefinition != null && string.IsNullOrEmpty(additionalSchema.Ref))
            {
                context.AddDefinition(additionalDefinition);
            }
        }

        return new ObjectSchemaDefinition(
            name,
            properties,
            objectSchema.Required,
            objectSchema.AdditionalProperties,
            objectSchema.MaxProperties
        );
    }

    private SchemaDefinition CreateGenericSchemaDefinition(string name, OpenAPISchema schema)
    {
        // For schemas that don't have a specific type, create a generic object schema
        return new ObjectSchemaDefinition(name, new Dictionary<string, SchemaDefinition>());
    }
}