using Reverse1999UrlCatcher.Core.Parsing;

namespace Reverse1999UrlCatcher.Tests;

public sealed class CaptureJsonParserTests
{
    [Fact]
    public void TryParse_ParsesCaptureLine()
    {
        var ok = CaptureJsonParser.TryParse(
            """CAPTURE_JSON:{"url":"https://game-re-en-service.sl916.com/query/summon?token=abc","host":"game-re-en-service.sl916.com","path":"/query/summon","matchedRule":"global-default"}""",
            out var result);

        Assert.True(ok);
        Assert.Equal("global-default", result.MatchedRule);
        Assert.DoesNotContain("abc", result.MaskedUrl);
    }

    [Fact]
    public void TryParse_IgnoresNonCaptureLine()
    {
        var ok = CaptureJsonParser.TryParse("normal mitmproxy output", out _);

        Assert.False(ok);
    }
}
