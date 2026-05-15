using System.Text.Json;
using System.Text.Json.Serialization;
using Reverse1999UrlCatcher.Core.Domain;
using Reverse1999UrlCatcher.Core.Privacy;

namespace Reverse1999UrlCatcher.Core.Parsing;

public static class CaptureJsonParser
{
    public const string Prefix = "CAPTURE_JSON:";

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static bool TryParse(string? line, out CaptureResult result)
    {
        result = default!;
        if (string.IsNullOrWhiteSpace(line) || !line.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<CapturePayload>(line[Prefix.Length..], Options);
            if (payload is null || string.IsNullOrWhiteSpace(payload.Url))
            {
                return false;
            }

            result = new CaptureResult(
                payload.Url,
                UrlMasker.MaskUrl(payload.Url),
                payload.Host ?? "",
                payload.Path ?? "",
                payload.MatchedRule ?? "",
                payload.Timestamp ?? DateTimeOffset.Now);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private sealed record CapturePayload
    {
        [JsonPropertyName("url")]
        public string? Url { get; init; }

        [JsonPropertyName("host")]
        public string? Host { get; init; }

        [JsonPropertyName("path")]
        public string? Path { get; init; }

        [JsonPropertyName("matchedRule")]
        public string? MatchedRule { get; init; }

        [JsonPropertyName("ts")]
        public DateTimeOffset? Timestamp { get; init; }
    }
}
