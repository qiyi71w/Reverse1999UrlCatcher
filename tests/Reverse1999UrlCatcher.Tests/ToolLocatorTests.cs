namespace Reverse1999UrlCatcher.Tests;

public sealed class ToolLocatorTests
{
    [Fact]
    public void ToolLocatorSource_DoesNotContainUserSpecificPath()
    {
        var sourcePath = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "Reverse1999UrlCatcher.Core",
            "Services",
            "ToolLocator.cs");

        var source = File.ReadAllText(sourcePath).Replace('/', '\\');

        var userSpecificPath = string.Join("\\", "C:", "Users", "admin");
        Assert.DoesNotContain(userSpecificPath, source, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Reverse1999UrlCatcher.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate Reverse1999UrlCatcher.sln.");
    }
}
