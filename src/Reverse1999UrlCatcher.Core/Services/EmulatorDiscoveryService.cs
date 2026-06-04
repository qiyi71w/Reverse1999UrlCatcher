using Reverse1999UrlCatcher.Core.Domain;
using System.Net;
using System.Net.NetworkInformation;

namespace Reverse1999UrlCatcher.Core.Services;

public sealed class EmulatorDiscoveryService(AdbService adbService)
{
    public async Task<IReadOnlyList<DeviceTarget>> DiscoverWithAutoConnectAsync(CancellationToken cancellationToken = default)
    {
        var devices = await DiscoverAsync(cancellationToken);
        if (devices.Count > 0)
        {
            return await TryExpandDevicesByLoopbackAsync(devices, cancellationToken);
        }

        devices = await TryHistoricalPortAsync(cancellationToken);
        if (devices.Count > 0)
        {
            return devices;
        }

        return await TryLoopbackCandidatePortsAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DeviceTarget>> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        var devices = await adbService.GetDevicesAsync(cancellationToken);
        var enriched = new List<DeviceTarget>();
        foreach (var device in devices)
        {
            try
            {
                enriched.Add(await adbService.EnrichAsync(device, cancellationToken));
            }
            catch
            {
                enriched.Add(device);
            }
        }

        return enriched;
    }

    public async Task<IReadOnlyList<DeviceTarget>> ConnectManualAsync(int port, CancellationToken cancellationToken = default)
    {
        await adbService.ConnectAsync(port, cancellationToken);
        return await DiscoverAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DeviceTarget>> TryHistoricalPortAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TryConnectWithRetryAsync(7555, cancellationToken);
        }
        catch
        {
            // Historical port probing is best effort; the caller still gets current devices.
        }

        return await DiscoverAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DeviceTarget>> TryLoopbackCandidatePortsAsync(CancellationToken cancellationToken = default)
    {
        var initialDevices = await DiscoverAsync(cancellationToken);
        var knownPorts = initialDevices
            .Where(device => device.Port.HasValue)
            .Select(device => device.Port!.Value)
            .ToHashSet();

        var candidatePorts = GetLoopbackCandidatePorts(knownPorts);
        foreach (var port in candidatePorts)
        {
            try
            {
                using var perPortCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                perPortCts.CancelAfter(TimeSpan.FromSeconds(2));
                await TryConnectWithRetryAsync(port, perPortCts.Token);
            }
            catch
            {
                // Best effort.
            }
        }

        return await DiscoverAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<DeviceTarget>> TryExpandDevicesByLoopbackAsync(
        IReadOnlyList<DeviceTarget> existingDevices,
        CancellationToken cancellationToken)
    {
        var knownPorts = existingDevices
            .Where(device => device.Port.HasValue)
            .Select(device => device.Port!.Value)
            .ToHashSet();

        var candidates = GetLoopbackCandidatePorts(knownPorts)
            .Where(port => port >= 10000)
            .Take(16);

        foreach (var port in candidates)
        {
            try
            {
                using var perPortCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                perPortCts.CancelAfter(TimeSpan.FromSeconds(2));
                await TryConnectWithRetryAsync(port, perPortCts.Token);
            }
            catch
            {
                // Best effort expansion.
            }
        }

        var expanded = await DiscoverAsync(cancellationToken);
        return expanded.Count >= existingDevices.Count ? expanded : existingDevices;
    }

    private async Task TryConnectWithRetryAsync(int port, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                await adbService.ConnectAsync(port, cancellationToken);
                return;
            }
            catch when (attempt < 2)
            {
                await Task.Delay(200, cancellationToken);
            }
        }
    }

    private static IReadOnlyList<int> GetLoopbackCandidatePorts(HashSet<int> knownPorts)
    {
        var loopbackPorts = IPGlobalProperties
            .GetIPGlobalProperties()
            .GetActiveTcpListeners()
            .Where(endpoint => IPAddress.IsLoopback(endpoint.Address))
            .Select(endpoint => endpoint.Port)
            .Where(IsLikelyEmulatorPort)
            .Distinct()
            .Where(port => !knownPorts.Contains(port))
            .ToList();

        var legacyPorts = Enumerable.Range(5555, 31)
            .Where(port => port % 2 == 1)
            .Where(port => !knownPorts.Contains(port))
            .ToList();

        return loopbackPorts
            .OrderByDescending(port => port is >= 15000 and <= 26000)
            .ThenBy(port => Math.Abs(port - 16416))
            .Concat(legacyPorts)
            .Distinct()
            .Take(12)
            .ToList();
    }

    private static bool IsLikelyEmulatorPort(int port)
    {
        if (port is 5555 or 7555 or 16416)
        {
            return true;
        }

        return port is >= 7000 and <= 30000;
    }
}
