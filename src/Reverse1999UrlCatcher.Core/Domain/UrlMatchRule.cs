using System.Text.Json.Serialization;

namespace Reverse1999UrlCatcher.Core.Domain;

public sealed record UrlRulesDocument
{
    [JsonPropertyName("rules")]
    public List<UrlMatchRule> Rules { get; init; } = [];
}

public sealed record UrlMatchRule
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("environment")]
    public string Environment { get; init; } = "";

    [JsonPropertyName("hosts")]
    public List<string> Hosts { get; init; } = [];

    [JsonPropertyName("pathContains")]
    public List<string> PathContains { get; init; } = [];

    [JsonPropertyName("method")]
    public string Method { get; init; } = "GET";

    [JsonPropertyName("requireHttps")]
    public bool RequireHttps { get; init; } = true;

    [JsonPropertyName("requireStatusCode")]
    public int? RequireStatusCode { get; init; } = 200;

    [JsonPropertyName("queryKeys")]
    public List<string> QueryKeys { get; init; } = [];
}
