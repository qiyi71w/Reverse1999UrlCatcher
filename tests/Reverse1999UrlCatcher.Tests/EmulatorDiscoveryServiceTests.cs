using System.Net;
using Reverse1999UrlCatcher.Core.Services;

namespace Reverse1999UrlCatcher.Tests;

public sealed class EmulatorDiscoveryServiceTests
{
    [Fact]
    public async Task DiscoverWithAutoConnectAsync_TriesLoopbackAliasWhenWildcardPortIsShadowed()
    {
        var runner = new FakeProcessRunner();
        var adb = new AdbService("adb.exe", runner);
        var service = new EmulatorDiscoveryService(
            adb,
            () =>
            [
                new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5555),
                new IPEndPoint(IPAddress.Any, 5555),
                new IPEndPoint(IPAddress.Parse("127.0.0.1"), 16384),
            ]);

        var devices = await service.DiscoverWithAutoConnectAsync();

        Assert.Contains(runner.ConnectTargets, target => target == "127.0.0.2:5555");
        Assert.Contains(devices, device => device.Serial == "127.0.0.2:5555");
    }

    private sealed class FakeProcessRunner : IProcessRunner
    {
        private bool _connectedAlias;

        public List<string> ConnectTargets { get; } = [];

        public Task<ProcessResult> RunAsync(
            string fileName,
            IEnumerable<string> arguments,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            var args = arguments.ToArray();
            if (args is ["devices", "-l"])
            {
                return Task.FromResult(new ProcessResult(0, DevicesOutput(), ""));
            }

            if (args is ["connect", var target])
            {
                ConnectTargets.Add(target);
                if (target == "127.0.0.2:5555")
                {
                    _connectedAlias = true;
                }

                return Task.FromResult(new ProcessResult(0, $"connected to {target}", ""));
            }

            if (args is ["-s", var serial, "shell", var command])
            {
                return Task.FromResult(new ProcessResult(0, ShellOutput(serial, command), ""));
            }

            return Task.FromResult(new ProcessResult(1, "", "unexpected adb command"));
        }

        private string DevicesOutput()
        {
            var output = """
            List of devices attached
            127.0.0.1:16384 device product:wukong model:PGEM10 device:wukong transport_id:33
            emulator-5554 device product:wukong model:PGEM10 device:wukong transport_id:43
            """;

            if (_connectedAlias)
            {
                output += "\n127.0.0.2:5555 device product:ASUS_AI2501_A model:ASUS_AI2501_A device:graceltexx transport_id:112";
            }

            return output;
        }

        private static string ShellOutput(string serial, string command)
        {
            var isLdPlayer = serial == "127.0.0.2:5555";
            return command switch
            {
                "getprop ro.product.model" => isLdPlayer ? "ASUS_AI2501_A" : "PGEM10",
                "getprop ro.product.brand" => isLdPlayer ? "ROG" : "OPPO",
                "getprop ro.build.version.release" => isLdPlayer ? "14" : "12",
                _ => "",
            };
        }
    }
}
