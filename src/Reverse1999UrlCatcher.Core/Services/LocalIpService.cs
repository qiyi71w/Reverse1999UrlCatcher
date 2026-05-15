using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Reverse1999UrlCatcher.Core.Domain;

namespace Reverse1999UrlCatcher.Core.Services;

public sealed class LocalIpService
{
    public IReadOnlyList<HostIpAddress> GetUsableIpv4Addresses()
    {
        var addresses = new List<HostIpAddress>();
        foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (networkInterface.OperationalStatus != OperationalStatus.Up ||
                networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
            {
                continue;
            }

            foreach (var unicast in networkInterface.GetIPProperties().UnicastAddresses)
            {
                if (unicast.Address.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(unicast.Address))
                {
                    continue;
                }

                var value = unicast.Address.ToString();
                addresses.Add(new HostIpAddress(value, networkInterface.Name, IsPrivate(value)));
            }
        }

        return addresses
            .OrderByDescending(address => address.IsRecommended)
            .ThenBy(address => address.Address, StringComparer.Ordinal)
            .ToList();
    }

    private static bool IsPrivate(string address)
    {
        var parts = address.Split('.');
        if (parts.Length != 4 || !byte.TryParse(parts[0], out var first) || !byte.TryParse(parts[1], out var second))
        {
            return false;
        }

        return first == 10 ||
               first == 192 && second == 168 ||
               first == 172 && second is >= 16 and <= 31;
    }
}
