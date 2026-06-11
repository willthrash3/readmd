using System.Text.RegularExpressions;

namespace Readmd.Tests;

public sealed partial class IconAssetTests
{
    [Fact]
    public void ApplicationIconIsWiredIntoProjectAndWindow()
    {
        var root = FindRepositoryRoot();
        var projectPath = Path.Combine(root, "src", "Readmd", "Readmd.csproj");
        var windowPath = Path.Combine(root, "src", "Readmd", "MainWindow.xaml");
        var iconPath = Path.Combine(root, "src", "Readmd", "Assets", "Readmd.ico");
        var svgPath = Path.Combine(root, "src", "Readmd", "Assets", "ReadmdIcon.svg");

        Assert.True(File.Exists(iconPath), $"Expected icon at {iconPath}");
        Assert.True(File.Exists(svgPath), $"Expected editable source icon at {svgPath}");
        Assert.Contains("<ApplicationIcon>Assets\\Readmd.ico</ApplicationIcon>", File.ReadAllText(projectPath));
        Assert.Contains("Icon=\"Assets/Readmd.ico\"", File.ReadAllText(windowPath));
    }

    [Fact]
    public void IconContainsExpectedWindowsSizes()
    {
        var root = FindRepositoryRoot();
        var iconPath = Path.Combine(root, "src", "Readmd", "Assets", "Readmd.ico");
        var bytes = File.ReadAllBytes(iconPath);

        Assert.True(bytes.Length > 0);
        Assert.Equal(0, ReadUInt16(bytes, 0));
        Assert.Equal(1, ReadUInt16(bytes, 2));

        var count = ReadUInt16(bytes, 4);
        var sizes = new HashSet<int>();
        for (var i = 0; i < count; i++)
        {
            var entryOffset = 6 + (i * 16);
            var width = bytes[entryOffset] == 0 ? 256 : bytes[entryOffset];
            var height = bytes[entryOffset + 1] == 0 ? 256 : bytes[entryOffset + 1];
            Assert.Equal(width, height);
            sizes.Add(width);
        }

        Assert.True(new[] { 16, 24, 32, 48, 64, 128, 256 }.All(sizes.Contains), $"Icon sizes found: {string.Join(", ", sizes.Order())}");
    }

    [Fact]
    public void SourceSvgUsesReadableMarkdownDocumentMotif()
    {
        var root = FindRepositoryRoot();
        var svgPath = Path.Combine(root, "src", "Readmd", "Assets", "ReadmdIcon.svg");
        var svg = File.ReadAllText(svgPath);

        Assert.Contains("Readmd application icon", svg);
        Assert.Matches(DocumentShapeRegex(), svg);
        Assert.Matches(ArrowShapeRegex(), svg);
    }

    private static ushort ReadUInt16(byte[] bytes, int offset)
    {
        return BitConverter.ToUInt16(bytes, offset);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Readmd.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find Readmd.sln above the test output directory.");
    }

    [GeneratedRegex("M70 42H160L194 76V210H70Z")]
    private static partial Regex DocumentShapeRegex();

    [GeneratedRegex("M132 143L150 162L168 143Z")]
    private static partial Regex ArrowShapeRegex();
}
