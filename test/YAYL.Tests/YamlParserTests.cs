using System.Text;
using System.Text.RegularExpressions;
using YAYL.Attributes;

namespace YAYL.Tests;

public class YamlParserTests
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


    [YamlPolymorphic("type")]
    [YamlDerivedType("circle", typeof(Circle))]
    [YamlDerivedType("rectangle", typeof(Rectangle))]
    public abstract record Shape(string Name);

    public record Circle(string Name, double Radius) : Shape(Name);
    public record Rectangle(string Name, double Width, double Height) : Shape(Name);

    [Fact]
    public void Parse_Polymorphic_Circle_Success()
    {
        var yaml = @"
            type: circle
            name: My Circle
            radius: 5.0";

        var result = _parser.Parse<Shape>(yaml);

        Assert.NotNull(result);
        Assert.IsType<Circle>(result);
        var circle = (Circle)result;
        Assert.Equal("My Circle", circle.Name);
        Assert.Equal(5.0, circle.Radius);
    }

    [Fact]
    public void Parse_Polymorphic_Rectangle_Success()
    {
        var yaml = @"
            type: rectangle
            name: My Rectangle
            width: 10.0
            height: 20.0";

        var result = _parser.Parse<Shape>(yaml);

        Assert.NotNull(result);
        Assert.IsType<Rectangle>(result);
        var rectangle = (Rectangle)result;
        Assert.Equal("My Rectangle", rectangle.Name);
        Assert.Equal(10.0, rectangle.Width);
        Assert.Equal(20.0, rectangle.Height);
    }

    [Fact]
    public void Parse_Polymorphic_InvalidType_ThrowsException()
    {
        var yaml = @"
            type: triangle
            name: My Triangle";

        Assert.Throws<YamlParseException>(() => _parser.Parse<Shape>(yaml));
    }

    [Fact]
    public void Parse_Polymorphic_MissingType_ThrowsException()
    {
        var yaml = @"
            name: My Shape";

        Assert.Throws<YamlParseException>(() => _parser.Parse<Shape>(yaml));
    }

    enum PetType
    {
        Dog,
        Cat,
    }

    [YamlPolymorphic("type")]
    [YamlDerivedTypeEnum<PetType>(PetType.Dog, typeof(DogInfo))]
    [YamlDerivedTypeEnum<PetType>(PetType.Cat, typeof(CatInfo))]
    abstract record PetInfo(string Name, PetType Type);

    record DogInfo(string Name, PetType Type, string Breed) : PetInfo(Name, Type);
    record CatInfo(string Name, PetType Type, bool Indoor) : PetInfo(Name, Type);

    [Fact]
    public void Parse_PolymorphicEnumFirstVariant_Success()
    {
        var yaml = @"
            type: dog
            name: Luna
            breed: Samoyed";

        var result = _parser.Parse<PetInfo>(yaml);

        Assert.NotNull(result);
        Assert.IsType<DogInfo>(result);
        var dog = (DogInfo)result;
        Assert.Equal("Luna", dog.Name);
        Assert.Equal(PetType.Dog, dog.Type);
        Assert.Equal("Samoyed", dog.Breed);
    }

    [Fact]
    public void Parse_PolymorphicEnumSecondVariant_Success()
    {
        var yaml = @"
            type: cat
            name: Violeta
            indoor: true";

        var result = _parser.Parse<PetInfo>(yaml);

        Assert.NotNull(result);
        Assert.IsType<CatInfo>(result);
        var cat = (CatInfo)result;
        Assert.Equal("Violeta", cat.Name);
        Assert.Equal(PetType.Cat, cat.Type);
        Assert.True(cat.Indoor);
    }

    [Fact]
    public void Parse_PolymorphicEnum_InvalidType_ThrowsException()
    {
        var yaml = @"
            type: bird
            name: Tweety";

        Assert.Throws<YamlParseException>(() => _parser.Parse<PetInfo>(yaml));
    }

    [Fact]
    public void Parse_PolymorphicEnum_MissingType_ThrowsException()
    {
        var yaml = @"
            name: Luna
            breed: Samoyed";

        Assert.Throws<YamlParseException>(() => _parser.Parse<PetInfo>(yaml));
    }

    enum DiceType
    {
        SixSided,
        TenSided,
        TwentySided,
    }

    [YamlPolymorphic("type")]
    [YamlDerivedTypeEnum<DiceType>(DiceType.SixSided, typeof(SixSidedDice))]
    [YamlDerivedTypeEnum<DiceType>(DiceType.TenSided, typeof(TenSidedDice))]
    [YamlDerivedTypeEnum<DiceType>(DiceType.TwentySided, typeof(TwentySidedDice))]
    abstract record Dice(string Name, DiceType Type);

    record SixSidedDice(string Name, DiceType Type) : Dice(Name, Type);
    record TenSidedDice(string Name, DiceType Type) : Dice(Name, Type);
    record TwentySidedDice(string Name, DiceType Type) : Dice(Name, Type);

    [Fact]
    public void Parse_PolymorphicEnumMultiWordFirstVariant_Success()
    {
        var yaml = @"
            type: six-sided
            name: D6";

        var result = _parser.Parse<Dice>(yaml);

        Assert.NotNull(result);
        Assert.IsType<SixSidedDice>(result);
        var dice = (SixSidedDice)result;
        Assert.Equal("D6", dice.Name);
        Assert.Equal(DiceType.SixSided, dice.Type);
    }

    [Fact]
    public void Parse_PolymorphicEnumMultiWordSecondVariant_Success()
    {
        var yaml = @"
            type: ten-sided
            name: D10";

        var result = _parser.Parse<Dice>(yaml);

        Assert.NotNull(result);
        Assert.IsType<TenSidedDice>(result);
        var dice = (TenSidedDice)result;
        Assert.Equal("D10", dice.Name);
        Assert.Equal(DiceType.TenSided, dice.Type);
    }

    [Fact]
    public void Parse_PolymorphicEnumMultiWordThirdVariant_Success()
    {
        var yaml = @"
            type: twenty-sided
            name: D20";

        var result = _parser.Parse<Dice>(yaml);

        Assert.NotNull(result);
        Assert.IsType<TwentySidedDice>(result);
        var dice = (TwentySidedDice)result;
        Assert.Equal("D20", dice.Name);
        Assert.Equal(DiceType.TwentySided, dice.Type);
    }

    [Fact]
    public void Parse_PolymorphicEnumMultiWord_InvalidType_ThrowsException()
    {
        var yaml = @"
            type: eight-sided
            name: D8";

        Assert.Throws<YamlParseException>(() => _parser.Parse<Dice>(yaml));
    }


    [YamlPolymorphic("kind")]
    [YamlDerivedType("pet", typeof(Pet))]
    [YamlDerivedType("wild", typeof(WildAnimal))]
    public abstract record Animal(string Species);

    public record Pet(string Species, string Name, string Owner) : Animal(Species);
    public record WildAnimal(string Species, string Habitat, bool Endangered) : Animal(Species);

    [Fact]
    public void Parse_PolymorphicArray_Success()
    {
        var yaml = @"
            - kind: pet
              species: Dog
              name: Rover
              owner: John
            - kind: wild
              species: Tiger
              habitat: Jungle
              endangered: true";

        var result = _parser.Parse<Animal[]>(yaml);

        Assert.NotNull(result);
        Assert.Equal(2, result.Length);

        Assert.IsType<Pet>(result[0]);
        var pet = (Pet)result[0];
        Assert.Equal("Dog", pet.Species);
        Assert.Equal("Rover", pet.Name);
        Assert.Equal("John", pet.Owner);

        Assert.IsType<WildAnimal>(result[1]);
        var wild = (WildAnimal)result[1];
        Assert.Equal("Tiger", wild.Species);
        Assert.Equal("Jungle", wild.Habitat);
        Assert.True(wild.Endangered);
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

    enum DocumentType
    {
        Simple,
        WithFooter,
    }

    [YamlPolymorphic("document-type")]
    [YamlDerivedTypeEnum<DocumentType>(DocumentType.Simple, typeof(Simple))]
    [YamlDerivedTypeEnum<DocumentType>(DocumentType.WithFooter, typeof(WithFooter))]
    abstract record Document(DocumentType DocumentType, string? Content)
    {
        public record Simple(string? Content) : Document(DocumentType.Simple, Content);
        public record WithFooter(string? Content, string? Footer) : Document(DocumentType.WithFooter, Content);
    }

    record DocumentConfig(string Name, List<string> Authors, Document Document);

    [Fact]
    public void Parse_PolymorphicDocument_Success()
    {
        var yaml = @"
            name: Sample Document
            authors:
                - Alice
                - Bob
            document:
                document-type: with-footer
                content: This is a sample document.";

        var result = _parser.Parse<DocumentConfig>(yaml);

        Assert.NotNull(result);
        Assert.Equal("Sample Document", result.Name);
        Assert.Equal(2, result.Authors.Count);
        Assert.Equal("Alice", result.Authors[0]);
        Assert.Equal("Bob", result.Authors[1]);
        Assert.IsType<Document.WithFooter>(result.Document);
        var docWithFooter = (Document.WithFooter)result.Document;
        Assert.Equal("This is a sample document.", docWithFooter.Content);
        Assert.Null(docWithFooter.Footer);
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
