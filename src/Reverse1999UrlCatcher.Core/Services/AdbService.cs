using Reverse1999UrlCatcher.Core.Domain;
using Reverse1999UrlCatcher.Core.Parsing;

namespace Reverse1999UrlCatcher.Core.Services;

public sealed class AdbService(string adbPath, IProcessRunner? runner = null)
{
    private readonly IProcessRunner _runner = runner ?? new ProcessRunner();

    public async Task<IReadOnlyList<DeviceTarget>> GetDevicesAsync(CancellationToken cancellationToken = default)
    {
        var result = await _runner.RunAsync(adbPath, ["devices", "-l"], TimeSpan.FromSeconds(10), cancellationToken);
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.StandardError);
        }

        return AdbDevicesParser.Parse(result.StandardOutput);
    }

    public async Task ConnectAsync(int port, CancellationToken cancellationToken = default)
    {
        await ConnectAsync("127.0.0.1", port, cancellationToken);
    }

    public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        var result = await _runner.RunAsync(adbPath, ["connect", $"{host}:{port}"], TimeSpan.FromSeconds(10), cancellationToken);
        if (!result.IsSuccess || result.StandardOutput.Contains("failed", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(result.StandardError.Length > 0 ? result.StandardError : result.StandardOutput);
        }
    }

    public async Task<DeviceTarget> EnrichAsync(DeviceTarget target, CancellationToken cancellationToken = default)
    {
        var model = await ShellAsync(target.Serial, "getprop ro.product.model", cancellationToken);
        var brand = await ShellAsync(target.Serial, "getprop ro.product.brand", cancellationToken);
        var version = await ShellAsync(target.Serial, "getprop ro.build.version.release", cancellationToken);
        return target with
        {
            Model = string.IsNullOrWhiteSpace(model) ? target.Model : model.Trim(),
            Brand = string.IsNullOrWhiteSpace(brand) ? target.Brand : brand.Trim(),
            AndroidVersion = string.IsNullOrWhiteSpace(version) ? target.AndroidVersion : version.Trim(),
        };
    }

    public async Task PushAsync(string serial, string localPath, string remotePath, CancellationToken cancellationToken = default)
    {
        var result = await _runner.RunAsync(adbPath, ["-s", serial, "push", localPath, remotePath], TimeSpan.FromSeconds(30), cancellationToken);
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.StandardError);
        }
    }

    public async Task<string> ShellAsync(string serial, string command, CancellationToken cancellationToken = default)
    {
        var result = await _runner.RunAsync(adbPath, ["-s", serial, "shell", command], TimeSpan.FromSeconds(10), cancellationToken);
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.StandardError);
        }

        return result.StandardOutput.Trim();
    }
}
