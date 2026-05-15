namespace Reverse1999UrlCatcher.Core.Domain;

public sealed record CaptureResult(
    string Url,
    string MaskedUrl,
    string Host,
    string Path,
    string MatchedRule,
    DateTimeOffset CapturedAt);
