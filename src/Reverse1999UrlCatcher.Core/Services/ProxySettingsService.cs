using Reverse1999UrlCatcher.Core.Domain;

namespace Reverse1999UrlCatcher.Core.Services;

public sealed class ProxySettingsService(AdbService adbService)
{
    public async Task<string?> ReadProxyAsync(string serial, CancellationToken cancellationToken = default)
    {
        var value = await adbService.ShellAsync(serial, "settings get global http_proxy", cancellationToken);
        return NormalizeProxyValue(value);
    }

    public async Task SetProxyAsync(string serial, string host, int port, CancellationToken cancellationToken = default)
    {
        await adbService.ShellAsync(serial, $"settings put global http_proxy {host}:{port}", cancellationToken);
    }

    public async Task RestoreProxyAsync(string serial, string? oldProxy, CancellationToken cancellationToken = default)
    {
        var action = GetRestoreAction(oldProxy);
        if (action.Command == "delete")
        {
            await ClearProxySettingsAsync(serial, cancellationToken);
            return;
        }

        await adbService.ShellAsync(serial, $"settings put global http_proxy {action.Value}", cancellationToken);
    }

    private async Task ClearProxySettingsAsync(string serial, CancellationToken cancellationToken)
    {
        await TryShellAsync(serial, "settings delete global http_proxy", cancellationToken);
        await TryShellAsync(serial, "settings delete global global_http_proxy_host", cancellationToken);
        await TryShellAsync(serial, "settings delete global global_http_proxy_port", cancellationToken);
        await TryShellAsync(serial, "settings delete global global_http_proxy_exclusion_list", cancellationToken);
        await TryShellAsync(serial, "settings delete global global_proxy_pac_url", cancellationToken);
        await TryShellAsync(serial, "settings put global http_proxy :0", cancellationToken);
    }

    private async Task TryShellAsync(string serial, string command, CancellationToken cancellationToken)
    {
        try
        {
            await adbService.ShellAsync(serial, command, cancellationToken);
        }
        catch
        {
        }
    }

    public static string? NormalizeProxyValue(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) ||
            trimmed.Equals("null", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("none", StringComparison.OrdinalIgnoreCase) ||
            trimmed == ":" ||
            trimmed == ":0")
        {
            return null;
        }

        return trimmed;
    }

    public static ProxyRestoreAction GetRestoreAction(string? oldProxy)
    {
        var normalized = NormalizeProxyValue(oldProxy);
        return normalized is null
            ? new ProxyRestoreAction("delete", null)
            : new ProxyRestoreAction("put", normalized);
    }
}
