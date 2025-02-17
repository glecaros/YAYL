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

## Installation

Install YAYL via NuGet:

```bash
dotnet add package YAYL
```

## Basic Usage

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
// or YamlNamingPolicy.CamelCase
// or YamlNamingPolicy.KebabCaseLower (default)
// or YamlNamingPolicy.KebabCaseUpper
// or YamlNamingPolicy.SnakeCaseUpper
```

## Custom Property Names

Override the naming policy for specific properties:

```csharp
public record User(
    [property: YamlPropertyName("user-id")] string Id,
    string Name
);
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

// Usage:
var yaml = @"
    type: circle
    name: My Circle
    radius: 5.0";

var shape = parser.Parse<Shape>(yaml); // Returns a Circle instance
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
```

## Complex Types

YAYL handles complex nested structures with ease:

```csharp
public record Configuration(
    Dictionary<string, List<Item>> Items,
    Dictionary<string, Dictionary<string, int>> Metrics
);
public record Item(string Name, int Value);

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
```

## Built-in Type Support

YAYL supports parsing of common .NET types:
- Primitive types (int, double, bool, etc.)
- DateTime and DateTimeOffset
- TimeSpan
- Guid
- Uri
- Enums
- Nullable value and reference types
- Collections (arrays, List<T>, IList<T>, ICollection<T>, IEnumerable<T>)
- Dictionaries

## Ignoring Properties

Skip parsing specific properties:

```csharp
public record UserCredentials(
    string Username,
    [property: YamlIgnore] string Password
);
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

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgments

YAYL is built on top of [YamlDotNet](https://github.com/aaubry/YamlDotNet), which does the heavy lifting of YAML parsing.