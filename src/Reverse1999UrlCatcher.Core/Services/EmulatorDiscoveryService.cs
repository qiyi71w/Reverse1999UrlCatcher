using System.Net;
using System.Net.NetworkInformation;
using Reverse1999UrlCatcher.Core.Domain;

namespace Reverse1999UrlCatcher.Core.Services;

public sealed class EmulatorDiscoveryService
{
    private static readonly IPAddress LoopbackAlias = IPAddress.Parse("127.0.0.2");
    private readonly AdbService _adbService;
    private readonly Func<IReadOnlyList<IPEndPoint>> _activeTcpListeners;

    public EmulatorDiscoveryService(
        AdbService adbService,
        Func<IReadOnlyList<IPEndPoint>>? activeTcpListeners = null)
    {
        _adbService = adbService;
        _activeTcpListeners = activeTcpListeners ?? GetActiveTcpListeners;
    }

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
        var devices = await _adbService.GetDevicesAsync(cancellationToken);
        var enriched = new List<DeviceTarget>();
        foreach (var device in devices)
        {
            try
            {
                enriched.Add(await _adbService.EnrichAsync(device, cancellationToken));
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
        await _adbService.ConnectAsync(port, cancellationToken);
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
        var knownPorts = GetKnownPorts(initialDevices);
        var knownSerials = GetKnownSerials(initialDevices);

        var candidates = GetLoopbackCandidateTargets(knownPorts, knownSerials);
        foreach (var candidate in candidates)
        {
            try
            {
                using var perPortCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                perPortCts.CancelAfter(TimeSpan.FromSeconds(2));
                await TryConnectWithRetryAsync(candidate, perPortCts.Token);
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
        var knownPorts = GetKnownPorts(existingDevices);
        var knownSerials = GetKnownSerials(existingDevices);

        var candidates = GetLoopbackCandidateTargets(knownPorts, knownSerials)
            .Where(target => target.Port >= 10000 || target.IsLoopbackAlias)
            .Take(16);

        foreach (var candidate in candidates)
        {
            try
            {
                using var perPortCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                perPortCts.CancelAfter(TimeSpan.FromSeconds(2));
                await TryConnectWithRetryAsync(candidate, perPortCts.Token);
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
        await TryConnectWithRetryAsync(new AdbConnectTarget("127.0.0.1", port), cancellationToken);
    }

    private async Task TryConnectWithRetryAsync(AdbConnectTarget target, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                await _adbService.ConnectAsync(target.Host, target.Port, cancellationToken);
                return;
            }
            catch when (attempt < 2)
            {
                await Task.Delay(200, cancellationToken);
            }
        }
    }

    private IReadOnlyList<AdbConnectTarget> GetLoopbackCandidateTargets(
        HashSet<int> knownPorts,
        HashSet<string> knownSerials)
    {
        var listeners = _activeTcpListeners();
        var listenerPorts = listeners
            .Where(endpoint => IsLocalCandidateAddress(endpoint.Address))
            .Select(endpoint => endpoint.Port)
            .Where(IsLikelyEmulatorPort)
            .Distinct()
            .Where(port => !knownPorts.Contains(port))
            .ToList();

        var legacyPorts = Enumerable.Range(5555, 31)
            .Where(port => port % 2 == 1)
            .Where(port => !knownPorts.Contains(port))
            .ToList();

        var targets = listenerPorts
            .OrderByDescending(port => port is >= 15000 and <= 26000)
            .ThenBy(port => Math.Abs(port - 16416))
            .Concat(legacyPorts)
            .Distinct()
            .Take(12)
            .Select(port => new AdbConnectTarget("127.0.0.1", port))
            .ToList();

        targets.AddRange(GetShadowedWildcardPortTargets(listeners, knownSerials));
        return targets.Distinct().ToList();
    }

    private static IReadOnlyList<AdbConnectTarget> GetShadowedWildcardPortTargets(
        IReadOnlyList<IPEndPoint> listeners,
        HashSet<string> knownSerials)
    {
        return listeners
            .Where(endpoint => endpoint.Address.Equals(IPAddress.Any))
            .Select(endpoint => endpoint.Port)
            .Where(port => listeners.Any(endpoint => endpoint.Port == port && endpoint.Address.Equals(IPAddress.Loopback)))
            .Where(port => !listeners.Any(endpoint => endpoint.Port == port && endpoint.Address.Equals(LoopbackAlias)))
            .Where(IsLikelyEmulatorPort)
            .Distinct()
            .Where(port => !knownSerials.Contains($"{LoopbackAlias}:{port}"))
            .Select(port => new AdbConnectTarget(LoopbackAlias.ToString(), port, IsLoopbackAlias: true))
            .ToList();
    }

    private static HashSet<int> GetKnownPorts(IReadOnlyList<DeviceTarget> devices)
    {
        return devices
            .Where(device => device.Port.HasValue)
            .Select(device => device.Port!.Value)
            .ToHashSet();
    }

    private static HashSet<string> GetKnownSerials(IReadOnlyList<DeviceTarget> devices)
    {
        return devices
            .Select(device => device.Serial)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<IPEndPoint> GetActiveTcpListeners()
    {
        return IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
    }

    private static bool IsLocalCandidateAddress(IPAddress address)
    {
        return IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any);
    }

    private static bool IsLikelyEmulatorPort(int port)
    {
        if (port is 5555 or 7555 or 16416)
        {
            return true;
        }

        return port is >= 7000 and <= 30000;
    }

    private readonly record struct AdbConnectTarget(string Host, int Port, bool IsLoopbackAlias = false);
}
