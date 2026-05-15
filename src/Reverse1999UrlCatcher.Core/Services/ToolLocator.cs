using Reverse1999UrlCatcher.Core.Domain;

namespace Reverse1999UrlCatcher.Core.Services;

public sealed class ToolLocator
{
    private static readonly string[] MuMuAdbCandidates =
    [
        @"C:\Program Files (x86)\Nemu\vmonitor\bin\adb_server.exe",
        @"C:\Program Files\Netease\MuMuPlayer-12.0\shell\adb.exe",
        @"C:\Program Files\Netease\MuMu Player 12\shell\adb.exe",
        @"C:\Users\admin\AppData\Local\Microsoft\WinGet\Packages\Google.PlatformTools_Microsoft.Winget.Source_8wekyb3d8bbwe\platform-tools\adb.exe",
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "Sdk", "platform-tools", "adb.exe"),
    ];

    private static readonly string[] MitmproxyCandidates =
    [
        @"C:\Program Files\mitmproxy\bin\mitmdump.exe",
        @"C:\Users\admin\AppData\Local\Programs\mitmproxy\bin\mitmdump.exe",
    ];

    public ToolStatus FindAdb(string? explicitPath = null)
    {
        return FindTool("adb", "adb.exe", explicitPath, MuMuAdbCandidates, Path.Combine(AppContext.BaseDirectory, "tools", "adb", "adb.exe"));
    }

    public ToolStatus FindMitmdump(string? explicitPath = null)
    {
        return FindTool("mitmdump", "mitmdump.exe", explicitPath, MitmproxyCandidates, Path.Combine(AppContext.BaseDirectory, "tools", "mitmproxy", "mitmdump.exe"));
    }

    private static ToolStatus FindTool(string name, string executableName, string? explicitPath, IEnumerable<string> knownPaths, string appLocalPath)
    {
        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            candidates.Add(explicitPath);
        }

        candidates.AddRange(knownPaths);
        candidates.Add(appLocalPath);
        candidates.AddRange(PathCandidates(executableName));

        var found = candidates.FirstOrDefault(File.Exists);
        return found is null
            ? new ToolStatus(name, null, false, $"未找到 {executableName}")
            : new ToolStatus(name, found, true);
    }

    private static IEnumerable<string> PathCandidates(string executableName)
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            string candidate;
            try
            {
                candidate = Path.Combine(directory.Trim(), executableName);
            }
            catch
            {
                continue;
            }

            yield return candidate;
        }
    }
}
