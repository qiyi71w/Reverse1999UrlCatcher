using System.Diagnostics;

namespace Reverse1999UrlCatcher.Core.Services;

public sealed class CertificateService
{
    public string GetDefaultConfDir()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Reverse1999UrlCatcher", "mitmproxy");
    }

    public string GetCertificatePath(string confDir)
    {
        return Path.Combine(confDir, "mitmproxy-ca-cert.cer");
    }

    public async Task<string> GenerateCertificateAsync(string mitmdumpPath, string confDir, int listenPort = 8877, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(confDir);

        var startInfo = new ProcessStartInfo(mitmdumpPath)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("--set");
        startInfo.ArgumentList.Add($"confdir={confDir}");
        startInfo.ArgumentList.Add("--set");
        startInfo.ArgumentList.Add($"listen_port={listenPort}");
        startInfo.ArgumentList.Add("--set");
        startInfo.ArgumentList.Add("flow_detail=0");

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start mitmdump.");
        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        ProcessRunner.TryKill(process);

        var certificatePath = GetCertificatePath(confDir);
        if (!File.Exists(certificatePath))
        {
            throw new FileNotFoundException("mitmproxy CA certificate was not generated.", certificatePath);
        }

        return certificatePath;
    }
}
