namespace Reverse1999UrlCatcher.Core.Domain;

public sealed record ProxyState(
    string Serial,
    string? OldProxy,
    string Host,
    int Port,
    DateTimeOffset CreatedAt);

public sealed record ProxyRestoreAction(string Command, string? Value);
