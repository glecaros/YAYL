using YAYL.Attributes;

namespace YAYL.Tests;

public class YamlVariantCollectionTests
{
    public record Bar(string Baz);
    public record Other(string Field);

    public record FooWithArrayVariant
    {
        [YamlVariant]
        [YamlVariantTypeScalar(typeof(string))]
        [YamlVariantTypeScalar(typeof(int))]
        [YamlVariantTypeObject(typeof(Bar), "baz")]
        [YamlVariantTypeObject(typeof(Other), "field")]
        public object[]? Values { get; set; }
    };

    public record FooWithListVariant
    {
        [YamlVariant]
        [YamlVariantTypeScalar(typeof(string))]
        [YamlVariantTypeScalar(typeof(int))]
        [YamlVariantTypeObject(typeof(Bar), "baz")]
        [YamlVariantTypeObject(typeof(Other), "field")]
        public List<object>? Values { get; set; }
    }

    public record FooWithDictVariant
    {
        [YamlVariant]
        [YamlVariantTypeScalar(typeof(string))]
        [YamlVariantTypeScalar(typeof(int))]
        [YamlVariantTypeObject(typeof(Bar), "baz")]
        [YamlVariantTypeObject(typeof(Other), "field")]
        public Dictionary<string, object>? Values { get; set; }
    }

    [Fact]
    public void Parse_WithArrayOfMixedVariants_ParsesCorrectly()
    {
        var yaml = "values:\n" +
                   "- hello\n" +
                   "- 42\n" +
                   "- baz: test value\n" +
                   "- field: another value\n";

        var parser = new YamlParser();
        var result = parser.Parse<FooWithArrayVariant>(yaml);

        Assert.NotNull(result);
        Assert.NotNull(result.Values);
        Assert.Equal(4, result.Values.Length);

        Assert.IsType<string>(result.Values[0]);
        Assert.Equal("hello", result.Values[0]);

        Assert.IsType<int>(result.Values[1]);
        Assert.Equal(42, result.Values[1]);

        Assert.IsType<Bar>(result.Values[2]);
        var bar = (Bar)result.Values[2];
        Assert.Equal("test value", bar.Baz);

        Assert.IsType<Other>(result.Values[3]);
        var other = (Other)result.Values[3];
        Assert.Equal("another value", other.Field);
    }

    [Fact]
    public void Parse_WithListOfMixedVariants_ParsesCorrectly()
    {
        var yaml = "values:\n" +
                   "  - hello\n" +
                   "  - 42\n" +
                   "  - baz: test value\n" +
                   "  - field: another value";

        var parser = new YamlParser();
        var result = parser.Parse<FooWithListVariant>(yaml);

        Assert.NotNull(result);
        Assert.NotNull(result.Values);
        Assert.Equal(4, result.Values.Count);

        Assert.IsType<string>(result.Values[0]);
        Assert.Equal("hello", result.Values[0]);

        Assert.IsType<int>(result.Values[1]);
        Assert.Equal(42, result.Values[1]);

        Assert.IsType<Bar>(result.Values[2]);
        var bar = (Bar)result.Values[2];
        Assert.Equal("test value", bar.Baz);

        Assert.IsType<Other>(result.Values[3]);
        var other = (Other)result.Values[3];
        Assert.Equal("another value", other.Field);
    }

    [Fact]
    public void Parse_WithDictionaryOfMixedVariants_ParsesCorrectly()
    {
        var yaml = "values:\n" +
                   "  str-key: hello\n" +
                   "  int-key: 42\n" +
                   "  bar-key:\n" +
                   "    baz: test value\n" +
                   "  other-key:\n" +
                   "    field: another value";

        var parser = new YamlParser();
        var result = parser.Parse<FooWithDictVariant>(yaml);

        Assert.NotNull(result);
        Assert.NotNull(result.Values);
        Assert.Equal(4, result.Values.Count);

        Assert.IsType<string>(result.Values["str-key"]);
        Assert.Equal("hello", result.Values["str-key"]);

        Assert.IsType<int>(result.Values["int-key"]);
        Assert.Equal(42, result.Values["int-key"]);

        Assert.IsType<Bar>(result.Values["bar-key"]);
        var bar = (Bar)result.Values["bar-key"];
        Assert.Equal("test value", bar.Baz);

        Assert.IsType<Other>(result.Values["other-key"]);
        var other = (Other)result.Values["other-key"];
        Assert.Equal("another value", other.Field);
    }

    public class InvalidListVariant
    {
        [YamlVariant]
        [YamlVariantTypeScalar(typeof(string))]
        public List<string>? Values { get; set; }
    }

    [Fact]
    public void Parse_InvalidListVariantType_ThrowsException()
    {
        var yaml = "values: [test]";

        var parser = new YamlParser();

        var exception = Assert.Throws<YamlParseException>(() => parser.Parse<InvalidListVariant>(yaml));
        Assert.Contains("must be 'object'", exception.Message);
    }

    public class InvalidDictVariant
    {
        [YamlVariant]
        [YamlVariantTypeScalar(typeof(string))]
        public Dictionary<string, string>? Values { get; set; }
    }

    [Fact]
    public void Parse_InvalidDictVariantType_ThrowsException()
    {
        var yaml = "values:\n" +
                   "  test: value";

        var parser = new YamlParser();

        var exception = Assert.Throws<YamlParseException>(() => parser.Parse<InvalidDictVariant>(yaml));
        Assert.Contains("the value type must be 'object'", exception.Message);
    }
}
