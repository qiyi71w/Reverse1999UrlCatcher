using System.Text.RegularExpressions;
using Reverse1999UrlCatcher.Core.Domain;

namespace Reverse1999UrlCatcher.Core.Parsing;

public static partial class AdbDevicesParser
{
    public static IReadOnlyList<DeviceTarget> Parse(string output)
    {
        var devices = new List<DeviceTarget>();
        foreach (var rawLine in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("List of devices", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var parts = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 || !string.Equals(parts[1], "device", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            devices.Add(new DeviceTarget(parts[0], TryReadPort(parts[0]), TryReadValue(line, "model")));
        }

        return devices;
    }

    private static int? TryReadPort(string serial)
    {
        var index = serial.LastIndexOf(':');
        if (index < 0 || index == serial.Length - 1)
        {
            return null;
        }

        return int.TryParse(serial[(index + 1)..], out var port) ? port : null;
    }

    private static string? TryReadValue(string line, string key)
    {
        var match = KeyValueRegex().Match(line);
        while (match.Success)
        {
            if (string.Equals(match.Groups["key"].Value, key, StringComparison.OrdinalIgnoreCase))
            {
                return match.Groups["value"].Value;
            }

            match = match.NextMatch();
        }

        return null;
    }

    [GeneratedRegex(@"(?<key>[A-Za-z_]+):(?<value>[^\s]+)", RegexOptions.CultureInvariant)]
    private static partial Regex KeyValueRegex();
}
