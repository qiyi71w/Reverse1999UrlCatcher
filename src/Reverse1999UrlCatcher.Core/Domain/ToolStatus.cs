namespace Reverse1999UrlCatcher.Core.Domain;

public sealed record ToolStatus(string Name, string? Path, bool IsAvailable, string? Message = null);

public sealed record HostIpAddress(string Address, string Name, bool IsRecommended);
