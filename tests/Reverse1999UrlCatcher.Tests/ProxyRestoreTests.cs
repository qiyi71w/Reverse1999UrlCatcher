using Reverse1999UrlCatcher.Core.Services;

namespace Reverse1999UrlCatcher.Tests;

public sealed class ProxyRestoreTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("null")]
    [InlineData(":")]
    [InlineData(":0")]
    public void GetRestoreAction_DeletesEmptyProxy(string? oldProxy)
    {
        var action = ProxySettingsService.GetRestoreAction(oldProxy);

        Assert.Equal("delete", action.Command);
        Assert.Null(action.Value);
    }

    [Fact]
    public void GetRestoreAction_RestoresExistingProxy()
    {
        var action = ProxySettingsService.GetRestoreAction("192.168.1.2:8888");

        Assert.Equal("put", action.Command);
        Assert.Equal("192.168.1.2:8888", action.Value);
    }
}
