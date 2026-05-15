using System.Diagnostics;
using System.Net.Sockets;
using Reverse1999UrlCatcher.Core.Domain;
using Reverse1999UrlCatcher.Core.Parsing;
using Reverse1999UrlCatcher.Core.Privacy;

namespace Reverse1999UrlCatcher.Core.Services;

public sealed class MitmproxyService
{
    public async Task<CaptureResult?> CaptureOnceAsync(
        string mitmdumpPath,
        string scriptPath,
        string rulesPath,
        string confDir,
        string listenHost,
        int listenPort,
        TimeSpan timeout,
        Action<string>? log = null,
        CancellationToken cancellationToken = default)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var startInfo = new ProcessStartInfo(mitmdumpPath)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        startInfo.ArgumentList.Add("-s");
        startInfo.ArgumentList.Add(scriptPath);
        startInfo.ArgumentList.Add("--set");
        startInfo.ArgumentList.Add($"confdir={confDir}");
        startInfo.ArgumentList.Add("--set");
        startInfo.ArgumentList.Add("mode=regular");
        startInfo.ArgumentList.Add("--set");
        startInfo.ArgumentList.Add($"listen_host={listenHost}");
        startInfo.ArgumentList.Add("--set");
        startInfo.ArgumentList.Add($"listen_port={listenPort}");
        startInfo.ArgumentList.Add("--set");
        startInfo.ArgumentList.Add("flow_detail=0");
        startInfo.ArgumentList.Add("--set");
        startInfo.ArgumentList.Add($"re1999_rules={rulesPath}");

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start mitmdump.");
        var captureCompletion = new TaskCompletionSource<CaptureResult?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = Task.Run(async () =>
        {
            if (await WaitForListenerReadyAsync(listenHost, listenPort, TimeSpan.FromSeconds(5), timeoutCts.Token))
            {
                log?.Invoke("MITM_READY");
            }
        }, timeoutCts.Token);

        _ = Task.Run(async () =>
        {
            while (true)
            {
                var line = await process.StandardOutput.ReadLineAsync(timeoutCts.Token);
                if (line is null)
                {
                    return;
                }

                if (CaptureJsonParser.TryParse(line, out var capture))
                {
                    captureCompletion.TrySetResult(capture);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(line))
                {
                    log?.Invoke(UrlMasker.MaskLogLine(line));
                }
            }
        }, timeoutCts.Token);

        _ = Task.Run(async () =>
        {
            while (true)
            {
                var line = await process.StandardError.ReadLineAsync(timeoutCts.Token);
                if (line is null)
                {
                    return;
                }

                if (!string.IsNullOrWhiteSpace(line))
                {
                    log?.Invoke(UrlMasker.MaskLogLine(line));
                }
            }
        }, timeoutCts.Token);

        try
        {
            var timeoutTask = Task.Delay(timeout, timeoutCts.Token);
            var processExitTask = process.WaitForExitAsync(CancellationToken.None);
            var completed = await Task.WhenAny(captureCompletion.Task, processExitTask, timeoutTask);
            if (completed == captureCompletion.Task)
            {
                return await captureCompletion.Task;
            }

            if (completed == processExitTask)
            {
                var exitCode = process.HasExited ? process.ExitCode : -1;
                log?.Invoke($"mitmdump exited with code {exitCode}.");
                return null;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            log?.Invoke("等待捕获超时，可能尚未安装 CA 或当前游戏版本不信任用户 CA。");
            return null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return null;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            log?.Invoke("等待捕获超时，可能尚未安装 CA 或当前游戏版本不信任用户 CA。");
            return null;
        }
        finally
        {
            ProcessRunner.TryKill(process);
        }
    }

    private static async Task<bool> WaitForListenerReadyAsync(string host, int port, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        while (!timeoutCts.IsCancellationRequested)
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(host, port, timeoutCts.Token);
                return true;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                await Task.Delay(120, timeoutCts.Token);
            }
        }

        return false;
    }
}
