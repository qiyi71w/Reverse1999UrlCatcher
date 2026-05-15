using System.Collections.ObjectModel;
using Reverse1999UrlCatcher.Core.Privacy;

namespace Reverse1999UrlCatcher.Core.Services;

public sealed class DiagnosticsService
{
    private readonly ObservableCollection<string> _lines = [];

    public ReadOnlyObservableCollection<string> Lines { get; }

    public DiagnosticsService()
    {
        Lines = new ReadOnlyObservableCollection<string>(_lines);
    }

    public void Info(string message) => Add("INFO", message);

    public void Warn(string message) => Add("WARN", message);

    public void Error(string message) => Add("ERROR", message);

    private void Add(string level, string message)
    {
        _lines.Add($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}][{level}] {UrlMasker.MaskLogLine(message)}");
    }
}
