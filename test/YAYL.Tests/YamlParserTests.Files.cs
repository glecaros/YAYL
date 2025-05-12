using YAYL.Attributes;

namespace YAYL.Tests;

public partial class YamlParserTests: IDisposable
{
    private string _tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public YamlParserTests()
    {
        if (!Directory.Exists(_tempDirectory))
        {
            Directory.CreateDirectory(_tempDirectory);
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    internal record ObjectWithFilePropertyCurrentDirectory(
            [property: YamlPathField(YamlFilePathType.RelativeToCurrentDirectory)]
            string ContentFile
    );

    internal record ObjectWithFilePropertyFile(
            [property: YamlPathField(YamlFilePathType.RelativeToFile)]
            string ContentFile
    );


    [Fact]
    public void Parse_FileProperty_RelativeToWorkingDirectory()
    {
        var yaml = @"content-file: ./content.txt";

        var parser = new YamlParser();

        var result = parser.Parse<ObjectWithFilePropertyCurrentDirectory>(yaml, new(){ WorkingDirectory = _tempDirectory });
        Assert.NotNull(result);
        Assert.Equal(Path.Combine(_tempDirectory, "content.txt"), result.ContentFile);
    }

    [Fact]
    public void Parse_FileProperty_RelativeToFile()
    {
        var yaml = @"content-file: ../content.txt";

        var parser = new YamlParser();

        var result = parser.Parse<ObjectWithFilePropertyFile>(yaml, new(){ FilePath = Path.Combine(_tempDirectory, "subdir", "test.yaml") });
        Assert.NotNull(result);
        Assert.Equal(Path.Combine(_tempDirectory, "content.txt"), result.ContentFile);
    }

    [Fact]
    public void ParseFile_ImplicitFilePath()
    {
        using var yamlFile = new TestFile(
            Path.Combine(_tempDirectory, "subdir", "test.yaml"),
            "content-file: ../content.txt");

        var parser = new YamlParser();
        var result = parser.ParseFile<ObjectWithFilePropertyFile>(yamlFile.FilePath);

        Assert.NotNull(result);
        Assert.Equal(Path.Combine(_tempDirectory, "content.txt"), result.ContentFile);
    }

    [Fact]
    public void ParseFile_ExplicitFilePath()
    {
        using var yamlFile = new TestFile(Path.Combine(
                                    _tempDirectory, "test.yaml"),
                                    "content-file: ../content.txt");

        var parser = new YamlParser();
        var result = parser.ParseFile<ObjectWithFilePropertyFile>(yamlFile.FilePath,
         new(){ FilePath = Path.Combine(_tempDirectory, "subdir", "test.yaml") });

        Assert.NotNull(result);
        Assert.Equal(Path.Combine(_tempDirectory, "content.txt"), result.ContentFile);
    }

    [Fact]
    public void ParseFile_ImplicitWorkingDirectory()
    {
        using TestFile yamlFile = new(
            Path.Combine(_tempDirectory, "test.yaml"),
            "content-file: ./subdir/content.txt");

        var parser = new YamlParser();

        using ScopeGuard<string> _e = new(() => {
            var oldWorkingDirectory = Environment.CurrentDirectory;
            Environment.CurrentDirectory = _tempDirectory;
            return oldWorkingDirectory;
        }, oldWorkingDirectory => {
            Environment.CurrentDirectory = oldWorkingDirectory;
        });

        var result = parser.ParseFile<ObjectWithFilePropertyCurrentDirectory>(yamlFile.FilePath);

        Assert.NotNull(result);

        Assert.Equal(Path.Combine(_tempDirectory, "subdir", "content.txt"), result.ContentFile);
    }

    [Fact]
    public void ParseFile_ExplicitWorkingDirectory()
    {
        using TestFile yamlFile = new(
            Path.Combine(_tempDirectory, "test.yaml"),
            "content-file: ./subdir/content.txt");

        var parser = new YamlParser();

        var result = parser.ParseFile<ObjectWithFilePropertyCurrentDirectory>(yamlFile.FilePath,
            new(){ WorkingDirectory = Path.Combine(_tempDirectory, "other") });

        Assert.NotNull(result);
        Assert.Equal(Path.Combine(_tempDirectory, "other", "subdir", "content.txt"), result.ContentFile);
    }

    [Fact]
    public void Parse_AbsolutePath_CurrentDirectory()
    {
        var yaml = @"content-file: /content.txt";

        var parser = new YamlParser();

        var result = parser.Parse<ObjectWithFilePropertyCurrentDirectory>(yaml, new(){ WorkingDirectory = _tempDirectory });
        Assert.NotNull(result);
        Assert.Equal("/content.txt", result.ContentFile);
    }

    [Fact]
    public void Parse_AbsolutePath_File()
    {
        var yaml = @"content-file: /content.txt";

        var parser = new YamlParser();

        var result = parser.Parse<ObjectWithFilePropertyFile>(yaml, new(){ FilePath = Path.Combine(_tempDirectory, "subdir", "test.yaml") });
        Assert.NotNull(result);
        Assert.Equal("/content.txt", result.ContentFile);
    }

    internal class TestFile: IDisposable
    {
        public readonly string FilePath;
        public readonly string Content;

        public TestFile(string path, string content)
        {
            FilePath = path;
            Content = content;
            var directory = Path.GetDirectoryName(path) ?? throw new InvalidOperationException($"Path {path} is invalid.");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(path, content);
        }

        public void Dispose()
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
        }
    }

    internal class ScopeGuard<T>(Func<T> init , Action<T> disposeAction) : IDisposable
    {
        private readonly Action<T> _disposeAction = disposeAction;
        private T _value = init();

        public void Dispose()
        {
            _disposeAction(_value);
        }
    }

}