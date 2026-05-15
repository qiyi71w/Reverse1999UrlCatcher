using Reverse1999UrlCatcher.Core.Config;

namespace Reverse1999UrlCatcher.Tests;

public sealed class UrlRulesLoaderTests
{
    [Fact]
    public async Task LoadAsync_LoadsRules()
    {
        var path = Path.GetTempFileName();
        await File.WriteAllTextAsync(path, """
        {
          "rules": [
            {
              "name": "global-default",
              "environment": "official-global",
              "hosts": ["game-re-en-service.sl916.com"],
              "pathContains": ["/query/summon"],
              "method": "GET",
              "requireHttps": true,
              "requireStatusCode": 200
            }
          ]
        }
        """);

        try
        {
            var rules = await new UrlRulesLoader().LoadAsync(path);
            Assert.Single(rules);
            Assert.Equal("global-default", rules[0].Name);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
