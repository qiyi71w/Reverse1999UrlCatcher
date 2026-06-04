namespace Reverse1999UrlCatcher.Tests;

public sealed class ToolLocatorTests
{
    [Fact]
    public void ToolLocatorSource_DoesNotContainUserSpecificPath()
    {
        var sourcePath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "Reverse1999UrlCatcher.Core",
            "Services",
            "ToolLocator.cs"));

        var source = File.ReadAllText(sourcePath);

        var userSpecificPath = string.Join(Path.DirectorySeparatorChar, "C:", "Users", "admin");
        Assert.DoesNotContain(userSpecificPath, source, StringComparison.OrdinalIgnoreCase);
    }
}
