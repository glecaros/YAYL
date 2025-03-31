using YAYL.Attributes;

namespace YAYL.Tests;

public partial class YamlSerializerTests
{
    [YamlPolymorphic("type")]
    [YamlDerivedType("circle", typeof(Circle))]
    [YamlDerivedType("rectangle", typeof(Rectangle))]
    public abstract record Shape(string Name);

    public record Circle(string Name, double Radius) : Shape(Name);
    public record Rectangle(string Name, double Width, double Height) : Shape(Name);

    [Fact]
    public void Serialize_Polymorphic_Circle_Success()
    {
        Shape shape = new Circle("My Circle", 5.0);

        var yaml = _serializer.Serialize(shape);

        Assert.Contains("type: circle", yaml);
        Assert.Contains("name: My Circle", yaml);
        Assert.Contains("radius: 5", yaml);

        var parsed = _parser.Parse<Shape>(yaml);
        Assert.IsType<Circle>(parsed);
        var circle = (Circle)parsed;
        Assert.Equal("My Circle", circle.Name);
        Assert.Equal(5.0, circle.Radius);
    }

    [Fact]
    public void Serialize_Polymorphic_Rectangle_Success()
    {
        Shape shape = new Rectangle("My Rectangle", 10.0, 20.0);

        var yaml = _serializer.Serialize(shape);

        Assert.Contains("type: rectangle", yaml);
        Assert.Contains("name: My Rectangle", yaml);
        Assert.Contains("width: 10", yaml);
        Assert.Contains("height: 20", yaml);

        var parsed = _parser.Parse<Shape>(yaml);
        Assert.IsType<Rectangle>(parsed);
        var rectangle = (Rectangle)parsed;
        Assert.Equal("My Rectangle", rectangle.Name);
        Assert.Equal(10.0, rectangle.Width);
        Assert.Equal(20.0, rectangle.Height);
    }

    [Fact]
    public void Serialize_PolymorphicArray_Success()
    {
        Shape[] shapes =
        [
            new Circle("My Circle", 3.0),
            new Rectangle("My Rectangle", 2.0, 3.0)
        ];

        var yaml = _serializer.Serialize(shapes);

        Assert.Contains("- type: circle", yaml);
        Assert.Contains("name: My Circle", yaml);
        Assert.Contains("radius: 3", yaml);
        Assert.Contains("- type: rectangle", yaml);
        Assert.Contains("name: My Rectangle", yaml);
        Assert.Contains("width: 2", yaml);
        Assert.Contains("height: 3", yaml);

        var parsed = _parser.Parse<Shape[]>(yaml);
        Assert.NotNull(parsed);
        Assert.Equal(2, parsed.Length);
        Assert.IsType<Circle>(parsed[0]);
        Assert.IsType<Rectangle>(parsed[1]);
    }

    [Fact]
    public void Serialize_Polymorphic_Dictionary_Success()
    {
        var shapeDictionary = new Dictionary<string, Shape>
        {
            ["circle"] = new Circle("My Circle", 3.0),
            ["rectangle"] = new Rectangle("My Rectangle", 2.0, 3.0)
        };

        var yaml = _serializer.Serialize(shapeDictionary);

        Assert.Contains("circle:", yaml);
        Assert.Contains("type: circle", yaml);
        Assert.Contains("name: My Circle", yaml);
        Assert.Contains("radius: 3", yaml);
        Assert.Contains("rectangle:", yaml);
        Assert.Contains("type: rectangle", yaml);
        Assert.Contains("name: My Rectangle", yaml);
        Assert.Contains("width: 2", yaml);
        Assert.Contains("height: 3", yaml);

        var parsed = _parser.Parse<Dictionary<string, Shape>>(yaml);
        Assert.NotNull(parsed);
        Assert.Equal(2, parsed.Count);
        Assert.IsType<Circle>(parsed["circle"]);
        Assert.IsType<Rectangle>(parsed["rectangle"]);
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
    public void Serialize_PolymorphicEnumFirstVariant_Success()
    {
        PetInfo pet = new DogInfo("Luna", PetType.Dog, "Samoyed");

        var yaml = _serializer.Serialize(pet);

        Assert.Contains("type: dog", yaml);
        Assert.Contains("name: Luna", yaml);
        Assert.Contains("breed: Samoyed", yaml);

        var parsed = _parser.Parse<PetInfo>(yaml);
        Assert.IsType<DogInfo>(parsed);
        var dog = (DogInfo)parsed;
        Assert.Equal("Luna", dog.Name);
        Assert.Equal(PetType.Dog, dog.Type);
        Assert.Equal("Samoyed", dog.Breed);
    }

    [Fact]
    public void Serialize_PolymorphicEnumSecondVariant_Success()
    {
        PetInfo pet = new CatInfo("Violeta", PetType.Cat, true);

        var yaml = _serializer.Serialize(pet);

        Assert.Contains("type: cat", yaml);
        Assert.Contains("name: Violeta", yaml);
        Assert.Contains("indoor: true", yaml);

        var parsed = _parser.Parse<PetInfo>(yaml);
        Assert.IsType<CatInfo>(parsed);
        var cat = (CatInfo)parsed;
        Assert.Equal("Violeta", cat.Name);
        Assert.Equal(PetType.Cat, cat.Type);
        Assert.True(cat.Indoor);
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
    public void Serialize_PolymorphicEnumMultiWord_Success()
    {
        Dice[] dice =
        [
            new SixSidedDice("D6", DiceType.SixSided),
            new TenSidedDice("D10", DiceType.TenSided),
            new TwentySidedDice("D20", DiceType.TwentySided)
        ];

        var yaml = _serializer.Serialize(dice);

        Assert.Contains("type: six-sided", yaml);
        Assert.Contains("name: D6", yaml);
        Assert.Contains("type: ten-sided", yaml);
        Assert.Contains("name: D10", yaml);
        Assert.Contains("type: twenty-sided", yaml);
        Assert.Contains("name: D20", yaml);

        var parsed = _parser.Parse<Dice[]>(yaml);
        Assert.NotNull(parsed);
        Assert.Equal(3, parsed.Length);
        Assert.IsType<SixSidedDice>(parsed[0]);
        Assert.IsType<TenSidedDice>(parsed[1]);
        Assert.IsType<TwentySidedDice>(parsed[2]);
    }

    [YamlPolymorphic("kingdom")]
    [YamlDerivedType("animal", typeof(Animal))]
    [YamlDerivedType("plant", typeof(Plant))]
    abstract record Organism(string Kingdom);

    [YamlPolymorphic("class")]
    [YamlDerivedType("mammal", typeof(Mammal))]
    [YamlDerivedType("bird", typeof(Bird))]
    abstract record Animal(string Kingdom, [property: YamlPropertyName("class")] string AnimalClass) : Organism(Kingdom);

    record Mammal(string Kingdom, string AnimalClass, string Species) : Animal(Kingdom, AnimalClass);
    record Bird(string Kingdom, string AnimalClass, string Species, bool Flight) : Animal(Kingdom, AnimalClass);

    [YamlPolymorphic("class")]
    record Plant(string Kingdom, string Name, string Species) : Organism(Kingdom);

    [Fact]
    public void Serialize_PolymorphicNested_Success()
    {
        Organism organism = new Mammal("animal", "mammal", "Dog");

        var yaml = _serializer.Serialize(organism);

        Assert.Contains("kingdom: animal", yaml);
        Assert.Contains("class: mammal", yaml);
        Assert.Contains("species: Dog", yaml);

        var parsed = _parser.Parse<Organism>(yaml);
        Assert.IsType<Mammal>(parsed);
        var mammal = (Mammal)parsed;
        Assert.Equal("animal", mammal.Kingdom);
        Assert.Equal("mammal", mammal.AnimalClass);
        Assert.Equal("Dog", mammal.Species);
    }

    [Fact]
    public void Serialize_PolymorphicNestedBird_Success()
    {
        Organism organism = new Bird("animal", "bird", "Eagle", true);

        var yaml = _serializer.Serialize(organism);

        Assert.Contains("kingdom: animal", yaml);
        Assert.Contains("class: bird", yaml);
        Assert.Contains("species: Eagle", yaml);
        Assert.Contains("flight: true", yaml);

        var parsed = _parser.Parse<Organism>(yaml);
        Assert.IsType<Bird>(parsed);
        var bird = (Bird)parsed;
        Assert.Equal("animal", bird.Kingdom);
        Assert.Equal("bird", bird.AnimalClass);
        Assert.Equal("Eagle", bird.Species);
        Assert.True(bird.Flight);
    }

    [Fact]
    public void Serialize_PolymorphicPlant_Success()
    {
        Organism organism = new Plant("plant", "Venus Flytrap", "Dionaea muscipula");

        var yaml = _serializer.Serialize(organism);

        Assert.Contains("kingdom: plant", yaml);
        Assert.Contains("name: Venus Flytrap", yaml);
        Assert.Contains("species: Dionaea muscipula", yaml);

        var parsed = _parser.Parse<Organism>(yaml);
        Assert.IsType<Plant>(parsed);
        var plant = (Plant)parsed;
        Assert.Equal("plant", plant.Kingdom);
        Assert.Equal("Venus Flytrap", plant.Name);
        Assert.Equal("Dionaea muscipula", plant.Species);
    }

    public record Document(Defaults? Defaults);

    public record Defaults(
        Authentication? Authentication
    );

    [YamlPolymorphic("type")]
    [YamlDerivedType("api-key", typeof(ApiKeyAuthentication))]
    [YamlDerivedType("azure-credentials", typeof(AzureCredentialsAuthentication))]
    public record Authentication();

    public record AzureCredentialsAuthentication(string[] Scopes) : Authentication;

    public record ApiKeyAuthentication(string Key) : Authentication
    {
        public string? Header { get; init; }
        public string? Prefix { get; init; }
    }

    [Fact]
    public void Serialize_ComplexPolymorphic_Success()
    {
        var document = new Document(
            new Defaults(
                new ApiKeyAuthentication("secret-api-key")
                {
                    Header = "X-Api-Key",
                    Prefix = "Bearer"
                }
            )
        );

        var yaml = _serializer.Serialize(document);

        Assert.Contains("defaults:", yaml);
        Assert.Contains("authentication:", yaml);
        Assert.Contains("type: api-key", yaml);
        Assert.Contains("key: secret-api-key", yaml);
        Assert.Contains("header: X-Api-Key", yaml);
        Assert.Contains("prefix: Bearer", yaml);

        var parsed = _parser.Parse<Document>(yaml);
        Assert.NotNull(parsed);
        Assert.NotNull(parsed.Defaults);
        Assert.NotNull(parsed.Defaults.Authentication);
        Assert.IsType<ApiKeyAuthentication>(parsed.Defaults.Authentication);

        var auth = (ApiKeyAuthentication)parsed.Defaults.Authentication;
        Assert.Equal("secret-api-key", auth.Key);
        Assert.Equal("X-Api-Key", auth.Header);
        Assert.Equal("Bearer", auth.Prefix);
    }

    [Fact]
    public void Serialize_ComplexPolymorphicAlternative_Success()
    {
        var document = new Document(
            new Defaults(
                new AzureCredentialsAuthentication(new[] { "https://management.azure.com/.default" })
            )
        );

        var yaml = _serializer.Serialize(document);

        Assert.Contains("defaults:", yaml);
        Assert.Contains("authentication:", yaml);
        Assert.Contains("type: azure-credentials", yaml);
        Assert.Contains("scopes:", yaml);
        Assert.Contains("- https://management.azure.com/.default", yaml);

        var parsed = _parser.Parse<Document>(yaml);
        Assert.NotNull(parsed);
        Assert.NotNull(parsed.Defaults);
        Assert.NotNull(parsed.Defaults.Authentication);
        Assert.IsType<AzureCredentialsAuthentication>(parsed.Defaults.Authentication);

        var auth = (AzureCredentialsAuthentication)parsed.Defaults.Authentication;
        Assert.Single(auth.Scopes);
        Assert.Equal("https://management.azure.com/.default", auth.Scopes[0]);
    }

    record Container(List<Shape> Shapes, PetInfo Pet);

    [Fact]
    public void Serialize_MixedPolymorphicTypes_Success()
    {
        var container = new Container(
            [
                new Circle("Small Circle", 1.0),
                new Rectangle("Big Rectangle", 10.0, 20.0)
            ],
            new DogInfo("Rex", PetType.Dog, "German Shepherd")
        );

        var yaml = _serializer.Serialize(container);

        Assert.Contains("shapes:", yaml);
        Assert.Contains("- type: circle", yaml);
        Assert.Contains("name: Small Circle", yaml);
        Assert.Contains("radius: 1", yaml);
        Assert.Contains("- type: rectangle", yaml);
        Assert.Contains("name: Big Rectangle", yaml);
        Assert.Contains("width: 10", yaml);
        Assert.Contains("height: 20", yaml);
        Assert.Contains("pet:", yaml);
        Assert.Contains("type: dog", yaml);
        Assert.Contains("name: Rex", yaml);
        Assert.Contains("breed: German Shepherd", yaml);

        var parsed = _parser.Parse<Container>(yaml);
        Assert.NotNull(parsed);
        Assert.Equal(2, parsed.Shapes.Count);
        Assert.IsType<Circle>(parsed.Shapes[0]);
        Assert.IsType<Rectangle>(parsed.Shapes[1]);
        Assert.IsType<DogInfo>(parsed.Pet);
    }
}
