using System.Text;
using System.Text.RegularExpressions;
using YAYL.Attributes;

namespace YAYL.Tests;

public partial class YamlParserTests
{
    private readonly YamlParser _parser = new YamlParser();

    record SingleProp(string Name);

    [Fact]
    public void Parse_SinglePropertyString_Success()
    {
        var yaml = "name: John Doe";

        var result = _parser.Parse<SingleProp>(yaml);

        Assert.NotNull(result);
        Assert.Equal("John Doe", result.Name);
    }

    record SinglePropRename([property: YamlPropertyName("da-name")] string Name);

    [Fact]
    public void Parse_SinglePropertyString_Rename_Success()
    {
        var yaml = "da-name: John Doe";

        var result = _parser.Parse<SinglePropRename>(yaml);

        Assert.NotNull(result);
        Assert.Equal("John Doe", result.Name);
    }

    record SinglePropInt(int Age);

    [Fact]
    public void Parse_SinglePropertyInteger_Success()
    {
        var yaml = "age: 30";

        var result = _parser.Parse<SinglePropInt>(yaml);

        Assert.NotNull(result);
        Assert.Equal(30, result.Age);
    }

    record SinglePropBool(bool IsActive);

    [Fact]
    public void Parse_SinglePropertyBoolean_Success()
    {
        var yaml = "is-active: true";

        var result = _parser.Parse<SinglePropBool>(yaml);

        Assert.NotNull(result);
        Assert.True(result.IsActive);
    }

    record SinglePropDouble(double Score);

    [Fact]
    public void Parse_SinglePropertyDouble_Success()
    {
        var yaml = "score: 85.5";

        var result = _parser.Parse<SinglePropDouble>(yaml);

        Assert.NotNull(result);
        Assert.Equal(85.5, result.Score);
    }

    [Fact]
    public void Parse_MultiLineString_Success()
    {
        var yaml = @"name: |
            This is a multi-line
            string value that should
            preserve newlines";

        var result = _parser.Parse<SingleProp>(yaml);

        Assert.NotNull(result);
        Assert.Contains("multi-line", result.Name);
        Assert.Contains("newlines", result.Name);
    }

    [Fact]
    public void Parse_FoldedMultiLineString_Success()
    {
        var yaml = @"name: >
            This is a multi-line
            string value that should
            be folded into a single line";

        var result = _parser.Parse<SingleProp>(yaml);

        Assert.NotNull(result);
        Assert.Equal("This is a multi-line string value that should be folded into a single line", result.Name.Trim());
    }

    record SimpleArray(string[] Tags);

    [Fact]
    public void Parse_SimpleArray_Success()
    {
        var yaml = @"tags:
            - tag1
            - tag2
            - tag3";

        var result = _parser.Parse<SimpleArray>(yaml);

        Assert.NotNull(result);
        Assert.Equal(3, result.Tags.Length);
        Assert.Contains("tag1", result.Tags);
        Assert.Contains("tag2", result.Tags);
        Assert.Contains("tag3", result.Tags);
    }

    record SimpleDictionary(Dictionary<string, string> Metadata);

    [Fact]
    public void Parse_Dictionary_Success()
    {
        var yaml = @"metadata:
            key1: value1
            key2: value2
            key3: value3";

        var result = _parser.Parse<SimpleDictionary>(yaml);

        Assert.NotNull(result);
        Assert.Equal(3, result.Metadata.Count);
        Assert.Equal("value1", result.Metadata["key1"]);
        Assert.Equal("value2", result.Metadata["key2"]);
        Assert.Equal("value3", result.Metadata["key3"]);
    }

    record SimpleIntDictionary(Dictionary<string, int> Metadata);

    [Fact]
    public void Parse_IntDictionary_Success()
    {
        var yaml = @"metadata:
            key1: 1
            key2: 2
            key3: 3";

        var result = _parser.Parse<SimpleIntDictionary>(yaml);

        Assert.NotNull(result);
        Assert.Equal(3, result.Metadata.Count);
        Assert.Equal(1, result.Metadata["key1"]);
        Assert.Equal(2, result.Metadata["key2"]);
        Assert.Equal(3, result.Metadata["key3"]);
    }

    record NestedObject(Nested Nested);
    record Nested(string Description, int Priority);

    [Fact]
    public void Parse_NestedObject_Success()
    {
        var yaml = @"nested:
            description: Test description
            priority: 1";

        var result = _parser.Parse<NestedObject>(yaml);

        Assert.NotNull(result);
        Assert.NotNull(result.Nested);
        Assert.Equal("Test description", result.Nested.Description);
        Assert.Equal(1, result.Nested.Priority);
    }

    record NestedObjectArray(Nested[] Nested);

    [Fact]
    public void ParseNestedArray_Success()
    {
        var yaml = @"
            nested:
                - description: Test description 1
                  priority: 1
                - description: Test description 2
                  priority: 2";

        var result = _parser.Parse<NestedObjectArray>(yaml);

        Assert.NotNull(result);
        Assert.Equal(2, result.Nested.Length);
        Assert.Equal("Test description 1", result.Nested[0].Description);
        Assert.Equal(1, result.Nested[0].Priority);
        Assert.Equal("Test description 2", result.Nested[1].Description);
        Assert.Equal(2, result.Nested[1].Priority);
    }

    record NullableProps(int? OptionalAge, string? OptionalName, double? OptionalScore);

    [Fact]
    public void Parse_NullableProperties_Success()
    {
        var yaml = @"
            optional-age: 25
            optional-score: ~";

        var result = _parser.Parse<NullableProps>(yaml);

        Assert.NotNull(result);
        Assert.Equal(25, result.OptionalAge);
        Assert.Null(result.OptionalName);
        Assert.Null(result.OptionalScore);
    }

    record ConfigItem(string DatabaseName, int PortNumber);

    [Fact]
    public void Parse_SnakeCaseNaming_Success()
    {
        var parser = new YamlParser(YamlNamingPolicy.SnakeCaseLower);
        var yaml = @"
            database_name: testdb
            port_number: 5432";

        var result = parser.Parse<ConfigItem>(yaml);

        Assert.NotNull(result);
        Assert.Equal("testdb", result.DatabaseName);
        Assert.Equal(5432, result.PortNumber);
    }

    [Fact]
    public void Parse_CamelCaseNaming_Success()
    {
        var parser = new YamlParser(YamlNamingPolicy.CamelCase);
        var yaml = @"
            databaseName: testdb
            portNumber: 5432";

        var result = parser.Parse<ConfigItem>(yaml);

        Assert.NotNull(result);
        Assert.Equal("testdb", result.DatabaseName);
        Assert.Equal(5432, result.PortNumber);
    }

    [Fact]
    public void Parse_EmptyYaml_ReturnsNull()
    {
        var result = _parser.Parse<ConfigItem>("");
        Assert.Null(result);
    }

    [Fact]
    public void Parse_WhitespaceYaml_ReturnsNull()
    {
        var result = _parser.Parse<ConfigItem>("   \n   \t   ");
        Assert.Null(result);
    }

    record SpecialTypes(
        Guid Id,
        DateTime CreatedAt,
        DateTimeOffset ModifiedAt,
        TimeSpan Duration,
        Uri Website
    );

    [Fact]
    public void Parse_SpecialTypes_Success()
    {
        var yaml = @"
            id: 550e8400-e29b-41d4-a716-446655440000
            created-at: 2024-02-16T12:00:00
            modified-at: 2024-02-16T12:00:00+02:00
            duration: 02:30:00
            website: https://example.com";

        var result = _parser.Parse<SpecialTypes>(yaml);

        Assert.NotNull(result);
        Assert.Equal(Guid.Parse("550e8400-e29b-41d4-a716-446655440000"), result.Id);
        Assert.Equal(DateTime.Parse("2024-02-16T12:00:00"), result.CreatedAt);
        Assert.Equal(DateTimeOffset.Parse("2024-02-16T12:00:00+02:00"), result.ModifiedAt);
        Assert.Equal(TimeSpan.Parse("02:30:00"), result.Duration);
        Assert.Equal(new Uri("https://example.com"), result.Website);
    }

    enum Status { Active, Inactive, Pending }
    record EnumContainer(Status Status);

    [Fact]
    public void Parse_Enum_Success()
    {
        var yaml = "status: Active";
        var result = _parser.Parse<EnumContainer>(yaml);

        Assert.NotNull(result);
        Assert.Equal(Status.Active, result.Status);
    }

    [Fact]
    public void Parse_EnumCaseInsensitive_Success()
    {
        var yaml = "status: active";
        var result = _parser.Parse<EnumContainer>(yaml);

        Assert.NotNull(result);
        Assert.Equal(Status.Active, result.Status);
    }

    record IgnoredProps(
        string Name,
        [property: YamlIgnore] string? Secret
    );

    [Fact]
    public void Parse_IgnoredProperty_Success()
    {
        var yaml = @"
            name: Test
            secret: should-be-ignored";

        var result = _parser.Parse<IgnoredProps>(yaml);

        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.Null(result.Secret);
    }

    record ListContainer(
        List<string> StringList,
        IList<int> IntList,
        ICollection<double> DoubleCollection,
        IEnumerable<bool> BoolEnumerable
    );

    [Fact]
    public void Parse_DifferentCollectionTypes_Success()
    {
        var yaml = @"
            string-list:
                - one
                - two
            int-list:
                - 1
                - 2
            double-collection:
                - 1.1
                - 2.2
            bool-enumerable:
                - true
                - false";

        var result = _parser.Parse<ListContainer>(yaml);

        Assert.NotNull(result);
        Assert.Equal(["one", "two"], result.StringList);
        Assert.Equal([1, 2], result.IntList);
        Assert.Equal([1.1, 2.2], result.DoubleCollection);
        Assert.Equal([true, false], result.BoolEnumerable);
    }

    record RequiredProps(bool Required);

    [Fact]
    public void Parse_MissingRequiredProperty_ThrowsException()
    {
        var yaml = "optional: value";
        Assert.Throws<YamlParseException>(() => _parser.Parse<RequiredProps>(yaml));
    }

    [Fact]
    public void Parse_InvalidTypeConversion_ThrowsException()
    {
        var yaml = "required: 3.2";
        Assert.Throws<YamlParseException>(() => _parser.Parse<RequiredProps>(yaml));
    }

    record ComplexNested(
        Dictionary<string, List<NestedItem>> Items,
        Dictionary<string, Dictionary<string, int>> Metrics
    );
    record NestedItem(string Name, int Value);

    [Fact]
    public void Parse_ComplexNestedStructure_Success()
    {
        var yaml = @"
            items:
                group1:
                    - name: item1
                      value: 1
                    - name: item2
                      value: 2
                group2:
                    - name: item3
                      value: 3
            metrics:
                section1:
                    metric1: 10
                    metric2: 20
                section2:
                    metric3: 30";

        var result = _parser.Parse<ComplexNested>(yaml);

        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.Items["group1"].Count);
        Assert.Single(result.Items["group2"]);
        Assert.Equal("item1", result.Items["group1"][0].Name);
        Assert.Equal(3, result.Items["group2"][0].Value);
        Assert.Equal(10, result.Metrics["section1"]["metric1"]);
        Assert.Equal(30, result.Metrics["section2"]["metric3"]);
    }

    public record TestRecord(string Name, int Age);

    [Fact]
    public void ParseFile_Success()
    {
        var yamlContent = "name: Test\nage: 42";
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, yamlContent);

            var result = _parser.ParseFile<TestRecord>(tempFile);

            Assert.NotNull(result);
            Assert.Equal("Test", result.Name);
            Assert.Equal(42, result.Age);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Parse_Stream_Success()
    {
        var yamlContent = "name: StreamUser\nage: 30";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(yamlContent));

        var result = _parser.Parse<TestRecord>(stream);

        Assert.NotNull(result);
        Assert.Equal("StreamUser", result.Name);
        Assert.Equal(30, result.Age);
    }

    [Fact]
    public async Task Parse_VariableResolution_ScalarString_Success()
    {
        _parser.AddVariableResolver(new Regex(@"\$\{([^}]+)\}"), (varName) => {
            return varName == "name" ? "World" : varName;
        });

        var yaml = "name: Hello, ${name}";

        var result = await _parser.ParseAsync<SingleProp>(yaml);

        Assert.NotNull(result);
        Assert.Equal("Hello, World", result.Name);
    }

    record NestedVar(string Info);
    record ContainerWithNested(NestedVar Nested);

    [Fact]
    public async Task Parse_VariableResolution_NestedObject_Success()
    {
        _parser.AddVariableResolver(new Regex(@"\$\{([^}]+)\}"), (varName) => {
            return varName == "info" ? "NestedValue" : varName;
        });

        var yaml = @"
            nested:
              info: Value is ${info}";
        var result = await _parser.ParseAsync<ContainerWithNested>(yaml);

        Assert.NotNull(result);
        Assert.Equal("Value is NestedValue", result.Nested.Info);
    }
}
