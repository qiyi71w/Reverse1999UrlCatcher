using System.Text.Json;
using Reverse1999UrlCatcher.Core.Domain;

namespace Reverse1999UrlCatcher.Core.Config;

public sealed class UrlRulesLoader
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public async Task<IReadOnlyList<UrlMatchRule>> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(path);
        var document = await JsonSerializer.DeserializeAsync<UrlRulesDocument>(stream, Options, cancellationToken);
        if (document?.Rules is not { Count: > 0 })
        {
            throw new InvalidOperationException($"No URL rules were found in {path}.");
        }

        return document.Rules;
    }
}
