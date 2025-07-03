using YAYL.Attributes;

namespace YAYL.Tests;

public class YamlExtraAttributeTests
{
    public record PersonWithExtras(string Name, int Age)
    {
        [YamlExtra]
        public Dictionary<string, object> AdditionalProperties { get; set; } = [];
    }

    public record PersonWithExtrasConstructor(
        string Name,
        int Age,
        [property: YamlExtra] Dictionary<string, object> AdditionalProperties);

    [Fact]
    public void Parse_WithExtraProperties_StoresUnmatchedInExtraProperty()
    {
        var yaml = "name: John Doe\n" +
                   "age: 30\n" +
                   "height: 180cm\n" +
                   "weight: 75kg\n" +
                   "hobby: reading\n";

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
        var yaml = "name: Jane Smith\n" +
                   "age: 25\n" +
                   "city: New York\n" +
                   "country: USA\n" +
                   "occupation: Engineer\n";

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
        var yaml = "name: Bob Wilson\n" +
                   "age: 35\n" +
                   "address:\n" +
                   "  street: 123 Main St\n" +
                   "  city: Springfield\n" +
                   "  postal-code: 12345\n" +
                   "skills:\n" +
                   "  - C#\n" +
                   "  - YAML\n" +
                   "  - Testing\n" +
                   "metadata:\n" +
                   "  created: 2023-01-01\n" +
                   "  active: true\n" +
                   "  score: 95.5\n";

        var parser = new YamlParser();
        var result = parser.Parse<PersonWithExtras>(yaml);

        Assert.NotNull(result);
        Assert.Equal("Bob Wilson", result.Name);
        Assert.Equal(35, result.Age);
        Assert.NotNull(result.AdditionalProperties);
        Assert.Equal(3, result.AdditionalProperties.Count);

        Assert.True(result.AdditionalProperties.ContainsKey("address"));
        var address = result.AdditionalProperties["address"] as Dictionary<string, object?>;
        Assert.NotNull(address);
        Assert.Equal("123 Main St", address["street"]);
        Assert.Equal("Springfield", address["city"]);
        Assert.Equal(12345, address["postal-code"]);

        Assert.True(result.AdditionalProperties.ContainsKey("skills"));
        var skills = result.AdditionalProperties["skills"] as List<object?>;
        Assert.NotNull(skills);
        Assert.Equal(3, skills.Count);
        Assert.Equal("C#", skills[0]);
        Assert.Equal("YAML", skills[1]);
        Assert.Equal("Testing", skills[2]);

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
        var yaml = "name: Alice Brown\n" +
                   "age: 28\n";

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
        var yaml = "name: Test\n" +
                   "extra: value\n";

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
        var yaml = "name: Test\n" +
                   "extra: value\n";

        var parser = new YamlParser();

        var exception = Assert.Throws<YamlParseException>(() => parser.Parse<MultipleExtraProperties>(yaml));
        Assert.Contains("Only one property can be decorated with YamlExtraAttribute", exception.Message);
    }
}
