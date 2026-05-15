namespace Reverse1999UrlCatcher.Core.Domain;

public sealed record DeviceTarget(
    string Serial,
    int? Port = null,
    string? Model = null,
    string? Brand = null,
    string? AndroidVersion = null);
