using YAYL.Attributes;

namespace YAYL.Tests;

public partial class YamlParserTests
{
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

    [Fact]
    public void Parse_PolymorphicArray_Success()
    {
        var yaml = @"
            - type: circle
              name: My Circle
              radius: 3.0
            - type: rectangle
              name: My Rectangle
              width: 2.0
              height: 3.0";

        var result = _parser.Parse<Shape[]>(yaml);

        Assert.NotNull(result);
        Assert.Equal(2, result.Length);

        Assert.IsType<Circle>(result[0]);
        var circle = (Circle)result[0];
        Assert.Equal("My Circle", circle.Name);
        Assert.Equal(3.0, circle.Radius);

        Assert.IsType<Rectangle>(result[1]);
        var rectangle = (Rectangle)result[1];
        Assert.Equal("My Rectangle", rectangle.Name);
        Assert.Equal(2.0, rectangle.Width);
        Assert.Equal(3.0, rectangle.Height);
    }

    [Fact]
    public void Parse_Polymorphic_Dictionary_Success()
    {
        var yaml = @"
            circle:
              type: circle
              name: My Circle
              radius: 3.0
            rectangle:
              type: rectangle
              name: My Rectangle
              width: 2.0
              height: 3.0";

        var result = _parser.Parse<Dictionary<string, Shape>>(yaml);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        Assert.IsType<Circle>(result["circle"]);
        var circle = (Circle)result["circle"];
        Assert.Equal("My Circle", circle.Name);
        Assert.Equal(3.0, circle.Radius);

        Assert.IsType<Rectangle>(result["rectangle"]);
        var rectangle = (Rectangle)result["rectangle"];
        Assert.Equal("My Rectangle", rectangle.Name);
        Assert.Equal(2.0, rectangle.Width);
        Assert.Equal(3.0, rectangle.Height);
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
    public void Parse_PolymorphicNested_Success()
    {
        var yaml = @"
            kingdom: animal
            class: mammal
            species: Dog";

        var result = _parser.Parse<Organism>(yaml);

        Assert.NotNull(result);
        Assert.IsType<Mammal>(result);
        var mammal = (Mammal)result;
        Assert.Equal("animal", mammal.Kingdom);
        Assert.Equal("mammal", mammal.AnimalClass);
        Assert.Equal("Dog", mammal.Species);
    }

    [YamlPolymorphic("type")]
    [YamlDerivedType("string", typeof(StringType))]
    [YamlDerivedTypeDefault(typeof(RefType))]
    abstract record SchemaType;

    record StringType(string Pattern) : SchemaType;
    record RefType([property:YamlPropertyName("$ref")] string Ref) : SchemaType;

    [Fact]
    public void Parse_PolymorphicDefault_Success()
    {
        var yaml = @"
            - type: string
              pattern: ^[a-zA-Z0-9]+$
            - $ref: '#/components/schemas/Cat'";

        var result = _parser.Parse<SchemaType[]>(yaml);

        Assert.NotNull(result);
        Assert.Equal(2, result.Length);
        Assert.IsType<StringType>(result[0]);
        var stringType = (StringType)result[0];
        Assert.Equal("^[a-zA-Z0-9]+$", stringType.Pattern);
        Assert.IsType<RefType>(result[1]);
        var refType = (RefType)result[1];
        Assert.Equal("#/components/schemas/Cat", refType.Ref);
    }
}
