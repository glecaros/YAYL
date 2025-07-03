using System.Collections.Generic;
using Xunit;
using YAYL.Attributes;

namespace YAYL.Tests;

public class YamlExtraAttributeTests
{
    public class PersonWithExtras
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }

        [YamlExtra]
        public Dictionary<string, object> AdditionalProperties { get; set; } = new();
    }

    public class PersonWithExtrasConstructor
    {
        public PersonWithExtrasConstructor(string name, int age)
        {
            Name = name;
            Age = age;
            AdditionalProperties = new();
        }

        public string Name { get; }
        public int Age { get; }

        [YamlExtra]
        public Dictionary<string, object> AdditionalProperties { get; set; }
    }

    [Fact]
    public void Parse_WithExtraProperties_StoresUnmatchedInExtraProperty()
    {
        var yaml = @"
name: John Doe
age: 30
height: 180cm
weight: 75kg
hobby: reading
";

        var parser = new YamlParser();
        var result = parser.Parse<PersonWithExtras>(yaml);

        Assert.NotNull(result);
        Assert.Equal("John Doe", result.Name);
        Assert.Equal(30, result.Age);
        Assert.NotNull(result.AdditionalProperties);
        Assert.Equal(3, result.AdditionalProperties.Count);
        Assert.Equal("180cm", result.AdditionalProperties["height"]);
        Assert.Equal("75kg", result.AdditionalProperties["weight"]);
        Assert.Equal("reading", result.AdditionalProperties["hobby"]);
    }

    [Fact]
    public void Parse_WithExtraPropertiesAndConstructor_StoresUnmatchedInExtraProperty()
    {
        var yaml = @"
name: Jane Smith
age: 25
city: New York
country: USA
occupation: Engineer
";

        var parser = new YamlParser();
        var result = parser.Parse<PersonWithExtrasConstructor>(yaml);

        Assert.NotNull(result);
        Assert.Equal("Jane Smith", result.Name);
        Assert.Equal(25, result.Age);
        Assert.NotNull(result.AdditionalProperties);
        Assert.Equal(3, result.AdditionalProperties.Count);
        Assert.Equal("New York", result.AdditionalProperties["city"]);
        Assert.Equal("USA", result.AdditionalProperties["country"]);
        Assert.Equal("Engineer", result.AdditionalProperties["occupation"]);
    }

    [Fact]
    public void Parse_WithComplexExtraProperties_HandlesNestedObjects()
    {
        var yaml = @"
name: Bob Wilson
age: 35
address:
  street: 123 Main St
  city: Springfield
  postal-code: 12345
skills:
  - C#
  - YAML
  - Testing
metadata:
  created: 2023-01-01
  active: true
  score: 95.5
";

        var parser = new YamlParser();
        var result = parser.Parse<PersonWithExtras>(yaml);

        Assert.NotNull(result);
        Assert.Equal("Bob Wilson", result.Name);
        Assert.Equal(35, result.Age);
        Assert.NotNull(result.AdditionalProperties);
        Assert.Equal(3, result.AdditionalProperties.Count);

        // Check nested dictionary (address)
        Assert.True(result.AdditionalProperties.ContainsKey("address"));
        var address = result.AdditionalProperties["address"] as Dictionary<string, object?>;
        Assert.NotNull(address);
        Assert.Equal("123 Main St", address["street"]);
        Assert.Equal("Springfield", address["city"]);
        Assert.Equal(12345, address["postal-code"]); // YAML parses numeric postal codes as integers

        // Check array (skills)
        Assert.True(result.AdditionalProperties.ContainsKey("skills"));
        var skills = result.AdditionalProperties["skills"] as List<object?>;
        Assert.NotNull(skills);
        Assert.Equal(3, skills.Count);
        Assert.Equal("C#", skills[0]);
        Assert.Equal("YAML", skills[1]);
        Assert.Equal("Testing", skills[2]);

        // Check metadata with various types
        Assert.True(result.AdditionalProperties.ContainsKey("metadata"));
        var metadata = result.AdditionalProperties["metadata"] as Dictionary<string, object?>;
        Assert.NotNull(metadata);
        Assert.True(metadata["active"] is bool);
        Assert.Equal(true, metadata["active"]);
        Assert.True(metadata["score"] is double);
        Assert.Equal(95.5, metadata["score"]);
    }

    [Fact]
    public void Parse_OnlyKnownProperties_ExtraPropertyIsEmpty()
    {
        var yaml = @"
name: Alice Brown
age: 28
";

        var parser = new YamlParser();
        var result = parser.Parse<PersonWithExtras>(yaml);

        Assert.NotNull(result);
        Assert.Equal("Alice Brown", result.Name);
        Assert.Equal(28, result.Age);
        Assert.NotNull(result.AdditionalProperties);
        Assert.Empty(result.AdditionalProperties);
    }

    public class InvalidExtraProperty
    {
        public string Name { get; set; } = string.Empty;

        [YamlExtra]
        public string NotADictionary { get; set; } = string.Empty;
    }

    [Fact]
    public void Parse_InvalidExtraPropertyType_ThrowsException()
    {
        var yaml = @"
name: Test
extra: value
";

        var parser = new YamlParser();

        var exception = Assert.Throws<YamlParseException>(() => parser.Parse<InvalidExtraProperty>(yaml));
        Assert.Contains("must be of type Dictionary<string, object>", exception.Message);
    }

    public class MultipleExtraProperties
    {
        public string Name { get; set; } = string.Empty;

        [YamlExtra]
        public Dictionary<string, object> Extras1 { get; set; } = new();

        [YamlExtra]
        public Dictionary<string, object> Extras2 { get; set; } = new();
    }

    [Fact]
    public void Parse_MultipleExtraProperties_ThrowsException()
    {
        var yaml = @"
name: Test
extra: value
";

        var parser = new YamlParser();

        var exception = Assert.Throws<YamlParseException>(() => parser.Parse<MultipleExtraProperties>(yaml));
        Assert.Contains("Only one property can be decorated with YamlExtraAttribute", exception.Message);
    }
}
