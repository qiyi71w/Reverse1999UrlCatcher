using System.Text;
using System.Text.RegularExpressions;

namespace Reverse1999UrlCatcher.Core.Privacy;

public static partial class UrlMasker
{
    public static string MaskUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return MaskSensitiveWords(value);
        }

        if (string.IsNullOrEmpty(uri.Query))
        {
            return uri.GetLeftPart(UriPartial.Path);
        }

        var builder = new StringBuilder();
        builder.Append(uri.GetLeftPart(UriPartial.Path));
        builder.Append('?');

        var query = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < query.Length; i++)
        {
            if (i > 0)
            {
                builder.Append('&');
            }

            var item = query[i];
            var equals = item.IndexOf('=');
            if (equals <= 0)
            {
                builder.Append(Uri.UnescapeDataString(item));
                builder.Append("=<hidden>");
                continue;
            }

            var key = Uri.UnescapeDataString(item[..equals]);
            builder.Append(key);
            builder.Append("=<hidden>");
        }

        return builder.ToString();
    }

    public static string MaskLogLine(string? line)
    {
        if (string.IsNullOrEmpty(line))
        {
            return "";
        }

        var withoutUrls = UrlRegex().Replace(line, match => MaskUrl(match.Value));
        return MaskSensitiveWords(withoutUrls);
    }

    private static string MaskSensitiveWords(string value)
    {
        return SensitiveKeyRegex().Replace(value, match => $"{match.Groups[1].Value}=<hidden>");
    }

    [GeneratedRegex(@"https?://[^\s""']+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UrlRegex();

    [GeneratedRegex(@"\b(token|auth|authkey|cookie|authorization|session|sign|ticket)=([^&\s]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SensitiveKeyRegex();
}
