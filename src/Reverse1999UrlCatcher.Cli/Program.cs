using Reverse1999UrlCatcher.Core.Domain;
using Reverse1999UrlCatcher.Core.Services;
using System.Runtime.Versioning;

namespace Reverse1999UrlCatcher.Cli;

[SupportedOSPlatform("windows")]
internal static class Program
{
    private const int DefaultProxyPort = 8877;
    private const string RemoteCertificatePath = "/sdcard/Download/mitmproxy-ca-cert.cer";

    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            PrintHelp();
            return 0;
        }

        try
        {
            return await RunAsync(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static async Task<int> RunAsync(string[] args)
    {
        var locator = new ToolLocator();
        var certificateService = new CertificateService();
        var store = new ProtectedStateStore();
        var command = args[0];
        var options = ParseOptions(args.Skip(1).ToArray());

        return command switch
        {
            "probe-env" => ProbeEnv(locator),
            "discover-emulator" => await DiscoverEmulatorAsync(locator, options),
            "gen-ca" => await GenerateCertificateAsync(locator, certificateService, options),
            "push-ca" => await PushCertificateAsync(locator, certificateService, options),
            "proxy-on" => await ProxyOnAsync(locator, store, options),
            "proxy-off" => await ProxyOffAsync(locator, store, options),
            "capture" => await CaptureAsync(locator, certificateService, options),
            "recover-proxy" => await RecoverProxyAsync(locator, store),
            _ => Unknown(command),
        };
    }

    private static int ProbeEnv(ToolLocator locator)
    {
        var adb = locator.FindAdb();
        var mitmdump = locator.FindMitmdump();
        var ips = new LocalIpService().GetUsableIpv4Addresses();

        PrintStatus(adb);
        PrintStatus(mitmdump);
        foreach (var ip in ips)
        {
            Console.WriteLine($"ip: {ip.Address} ({ip.Name}){(ip.IsRecommended ? " recommended" : "")}");
        }

        return adb.IsAvailable && mitmdump.IsAvailable && ips.Count > 0 ? 0 : 1;
    }

    private static async Task<int> DiscoverEmulatorAsync(ToolLocator locator, Dictionary<string, string> options)
    {
        var adb = RequireTool(locator.FindAdb(options.GetValueOrDefault("adb")));
        var service = new EmulatorDiscoveryService(new AdbService(adb));

        IReadOnlyList<DeviceTarget> devices;
        if (TryReadInt(options, "port", out var port))
        {
            devices = await service.ConnectManualAsync(port);
        }
        else
        {
            devices = await service.DiscoverWithAutoConnectAsync();
        }

        foreach (var device in devices)
        {
            Console.WriteLine($"{device.Serial}\tmodel={device.Model ?? "-"} brand={device.Brand ?? "-"} android={device.AndroidVersion ?? "-"}");
        }

        if (devices.Count == 0)
        {
            Console.Error.WriteLine("未发现已启动的模拟器设备，请先启动模拟器或手动指定 --port。");
        }

        return devices.Count > 0 ? 0 : 1;
    }

    private static async Task<int> GenerateCertificateAsync(ToolLocator locator, CertificateService certificateService, Dictionary<string, string> options)
    {
        var mitmdump = RequireTool(locator.FindMitmdump(options.GetValueOrDefault("mitmdump")));
        var confDir = options.GetValueOrDefault("confdir") ?? certificateService.GetDefaultConfDir();
        var cert = await certificateService.GenerateCertificateAsync(mitmdump, confDir, ReadPort(options));
        Console.WriteLine(cert);
        return 0;
    }

    private static async Task<int> PushCertificateAsync(ToolLocator locator, CertificateService certificateService, Dictionary<string, string> options)
    {
        var serial = RequireOption(options, "serial");
        var adb = RequireTool(locator.FindAdb(options.GetValueOrDefault("adb")));
        var confDir = options.GetValueOrDefault("confdir") ?? certificateService.GetDefaultConfDir();
        var cert = certificateService.GetCertificatePath(confDir);
        if (!File.Exists(cert))
        {
            throw new FileNotFoundException("证书不存在，请先运行 gen-ca。", cert);
        }

        await new AdbService(adb).PushAsync(serial, cert, RemoteCertificatePath);
        Console.WriteLine($"已推送到 {RemoteCertificatePath}");
        return 0;
    }

    private static async Task<int> ProxyOnAsync(ToolLocator locator, ProtectedStateStore store, Dictionary<string, string> options)
    {
        var serial = RequireOption(options, "serial");
        var host = RequireOption(options, "host");
        var port = ReadPort(options);
        var adb = RequireTool(locator.FindAdb(options.GetValueOrDefault("adb")));
        var proxy = new ProxySettingsService(new AdbService(adb));

        var oldProxy = await proxy.ReadProxyAsync(serial);
        await store.SavePendingProxyRestoreAsync(new ProxyState(serial, oldProxy, host, port, DateTimeOffset.Now));
        await proxy.SetProxyAsync(serial, host, port);
        Console.WriteLine($"已设置代理 {host}:{port}");
        return 0;
    }

    private static async Task<int> ProxyOffAsync(ToolLocator locator, ProtectedStateStore store, Dictionary<string, string> options)
    {
        var serial = RequireOption(options, "serial");
        var adb = RequireTool(locator.FindAdb(options.GetValueOrDefault("adb")));
        var pending = await store.LoadPendingProxyRestoreAsync();
        var oldProxy = pending?.Serial == serial ? pending.OldProxy : null;
        await new ProxySettingsService(new AdbService(adb)).RestoreProxyAsync(serial, oldProxy);
        if (pending?.Serial == serial)
        {
            store.ClearPendingProxyRestore();
        }

        Console.WriteLine("已恢复代理");
        return 0;
    }

    private static async Task<int> CaptureAsync(ToolLocator locator, CertificateService certificateService, Dictionary<string, string> options)
    {
        var mitmdump = RequireTool(locator.FindMitmdump(options.GetValueOrDefault("mitmdump")));
        var host = RequireOption(options, "host");
        var port = ReadPort(options);
        var timeoutSeconds = TryReadInt(options, "timeout", out var timeout) ? timeout : 120;
        var confDir = options.GetValueOrDefault("confdir") ?? certificateService.GetDefaultConfDir();
        var script = options.GetValueOrDefault("script") ?? Path.Combine(AppContext.BaseDirectory, "scripts", "re1999_capture.py");
        var rules = options.GetValueOrDefault("rules") ?? Path.Combine(AppContext.BaseDirectory, "config", "url_rules.json");
        var serial = options.GetValueOrDefault("serial");
        var store = new ProtectedStateStore();
        AdbService? adb = null;
        ProxySettingsService? proxy = null;

        if (!string.IsNullOrWhiteSpace(serial))
        {
            var adbPath = RequireTool(locator.FindAdb(options.GetValueOrDefault("adb")));
            adb = new AdbService(adbPath);
            proxy = new ProxySettingsService(adb);
            var oldProxy = await proxy.ReadProxyAsync(serial);
            await store.SavePendingProxyRestoreAsync(new ProxyState(serial, oldProxy, host, port, DateTimeOffset.Now));
            await proxy.SetProxyAsync(serial, host, port);
            Console.Error.WriteLine($"已设置代理 {host}:{port}");
        }

        try
        {
            var capture = await new MitmproxyService().CaptureOnceAsync(
                mitmdump,
                script,
                rules,
                confDir,
                host,
                port,
                TimeSpan.FromSeconds(timeoutSeconds),
                line => Console.Error.WriteLine(line));

            if (capture is null)
            {
                Console.Error.WriteLine("未捕获到抽卡历史 URL。");
                return 2;
            }

            Console.WriteLine(capture.Url);
            return 0;
        }
        finally
        {
            if (proxy is not null && !string.IsNullOrWhiteSpace(serial))
            {
                var pending = await store.LoadPendingProxyRestoreAsync();
                await proxy.RestoreProxyAsync(serial, pending?.OldProxy);
                store.ClearPendingProxyRestore();
                Console.Error.WriteLine("已恢复代理");
            }
        }
    }

    private static async Task<int> RecoverProxyAsync(ToolLocator locator, ProtectedStateStore store)
    {
        var pending = await store.LoadPendingProxyRestoreAsync();
        if (pending is null)
        {
            Console.WriteLine("没有待恢复代理。");
            return 0;
        }

        var adb = RequireTool(locator.FindAdb());
        await new ProxySettingsService(new AdbService(adb)).RestoreProxyAsync(pending.Serial, pending.OldProxy);
        store.ClearPendingProxyRestore();
        Console.WriteLine($"已恢复 {pending.Serial} 的代理。");
        return 0;
    }

    private static Dictionary<string, string> ParseOptions(string[] args)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var key = args[i][2..];
            var value = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal) ? args[++i] : "true";
            options[key] = value;
        }

        return options;
    }

    private static string RequireTool(ToolStatus status)
    {
        if (!status.IsAvailable || string.IsNullOrWhiteSpace(status.Path))
        {
            throw new InvalidOperationException(status.Message ?? $"未找到 {status.Name}");
        }

        return status.Path;
    }

    private static string RequireOption(Dictionary<string, string> options, string key)
    {
        return options.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException($"缺少参数 --{key}");
    }

    private static int ReadPort(Dictionary<string, string> options)
    {
        return TryReadInt(options, "port", out var port) ? port : DefaultProxyPort;
    }

    private static bool TryReadInt(Dictionary<string, string> options, string key, out int value)
    {
        value = 0;
        return options.TryGetValue(key, out var raw) && int.TryParse(raw, out value);
    }

    private static void PrintStatus(ToolStatus status)
    {
        Console.WriteLine($"{status.Name}: {(status.IsAvailable ? status.Path : status.Message)}");
    }

    private static int Unknown(string command)
    {
        Console.Error.WriteLine($"未知命令：{command}");
        PrintHelp();
        return 1;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
        Reverse1999UrlCatcher CLI

        Commands:
          probe-env
          discover-emulator [--port <adbPort>]
          gen-ca [--port <proxyPort>] [--confdir <path>]
          push-ca --serial <serial> [--confdir <path>]
          proxy-on --serial <serial> --host <ip> [--port <proxyPort>]
          proxy-off --serial <serial>
          capture --host <ip> [--serial <serial>] [--port <proxyPort>] [--timeout <seconds>]
          recover-proxy
        """);
    }
}
