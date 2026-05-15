using Reverse1999UrlCatcher.Core.Parsing;

namespace Reverse1999UrlCatcher.Tests;

public sealed class AdbDevicesParserTests
{
    [Fact]
    public void Parse_ReturnsOnlyOnlineDevices()
    {
        const string output = """
        List of devices attached
        127.0.0.1:16384 device product:mumu model:MuMu12 device:android transport_id:1
        127.0.0.1:7555 offline
        """;

        var devices = AdbDevicesParser.Parse(output);

        Assert.Single(devices);
        Assert.Equal("127.0.0.1:16384", devices[0].Serial);
        Assert.Equal(16384, devices[0].Port);
        Assert.Equal("MuMu12", devices[0].Model);
    }
}
