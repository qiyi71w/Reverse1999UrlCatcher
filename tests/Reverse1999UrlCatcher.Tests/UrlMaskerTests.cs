using Reverse1999UrlCatcher.Core.Privacy;

namespace Reverse1999UrlCatcher.Tests;

public sealed class UrlMaskerTests
{
    [Fact]
    public void MaskUrl_HidesQueryValues()
    {
        var masked = UrlMasker.MaskUrl("https://game-re-en-service.sl916.com/query/summon?token=abc&roleId=123");

        Assert.Equal("https://game-re-en-service.sl916.com/query/summon?token=<hidden>&roleId=<hidden>", masked);
    }

    [Fact]
    public void MaskLogLine_HidesSensitiveInlineValues()
    {
        var masked = UrlMasker.MaskLogLine("failed token=abc123 authkey=secret");

        Assert.DoesNotContain("abc123", masked);
        Assert.DoesNotContain("secret", masked);
    }
}
