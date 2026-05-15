using System.Security.Cryptography;
using System.Text.Json;
using System.Runtime.Versioning;
using Reverse1999UrlCatcher.Core.Domain;

namespace Reverse1999UrlCatcher.Core.Services;

[SupportedOSPlatform("windows")]
public sealed class ProtectedStateStore
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    private readonly string _path;

    public ProtectedStateStore(string? path = null)
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Reverse1999UrlCatcher");
        Directory.CreateDirectory(directory);
        _path = path ?? Path.Combine(directory, "pending-proxy.bin");
    }

    public async Task SavePendingProxyRestoreAsync(ProxyState state, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(state, Options);
        var protectedBytes = ProtectedData.Protect(json, optionalEntropy: null, DataProtectionScope.CurrentUser);
        await File.WriteAllBytesAsync(_path, protectedBytes, cancellationToken);
    }

    public async Task<ProxyState?> LoadPendingProxyRestoreAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_path))
        {
            return null;
        }

        var protectedBytes = await File.ReadAllBytesAsync(_path, cancellationToken);
        var json = ProtectedData.Unprotect(protectedBytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
        return JsonSerializer.Deserialize<ProxyState>(json, Options);
    }

    public void ClearPendingProxyRestore()
    {
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }
    }
}
