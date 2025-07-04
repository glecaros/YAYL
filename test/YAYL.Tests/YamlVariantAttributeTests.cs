using YAYL.Attributes;

namespace YAYL.Tests;

public class YamlVariantAttributeTests
{
    public record Bar(string Baz);
    public record Other(string Field);

    public record FooWithProperties
    {
        [YamlVariant]
        [YamlVariantTypeScalar(typeof(string))]
        [YamlVariantTypeScalar(typeof(int))]
        [YamlVariantTypeObject(typeof(Bar), "baz")]
        [YamlVariantTypeObject(typeof(Other), "field")]
        public object? Value { get; set; }
    }

    public record FooWithConstructor(
        [property: YamlVariant]
        [property: YamlVariantTypeScalar(typeof(string))]
        [property: YamlVariantTypeScalar(typeof(int))]
        [property: YamlVariantTypeObject(typeof(Bar), "baz")]
        [property: YamlVariantTypeObject(typeof(Other), "field")]
        object? Value
    );

    [Fact]
    public void Parse_WithStringVariant_ParsesAsString()
    {
        var yaml = "value: hello world";

        var parser = new YamlParser();
        var result = parser.Parse<FooWithProperties>(yaml);

        Assert.NotNull(result);
        Assert.IsType<string>(result.Value);
        Assert.Equal("hello world", result.Value);
    }

    [Fact]
    public void Parse_WithIntVariant_ParsesAsInt()
    {
        var yaml = "value: 42";

        var parser = new YamlParser();
        var result = parser.Parse<FooWithProperties>(yaml);

        Assert.NotNull(result);
        Assert.IsType<int>(result.Value);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Parse_WithBarObjectVariant_ParsesAsBar()
    {
        var yaml = "value:\n  baz: test value";

        var parser = new YamlParser();
        var result = parser.Parse<FooWithProperties>(yaml);

        Assert.NotNull(result);
        Assert.IsType<Bar>(result.Value);
        var bar = (Bar)result.Value!;
        Assert.Equal("test value", bar.Baz);
    }

    [Fact]
    public void Parse_WithOtherObjectVariant_ParsesAsOther()
    {
        var yaml = "value:\n  field: another value";

        var parser = new YamlParser();
        var result = parser.Parse<FooWithProperties>(yaml);

        Assert.NotNull(result);
        Assert.IsType<Other>(result.Value);
        var other = (Other)result.Value!;
        Assert.Equal("another value", other.Field);
    }

    [Fact]
    public void Parse_WithConstructorParameter_StringVariant_ParsesAsString()
    {
        var yaml = "value: hello constructor";

        var parser = new YamlParser();
        var result = parser.Parse<FooWithConstructor>(yaml);

        Assert.NotNull(result);
        Assert.IsType<string>(result.Value);
        Assert.Equal("hello constructor", result.Value);
    }

    [Fact]
    public void Parse_WithConstructorParameter_ObjectVariant_ParsesAsBar()
    {
        var yaml = "value:\n  baz: constructor bar";

        var parser = new YamlParser();
        var result = parser.Parse<FooWithConstructor>(yaml);

        Assert.NotNull(result);
        Assert.IsType<Bar>(result.Value);
        var bar = (Bar)result.Value!;
        Assert.Equal("constructor bar", bar.Baz);
    }

    public class InvalidVariantProperty
    {
        [YamlVariantTypeScalar(typeof(string))]
        public object? Value { get; set; }
    }

    public class InvalidScalarType
    {
        [YamlVariant]
        [YamlVariantTypeScalar(typeof(int))]
        public object? Value { get; set; }
    }

    [Fact]
    public void Parse_InvalidScalarType_ThrowsException()
    {
        var yaml = "value: test";

        var parser = new YamlParser();

        var exception = Assert.Throws<YamlParseException>(() => parser.Parse<InvalidScalarType>(yaml));
        Assert.Contains("Cannot convert scalar value 'test' to any of the allowed types:", exception.Message);
    }

    [Fact]
    public void Parse_UnmatchedObjectField_ThrowsException()
    {
        var yaml = "value:\n" +
                   "  unknown: value";

        var parser = new YamlParser();

        var exception = Assert.Throws<YamlParseException>(() => parser.Parse<FooWithProperties>(yaml));
        Assert.Contains("Mapping node for property 'Value' does not contain any of the known variant types.", exception.Message);
    }
}
