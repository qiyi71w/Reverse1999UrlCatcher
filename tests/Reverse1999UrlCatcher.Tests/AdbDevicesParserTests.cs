using Reverse1999UrlCatcher.Core.Parsing;

namespace Reverse1999UrlCatcher.Tests;

public sealed class AdbDevicesParserTests
{
    [Fact]
    public void Parse_ReturnsOnlyOnlineEmulatorDevices()
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
        Assert.NotNull(devices[0].Model);
    }

    [Fact]
    public void Parse_ReturnsLdPlayerDevice()
    {
        const string output = """
        List of devices attached
        127.0.0.1:5555 device product:leidian model:LDPlayer14 device:android transport_id:2
        """;

        var devices = AdbDevicesParser.Parse(output);

        Assert.Single(devices);
        Assert.Equal("127.0.0.1:5555", devices[0].Serial);
        Assert.Equal(5555, devices[0].Port);
        Assert.Equal("LDPlayer14", devices[0].Model);
    }
}
