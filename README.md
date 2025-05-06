# YAYL (Yet Another YAML Library)

[![NuGet Version](https://img.shields.io/nuget/v/YAYL)](https://www.nuget.org/packages/YAYL)


YAYL is a high-level YAML parsing library for .NET that builds on top of YamlDotNet. It provides a clean, intuitive API similar to System.Text.Json, with strong support for C# records and modern language features.

## Features

- Simple, intuitive API inspired by System.Text.Json
- First-class support for C# records
- Polymorphic type handling with discriminators
- Flexible naming policies (Camel Case, Snake Case, Kebab Case)
- Support for nullable reference types
- Custom property name attributes
- Comprehensive collection type support
- Built-in parsing for common .NET types
- Serialization support for converting objects to YAML

## Installation

Install YAYL via NuGet:

```bash
dotnet add package YAYL
```

## Basic Usage

### Parsing YAML to Objects

Create a record and parse YAML directly into it:

```csharp
// Define your record
public record Person(string Name, int Age, List<string> Hobbies);

// Create a parser (defaults to kebab-case-lower naming)
var parser = new YamlParser();

// Parse YAML
var yaml = @"
    name: John Doe
    age: 30
    hobbies:
        - reading
        - hiking
        - photography";

var person = parser.Parse<Person>(yaml);
```

YAYL also supports parsing YAML from files and streams.

### Serializing Objects to YAML

Convert your .NET objects to YAML:

```csharp
// Define your record
public record Person(string Name, int Age, List<string> Hobbies);

// Create a serializer (defaults to kebab-case-lower naming)
var serializer = new YamlSerializer();

// Serialize an object
var person = new Person("John Doe", 30, new List<string> { "reading", "hiking", "photography" });
string yaml = serializer.Serialize(person);
// Result:
// name: John Doe
// age: 30
// hobbies:
// - reading
// - hiking
// - photography
```

### Parsing from a File

You can directly parse a YAML file:
```csharp
var parser = new YamlParser();
var result = parser.ParseFile<Person>("/path/to/file.yaml");
```

### Parsing from a Stream

You can parse YAML from any stream:
```csharp
using var stream = File.OpenRead("/path/to/file.yaml");
var result = parser.Parse<Person>(stream);
```

## Naming Policies

YAYL supports multiple naming policies for YAML property names:

```csharp
// Choose your preferred naming policy
var parser = new YamlParser(YamlNamingPolicy.SnakeCaseLower);
var serializer = new YamlSerializer(YamlNamingPolicy.SnakeCaseLower);
// or YamlNamingPolicy.CamelCase
// or YamlNamingPolicy.KebabCaseLower (default)
// or YamlNamingPolicy.KebabCaseUpper
// or YamlNamingPolicy.SnakeCaseUpper
```

Example with different naming policies:
```csharp
// Create serializers with different naming policies
var snakeCaseSerializer = new YamlSerializer(YamlNamingPolicy.SnakeCaseLower);
var camelCaseSerializer = new YamlSerializer(YamlNamingPolicy.CamelCase);

record ConfigItem(string DatabaseName, int PortNumber);
var config = new ConfigItem("testdb", 5432);

// Snake case output:
// database_name: testdb
// port_number: 5432
string snakeYaml = snakeCaseSerializer.Serialize(config);

// Camel case output:
// databaseName: testdb
// portNumber: 5432
string camelYaml = camelCaseSerializer.Serialize(config);
```

## Custom Property Names

Override the naming policy for specific properties:

```csharp
public record User(
    [property: YamlPropertyName("user-id")] string Id,
    string Name
);
```

When serializing:
```csharp
var user = new User("123", "John");
var yaml = serializer.Serialize(user);
// Output:
// user-id: 123
// name: John
```

## Ignoring Properties

Skip parsing or serializing specific properties:

```csharp
public record UserCredentials(
    string Username,
    [property: YamlIgnore] string Password
);
```

```csharp
var creds = new UserCredentials("admin", "secret123");
var yaml = serializer.Serialize(creds);
// Output will only include username, password is ignored:
// username: admin
```

## Polymorphic Types

YAYL supports polymorphic type handling using discriminators:

```csharp
[YamlPolymorphic("type")]
[YamlDerivedType("circle", typeof(Circle))]
[YamlDerivedType("rectangle", typeof(Rectangle))]
public abstract record Shape(string Name);

public record Circle(string Name, double Radius) : Shape(Name);
public record Rectangle(string Name, double Width, double Height) : Shape(Name);

// Parsing:
var yaml = @"
    type: circle
    name: My Circle
    radius: 5.0";

var shape = parser.Parse<Shape>(yaml); // Returns a Circle instance

// Serializing:
Shape shape = new Circle("My Circle", 5.0);
var yaml = serializer.Serialize(shape);
// Output:
// type: circle
// name: My Circle
// radius: 5
```

The library also supports fallback types for when the discriminantor is not available.

```csharp
[YamlPolymorphic("type")]
[YamlDerivedType("string", typeof(StringType))]
[YamlDerivedTypeDefault(typeof(RefType))]
public abstract record SchemaType;

public record StringType(string Pattern) : SchemaType;
public record RefType([property:YamlPropertyName("$ref")] string Ref) : SchemaType;

var yaml = @"
    - type: string
        pattern: ^[a-zA-Z0-9]+$
    - $ref: '#/components/schemas/Cat'";

var types = parser.Parse<SchemaType[]>(yaml);
// types[0] contains a StringType
// types[1] contains a RefType
```

## Enum Support

YAYL provides elegant handling of enums in polymorphic types:

```csharp
enum PetType { Dog, Cat }

[YamlPolymorphic("type")]
[YamlDerivedTypeEnum<PetType>(PetType.Dog, typeof(DogInfo))]
[YamlDerivedTypeEnum<PetType>(PetType.Cat, typeof(CatInfo))]
abstract record PetInfo(string Name, PetType Type);

record DogInfo(string Name, PetType Type, string Breed) : PetInfo(Name, Type);
record CatInfo(string Name, PetType Type, bool Indoor) : PetInfo(Name, Type);

// Serializing:
PetInfo pet = new DogInfo("Luna", PetType.Dog, "Samoyed");
var yaml = serializer.Serialize(pet);
// Output:
// type: dog
// name: Luna
// breed: Samoyed
```

## Complex Types

YAYL handles complex nested structures with ease:

```csharp
public record Configuration(
    Dictionary<string, List<Item>> Items,
    Dictionary<string, Dictionary<string, int>> Metrics
);
public record Item(string Name, int Value);

// Parsing:
var yaml = @"
    items:
        group1:
            - name: item1
              value: 1
        group2:
            - name: item3
              value: 3
    metrics:
        section1:
            metric1: 10
            metric2: 20";

var config = parser.Parse<Configuration>(yaml);

// Serializing:
var config = new Configuration(
    new Dictionary<string, List<Item>> {
        ["group1"] = new List<Item> {
            new Item("item1", 1),
            new Item("item2", 2)
        }
    },
    new Dictionary<string, Dictionary<string, int>> {
        ["section1"] = new Dictionary<string, int> {
            ["metric1"] = 10,
            ["metric2"] = 20
        }
    }
);
var yaml = serializer.Serialize(config);
```

## Collections and Dictionaries

YAYL seamlessly handles various collection types:

```csharp
// Arrays
var tags = new SimpleArray(["tag1", "tag2", "tag3"]);
var yaml = serializer.Serialize(tags);
// Result:
// tags:
// - tag1
// - tag2
// - tag3

// Dictionaries
var metadata = new SimpleDictionary(new Dictionary<string, string>
{
    ["key1"] = "value1",
    ["key2"] = "value2"
});
var yaml = serializer.Serialize(metadata);
// Result:
// metadata:
//   key1: value1
//   key2: value2
```

## Built-in Type Support

YAYL supports parsing and serializing of common .NET types:
- Primitive types (int, double, bool, etc.)
- DateTime and DateTimeOffset
- TimeSpan
- Guid
- Uri
- Enums
- Nullable value and reference types
- Collections (arrays, List<T>, IList<T>, ICollection<T>, IEnumerable<T>)
- Dictionaries

```csharp
record SpecialTypes(
    Guid Id,
    DateTime CreatedAt,
    DateTimeOffset ModifiedAt,
    TimeSpan Duration,
    Uri Website
);

var obj = new SpecialTypes(
    Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
    DateTime.Parse("2024-02-16T12:00:00"),
    DateTimeOffset.Parse("2024-02-16T12:00:00+02:00"),
    TimeSpan.Parse("02:30:00"),
    new Uri("https://example.com")
);

var yaml = serializer.Serialize(obj);
// Result includes serialized versions of special types:
// id: 550e8400-e29b-41d4-a716-446655440000
// created-at: 2024-02-16T12:00:00.0000000
// modified-at: 2024-02-16T12:00:00.0000000+02:00
// duration: 02:30:00
// website: https://example.com/
```

## Nullable Types

YAYL handles nullables elegantly:

```csharp
record NullableProps(int? OptionalAge, string? OptionalName);
var obj = new NullableProps(25, null);
var yaml = serializer.Serialize(obj);
// Result (null properties are skipped):
// optional-age: 25
```

## Error Handling

YAYL provides clear error messages through `YamlParseException`:

```csharp
try
{
    var result = parser.Parse<MyType>(yaml);
}
catch (YamlParseException ex)
{
    Console.WriteLine($"Failed to parse YAML: {ex.Message}");
}
```

## Async Support

YAYL offers asynchronous parsing support to help you write non-blocking I/O code.

Example:
```csharp
using var stream = File.OpenRead("config.yaml");
var config = await parser.ParseAsync<Configuration>(stream);
```

## Variable Resolution

Introduce dynamic value substitution by registering custom variable resolvers.
This feature allows you to embed placeholders in your YAML that are resolved at parse time.

Example:
```csharp
// Register a resolver for "${variable}" syntax
parser.AddVariableResolver(new Regex(@"\$\{([^}]+)\}"), async (varName, ct) =>
{
    // Return a value based on the variable name.
    return varName == "name" ? "World" : varName;
});

var person = await parser.ParseAsync<Person>("name: Hello, ${name}");
// person.Name will be "Hello, World"
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

YAYL is built on top of [YamlDotNet](https://github.com/aaubry/YamlDotNet), which does the heavy lifting of YAML parsing.