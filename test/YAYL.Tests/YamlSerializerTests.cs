using YAYL.Attributes;

namespace YAYL.Tests;

public partial class YamlSerializerTests
{
    private readonly YamlSerializer _serializer = new YamlSerializer();
    private readonly YamlParser _parser = new YamlParser();

    record SingleProp(string Name);

    [Fact]
    public void Serialize_SinglePropertyString_Success()
    {
        var obj = new SingleProp("John Doe");

        var yaml = _serializer.Serialize(obj);

        var expected =
        "name: John Doe";
        Assert.Equal(expected, yaml.Trim());

        var parsed = _parser.Parse<SingleProp>(yaml);
        Assert.Equal(obj.Name, parsed?.Name);
    }

    record SinglePropRename([property: YamlPropertyName("da-name")] string Name);

    [Fact]
    public void Serialize_SinglePropertyString_Rename_Success()
    {
        var obj = new SinglePropRename("John Doe");

        var yaml = _serializer.Serialize(obj);

        var expected =
        "da-name: John Doe";
        Assert.Equal(expected, yaml.Trim());

        var parsed = _parser.Parse<SinglePropRename>(yaml);
        Assert.Equal(obj.Name, parsed?.Name);
    }

    record SinglePropInt(int Age);

    [Fact]
    public void Serialize_SinglePropertyInteger_Success()
    {
        var obj = new SinglePropInt(30);

        var yaml = _serializer.Serialize(obj);

        var expected =
        "age: 30";
        Assert.Equal(expected, yaml.Trim());

        var parsed = _parser.Parse<SinglePropInt>(yaml);
        Assert.Equal(obj.Age, parsed?.Age);
    }

    record SinglePropBool(bool IsActive);

    [Fact]
    public void Serialize_SinglePropertyBoolean_Success()
    {
        var obj = new SinglePropBool(true);

        var yaml = _serializer.Serialize(obj);

        var expected =
        "is-active: true";
        Assert.Equal(expected, yaml.Trim());

        var parsed = _parser.Parse<SinglePropBool>(yaml);
        Assert.Equal(obj.IsActive, parsed?.IsActive);
    }

    record SinglePropDouble(double Score);

    [Fact]
    public void Serialize_SinglePropertyDouble_Success()
    {
        var obj = new SinglePropDouble(85.5);

        var yaml = _serializer.Serialize(obj);

        var expected =
        "score: 85.5";
        Assert.Equal(expected, yaml.Trim());

        var parsed = _parser.Parse<SinglePropDouble>(yaml);
        Assert.Equal(obj.Score, parsed?.Score);
    }

    record SimpleArray(string[] Tags);

    [Fact]
    public void Serialize_SimpleArray_Success()
    {
        var obj = new SimpleArray(["tag1", "tag2", "tag3"]);

        var yaml = _serializer.Serialize(obj);

        var expected =
        "tags:\n" +
        "- tag1\n" +
        "- tag2\n" +
        "- tag3";
        Assert.Equal(expected, yaml.Trim());

        var parsed = _parser.Parse<SimpleArray>(yaml);
        Assert.NotNull(parsed);
        Assert.Equal(3, parsed.Tags.Length);
        Assert.Equal(obj.Tags, parsed.Tags);
    }

    record SimpleDictionary(Dictionary<string, string> Metadata);

    [Fact]
    public void Serialize_Dictionary_Success()
    {
        var obj = new SimpleDictionary(new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2",
            ["key3"] = "value3"
        });

        var yaml = _serializer.Serialize(obj);

        var expected =
        "metadata:\n" +
        "  key1: value1\n" +
        "  key2: value2\n" +
        "  key3: value3";
        Assert.Equal(expected, yaml.Trim());

        var parsed = _parser.Parse<SimpleDictionary>(yaml);
        Assert.NotNull(parsed);
        Assert.Equal(3, parsed.Metadata.Count);
        foreach (var key in obj.Metadata.Keys)
        {
            Assert.Equal(obj.Metadata[key], parsed.Metadata[key]);
        }
    }

    record SimpleIntDictionary(Dictionary<string, int> Metadata);

    [Fact]
    public void Serialize_IntDictionary_Success()
    {
        var obj = new SimpleIntDictionary(new Dictionary<string, int>
        {
            ["key1"] = 1,
            ["key2"] = 2,
            ["key3"] = 3
        });

        var yaml = _serializer.Serialize(obj);

        var expected =
        "metadata:\n" +
        "  key1: 1\n" +
        "  key2: 2\n" +
        "  key3: 3";
        Assert.Equal(expected, yaml.Trim());

        var parsed = _parser.Parse<SimpleIntDictionary>(yaml);
        Assert.NotNull(parsed);
        Assert.Equal(3, parsed.Metadata.Count);
        foreach (var key in obj.Metadata.Keys)
        {
            Assert.Equal(obj.Metadata[key], parsed.Metadata[key]);
        }
    }

    record NestedObject(Nested Nested);
    record Nested(string Description, int Priority);

    [Fact]
    public void Serialize_NestedObject_Success()
    {
        var obj = new NestedObject(new Nested("Test description", 1));

        var yaml = _serializer.Serialize(obj);

        var expected =
        "nested:\n" +
        "  description: Test description\n" +
        "  priority: 1";
        Assert.Equal(expected, yaml.Trim());

        var parsed = _parser.Parse<NestedObject>(yaml);
        Assert.NotNull(parsed);
        Assert.NotNull(parsed.Nested);
        Assert.Equal(obj.Nested.Description, parsed.Nested.Description);
        Assert.Equal(obj.Nested.Priority, parsed.Nested.Priority);
    }

    record NestedObjectArray(Nested[] Nested);

    [Fact]
    public void SerializeNestedArray_Success()
    {
        var obj = new NestedObjectArray(new[]
        {
            new Nested("Test description 1", 1),
            new Nested("Test description 2", 2)
        });

        var yaml = _serializer.Serialize(obj);

        var expected =
        "nested:\n" +
        "- description: Test description 1\n" +
        "  priority: 1\n" +
        "- description: Test description 2\n" +
        "  priority: 2";
        Assert.Equal(expected, yaml.Trim());

        var parsed = _parser.Parse<NestedObjectArray>(yaml);
        Assert.NotNull(parsed);
        Assert.Equal(2, parsed.Nested.Length);
        for (int i = 0; i < obj.Nested.Length; i++)
        {
            Assert.Equal(obj.Nested[i].Description, parsed.Nested[i].Description);
            Assert.Equal(obj.Nested[i].Priority, parsed.Nested[i].Priority);
        }
    }

    record NullableProps(int? OptionalAge, string? OptionalName, double? OptionalScore);

    [Fact]
    public void Serialize_NullableProperties_Success()
    {
        var obj = new NullableProps(25, null, null);

        var yaml = _serializer.Serialize(obj);

        var expected =
        "optional-age: 25";
        Assert.Equal(expected, yaml.Trim());

        var parsed = _parser.Parse<NullableProps>(yaml);
        Assert.NotNull(parsed);
        Assert.Equal(25, parsed.OptionalAge);
        Assert.Null(parsed.OptionalName);
        Assert.Null(parsed.OptionalScore);
    }

    record ConfigItem(string DatabaseName, int PortNumber);

    [Fact]
    public void Serialize_SnakeCaseNaming_Success()
    {
        var serializer = new YamlSerializer(YamlNamingPolicy.SnakeCaseLower);
        var obj = new ConfigItem("testdb", 5432);

        var yaml = serializer.Serialize(obj);

        var expected =
        "database_name: testdb\n" +
        "port_number: 5432";
        Assert.Equal(expected, yaml.Trim());

        var parser = new YamlParser(YamlNamingPolicy.SnakeCaseLower);
        var parsed = parser.Parse<ConfigItem>(yaml);
        Assert.NotNull(parsed);
        Assert.Equal(obj.DatabaseName, parsed.DatabaseName);
        Assert.Equal(obj.PortNumber, parsed.PortNumber);
    }

    [Fact]
    public void Serialize_CamelCaseNaming_Success()
    {
        var serializer = new YamlSerializer(YamlNamingPolicy.CamelCase);
        var obj = new ConfigItem("testdb", 5432);

        var yaml = serializer.Serialize(obj);

        var expected =
        "databaseName: testdb\n" +
        "portNumber: 5432";
        Assert.Equal(expected, yaml.Trim());

        var parser = new YamlParser(YamlNamingPolicy.CamelCase);
        var parsed = parser.Parse<ConfigItem>(yaml);
        Assert.NotNull(parsed);
        Assert.Equal(obj.DatabaseName, parsed.DatabaseName);
        Assert.Equal(obj.PortNumber, parsed.PortNumber);
    }

    record SpecialTypes(
        Guid Id,
        DateTime CreatedAt,
        DateTimeOffset ModifiedAt,
        TimeSpan Duration,
        Uri Website
    );

    [Fact]
    public void Serialize_SpecialTypes_Success()
    {
        var obj = new SpecialTypes(
            Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
            DateTime.Parse("2024-02-16T12:00:00"),
            DateTimeOffset.Parse("2024-02-16T12:00:00+02:00"),
            TimeSpan.Parse("02:30:00"),
            new Uri("https://example.com")
        );

        var yaml = _serializer.Serialize(obj);

        Assert.Contains("id: 550e8400-e29b-41d4-a716-446655440000", yaml);
        Assert.Contains("created-at:", yaml);
        Assert.Contains("modified-at:", yaml);
        Assert.Contains("duration:", yaml);
        Assert.Contains("website: https://example.com/", yaml);

        var parsed = _parser.Parse<SpecialTypes>(yaml);
        Assert.NotNull(parsed);
        Assert.Equal(obj.Id, parsed.Id);
        Assert.Equal(obj.Website, parsed.Website);
    }

    enum Status { Active, Inactive, Pending }
    record EnumContainer(Status Status);

    [Fact]
    public void Serialize_Enum_Success()
    {
        var obj = new EnumContainer(Status.Active);

        var yaml = _serializer.Serialize(obj);

        var expected =
        "status: active";
        Assert.Equal(expected, yaml.Trim());

        var parsed = _parser.Parse<EnumContainer>(yaml);
        Assert.NotNull(parsed);
        Assert.Equal(Status.Active, parsed.Status);
    }

    record IgnoredProps(
        string Name,
        [property: YamlIgnore] string? Secret
    );

    [Fact]
    public void Serialize_IgnoredProperty_Success()
    {
        var obj = new IgnoredProps("Test", "should-be-ignored");

        var yaml = _serializer.Serialize(obj);

        var expected =
        "name: Test";
        Assert.Equal(expected, yaml.Trim());

        var parsed = _parser.Parse<IgnoredProps>(yaml);
        Assert.NotNull(parsed);
        Assert.Equal("Test", parsed.Name);
        Assert.Null(parsed.Secret);
    }

    record ListContainer(
        List<string> StringList,
        IList<int> IntList,
        ICollection<double> DoubleCollection,
        IEnumerable<bool> BoolEnumerable
    );

    [Fact]
    public void Serialize_DifferentCollectionTypes_Success()
    {
        var obj = new ListContainer(
            new List<string> { "one", "two" },
            new List<int> { 1, 2 },
            new List<double> { 1.1, 2.2 },
            new List<bool> { true, false }
        );

        var yaml = _serializer.Serialize(obj);

        var expected =
        "string-list:\n" +
        "- one\n" +
        "- two\n" +
        "int-list:\n" +
        "- 1\n" +
        "- 2\n" +
        "double-collection:\n" +
        "- 1.1\n" +
        "- 2.2\n" +
        "bool-enumerable:\n" +
        "- true\n" +
        "- false";
        Assert.Equal(expected, yaml.Trim());

        var parsed = _parser.Parse<ListContainer>(yaml);
        Assert.NotNull(parsed);
        Assert.Equal(obj.StringList.Count, parsed.StringList.Count);
        Assert.Equal(obj.IntList.Count, parsed.IntList.Count);
    }

    record ComplexNested(
        Dictionary<string, List<NestedItem>> Items,
        Dictionary<string, Dictionary<string, int>> Metrics
    );
    record NestedItem(string Name, int Value);

    [Fact]
    public void Serialize_ComplexNestedStructure_Success()
    {
        var obj = new ComplexNested(
            new Dictionary<string, List<NestedItem>>
            {
                ["group1"] = new List<NestedItem>
                {
                    new NestedItem("item1", 1),
                    new NestedItem("item2", 2)
                },
                ["group2"] = new List<NestedItem>
                {
                    new NestedItem("item3", 3)
                }
            },
            new Dictionary<string, Dictionary<string, int>>
            {
                ["section1"] = new Dictionary<string, int>
                {
                    ["metric1"] = 10,
                    ["metric2"] = 20
                },
                ["section2"] = new Dictionary<string, int>
                {
                    ["metric3"] = 30
                }
            }
        );

        var yaml = _serializer.Serialize(obj);

        var expected =
        "items:\n" +
        "  group1:\n" +
        "  - name: item1\n" +
        "    value: 1\n" +
        "  - name: item2\n" +
        "    value: 2\n" +
        "  group2:\n" +
        "  - name: item3\n" +
        "    value: 3\n" +
        "metrics:\n" +
        "  section1:\n" +
        "    metric1: 10\n" +
        "    metric2: 20\n" +
        "  section2:\n" +
        "    metric3: 30";
        Assert.Equal(expected, yaml.Trim());

        var parsed = _parser.Parse<ComplexNested>(yaml);
        Assert.NotNull(parsed);
        Assert.Equal(2, parsed.Items.Count);
        Assert.Equal(2, parsed.Items["group1"].Count);
        Assert.Equal("item1", parsed.Items["group1"][0].Name);
        Assert.Equal(3, parsed.Items["group2"][0].Value);
    }
}
