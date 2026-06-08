using System.Xml.Linq;

namespace Reverse1999UrlCatcher.Tests;

public sealed class AppVersionDisplayTests
{
    [Fact]
    public void InformationalVersion_DoesNotAppendSourceRevision()
    {
        var propsPath = Path.Combine(FindRepositoryRoot(), "Directory.Build.props");
        var props = XDocument.Load(propsPath);

        var includeSourceRevision = props
            .Descendants("IncludeSourceRevisionInInformationalVersion")
            .SingleOrDefault()
            ?.Value;

        Assert.Equal("false", includeSourceRevision);
    }

    [Fact]
    public void MainViewModel_AppVersionUsesProductVersionWithoutPresentationPrefix()
    {
        var sourcePath = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "Reverse1999UrlCatcher.App",
            "ViewModels",
            "MainViewModel.cs");

        var source = File.ReadAllText(sourcePath);

        Assert.Contains("AssemblyInformationalVersionAttribute", source);
        Assert.DoesNotContain("$\"v{", source);
    }

    [Fact]
    public void MainWindow_DisplaysVersionPrefixInXaml()
    {
        var sourcePath = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "Reverse1999UrlCatcher.App",
            "MainWindow.xaml");

        var source = File.ReadAllText(sourcePath);

        Assert.Contains("StringFormat=v{0}", source);
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
