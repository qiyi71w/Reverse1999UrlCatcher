using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Reverse1999UrlCatcher.App.Commands;
using Reverse1999UrlCatcher.App.Services;
using Reverse1999UrlCatcher.Core.Config;
using Reverse1999UrlCatcher.Core.Domain;
using Reverse1999UrlCatcher.Core.Privacy;
using Reverse1999UrlCatcher.Core.Services;

namespace Reverse1999UrlCatcher.App.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private const string RemoteCertificatePath = "/sdcard/Download/mitmproxy-ca-cert.cer";
    private readonly ToolLocator _toolLocator = new();
    private readonly CertificateService _certificateService = new();
    private readonly ProtectedStateStore _stateStore = new();
    private readonly AppSettingsStore _settingsStore = new();
    private readonly UrlRulesLoader _rulesLoader = new();
    private readonly ClipboardService _clipboardService = new();
    private CancellationTokenSource? _captureCts;
    private string? _capturedUrl;
    private string _status = "等待环境检测";
    private string _adbPath = "";
    private string _mitmdumpPath = "";
    private string _manualPort = "";
    private int _proxyPort = 8877;
    private HostIpAddress? _selectedHostIp;
    private DeviceTarget? _selectedDevice;
    private string _capturePreview = "";
    private bool _isBusy;
    private bool _isCapturing;
    private bool _hasPendingRestore;
    private string _proxyApplyStatus = "未开始";
    private string _trafficStatus = "未开始";
    private bool _isDarkTheme = true;
    private bool _showStatusBadge;
    private CancellationTokenSource? _statusBadgeCts;
    private bool _suspendSettingsSave;
    private AppSettings _loadedSettings = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<DeviceTarget> Devices { get; } = [];
    public ObservableCollection<HostIpAddress> HostIps { get; } = [];
    public ObservableCollection<string> Logs { get; } = [];

    public MainViewModel()
    {
        ProbeEnvironmentCommand = new AsyncRelayCommand(ProbeEnvironmentByUserAsync);
        DiscoverDevicesCommand = new AsyncRelayCommand(DiscoverDevicesAsync, HasAdb);
        ConnectManualCommand = new AsyncRelayCommand(ConnectManualAsync, HasAdb);
        GenerateCertificateCommand = new AsyncRelayCommand(GenerateCertificateAsync, HasMitmdump);
        PushCertificateCommand = new AsyncRelayCommand(PushCertificateAsync, () => HasAdb() && SelectedDevice is not null);
        SemiAutoInstallCaCommand = new AsyncRelayCommand(SemiAutoInstallCaAsync, () => HasAdb() && HasMitmdump() && SelectedDevice is not null);
        OpenCertificateDirectoryCommand = new AsyncRelayCommand(OpenCertificateDirectoryAsync);
        ConfirmCertificateInstalledCommand = new AsyncRelayCommand(ConfirmCertificateInstalledAsync);
        ReloadRulesCommand = new AsyncRelayCommand(ReloadRulesAsync);
        StartCaptureCommand = new AsyncRelayCommand(StartCaptureAsync, CanStartCapture);
        StopCaptureCommand = new AsyncRelayCommand(StopCaptureAsync, () => IsCapturing && SelectedDevice is not null);
        CopyUrlCommand = new AsyncRelayCommand(CopyUrlAsync, () => !string.IsNullOrEmpty(_capturedUrl));
        RecoverProxyCommand = new AsyncRelayCommand(RecoverProxyAsync, () => HasAdb() && SelectedDevice is not null);

        ApplySavedSettings();
        _ = ProbeEnvironmentAsync(showBadge: false);
    }

    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    public string AdbPath
    {
        get => _adbPath;
        set
        {
            if (SetField(ref _adbPath, value))
            {
                RaiseCommandStates();
                SaveSettings();
            }
        }
    }

    public string MitmdumpPath
    {
        get => _mitmdumpPath;
        set
        {
            if (SetField(ref _mitmdumpPath, value))
            {
                RaiseCommandStates();
                SaveSettings();
            }
        }
    }

    public string ManualPort
    {
        get => _manualPort;
        set => SetField(ref _manualPort, value);
    }

    public int ProxyPort
    {
        get => _proxyPort;
        set
        {
            if (SetField(ref _proxyPort, value))
            {
                SaveSettings();
            }
        }
    }

    public HostIpAddress? SelectedHostIp
    {
        get => _selectedHostIp;
        set
        {
            if (SetField(ref _selectedHostIp, value))
            {
                RaiseCommandStates();
                SaveSettings();
            }
        }
    }

    public DeviceTarget? SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            if (SetField(ref _selectedDevice, value))
            {
                RaiseCommandStates();
                SaveSettings();
            }
        }
    }

    public string CapturePreview
    {
        get => _capturePreview;
        set => SetField(ref _capturePreview, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetField(ref _isBusy, value);
    }

    public bool IsCapturing
    {
        get => _isCapturing;
        set
        {
            if (SetField(ref _isCapturing, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public bool HasPendingRestore
    {
        get => _hasPendingRestore;
        set
        {
            if (SetField(ref _hasPendingRestore, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public string ProxyApplyStatus
    {
        get => _proxyApplyStatus;
        private set => SetField(ref _proxyApplyStatus, value);
    }

    public string TrafficStatus
    {
        get => _trafficStatus;
        private set => SetField(ref _trafficStatus, value);
    }

    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (SetField(ref _isDarkTheme, value))
            {
                SaveSettings();
            }
        }
    }

    public bool ShowStatusBadge
    {
        get => _showStatusBadge;
        private set => SetField(ref _showStatusBadge, value);
    }

    public ICommand ProbeEnvironmentCommand { get; }
    public ICommand DiscoverDevicesCommand { get; }
    public ICommand ConnectManualCommand { get; }
    public ICommand GenerateCertificateCommand { get; }
    public ICommand PushCertificateCommand { get; }
    public ICommand SemiAutoInstallCaCommand { get; }
    public ICommand OpenCertificateDirectoryCommand { get; }
    public ICommand ConfirmCertificateInstalledCommand { get; }
    public ICommand ReloadRulesCommand { get; }
    public ICommand StartCaptureCommand { get; }
    public ICommand StopCaptureCommand { get; }
    public ICommand CopyUrlCommand { get; }
    public ICommand RecoverProxyCommand { get; }

    private Task ProbeEnvironmentByUserAsync()
    {
        return ProbeEnvironmentAsync(showBadge: true);
    }

    private async Task ProbeEnvironmentAsync(bool showBadge)
    {
        await RunUiAsync(async () =>
        {
            var adb = _toolLocator.FindAdb(string.IsNullOrWhiteSpace(AdbPath) ? null : AdbPath);
            var mitmdump = _toolLocator.FindMitmdump(string.IsNullOrWhiteSpace(MitmdumpPath) ? null : MitmdumpPath);
            AdbPath = adb.Path ?? "";
            MitmdumpPath = mitmdump.Path ?? "";

            HostIps.Clear();
            foreach (var ip in new LocalIpService().GetUsableIpv4Addresses())
            {
                HostIps.Add(ip);
            }

            SelectedHostIp ??= HostIps.FirstOrDefault(ip => ip.Address == _loadedSettings.LastHostIp) ?? HostIps.FirstOrDefault();
            HasPendingRestore = await _stateStore.LoadPendingProxyRestoreAsync() is not null;
            AddLog(adb.IsAvailable ? $"adb: {adb.Path}" : adb.Message ?? "adb 不可用");
            AddLog(mitmdump.IsAvailable ? $"mitmdump: {mitmdump.Path}" : mitmdump.Message ?? "mitmdump 不可用");
            Status = "环境检测完成";

            if (showBadge)
            {
                await ShowStatusBadgeForAsync(TimeSpan.FromSeconds(3));
            }
        });
    }

    private async Task ShowStatusBadgeForAsync(TimeSpan duration)
    {
        _statusBadgeCts?.Cancel();
        _statusBadgeCts?.Dispose();
        _statusBadgeCts = new CancellationTokenSource();
        var token = _statusBadgeCts.Token;
        ShowStatusBadge = true;
        try
        {
            await Task.Delay(duration, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        ShowStatusBadge = false;
    }

    private async Task DiscoverDevicesAsync()
    {
        await RunUiAsync(async () =>
        {
            Devices.Clear();
            var devices = await CreateDiscoveryService().DiscoverWithAutoConnectAsync();

            foreach (var device in devices)
            {
                Devices.Add(device);
            }

            var savedSerial = _loadedSettings.LastSerial;
            SelectedDevice ??= Devices.FirstOrDefault(device => string.Equals(device.Serial, savedSerial, StringComparison.OrdinalIgnoreCase))
                               ?? Devices.FirstOrDefault();
            Status = devices.Count > 0 ? $"已发现模拟器设备：{SelectedDevice?.Serial}" : "未发现模拟器设备";
        });
    }

    private async Task ConnectManualAsync()
    {
        await RunUiAsync(async () =>
        {
            if (!int.TryParse(ManualPort, out var port))
            {
                throw new InvalidOperationException("请输入有效的 ADB 端口。");
            }

            Devices.Clear();
            foreach (var device in await CreateDiscoveryService().ConnectManualAsync(port))
            {
                Devices.Add(device);
            }

            SelectedDevice = Devices.FirstOrDefault(device => device.Port == port) ?? Devices.FirstOrDefault();
            Status = $"已连接 ADB 端口：{port}";
        });
    }

    private async Task GenerateCertificateAsync()
    {
        await RunUiAsync(async () =>
        {
            var cert = await GenerateCertificateCoreAsync();
            Status = "已生成 CA 证书";
            AddLog($"证书路径：{cert}");
        });
    }

    private async Task PushCertificateAsync()
    {
        await RunUiAsync(async () =>
        {
            var device = RequireDevice();
            await PushCertificateCoreAsync(device.Serial);
            Status = "已推送证书到模拟器下载目录";
            AddLog($"请在模拟器中安装 Downloads/{Path.GetFileName(RemoteCertificatePath)}。");
        });
    }

    private async Task SemiAutoInstallCaAsync()
    {
        await RunUiAsync(async () =>
        {
            var device = RequireDevice();
            Status = "半自动安装：正在生成 CA 证书...";
            var cert = await GenerateCertificateCoreAsync();
            Status = "半自动安装：正在推送证书到模拟器...";
            await PushCertificateCoreAsync(device.Serial);
            AddLog($"已推送证书：{RemoteCertificatePath}");
            Status = "半自动安装：正在打开安卓安装页面...";
            var opened = await OpenCertificateInstallSettingsAsync(device.Serial);
            AddLog($"证书路径：{cert}");
            AddLog("请在模拟器内按路径安装：设置-网络和互联网-互联网-网络偏好设置-安装证书。");
            Status = opened
                ? "已打开安装页，请在模拟器完成证书安装后点击“我已安装，检测解密”"
                : "已推送证书，但未能直达安装页，请手动进入“设置-网络和互联网-互联网-网络偏好设置-安装证书”后点击“我已安装，检测解密”";
        });
    }

    private Task OpenCertificateDirectoryAsync()
    {
        var directory = _certificateService.GetDefaultConfDir();
        Directory.CreateDirectory(directory);
        Process.Start(new ProcessStartInfo("explorer.exe", directory) { UseShellExecute = true });
        Status = "已打开证书目录";
        return Task.CompletedTask;
    }

    private async Task ConfirmCertificateInstalledAsync()
    {
        await RunUiAsync(async () =>
        {
            var device = RequireDevice();
            var probe = await RunHttpsDecryptProbeAsync(device.Serial);
            if (probe)
            {
                Status = "CA 安装检测通过：HTTPS 可解密";
                AddLog("检测结果：已可解密 HTTPS。");
                return;
            }

            Status = "CA 安装检测未通过：仍不可解密，请检查是否完成安装";
            AddLog("检测结果：未确认 HTTPS 可解密。");
        });
    }


    private async Task<string> GenerateCertificateCoreAsync()
    {
        return await _certificateService.GenerateCertificateAsync(
            MitmdumpPath,
            _certificateService.GetDefaultConfDir(),
            ProxyPort);
    }

    private async Task PushCertificateCoreAsync(string serial)
    {
        var cert = _certificateService.GetCertificatePath(_certificateService.GetDefaultConfDir());
        await CreateAdbService().PushAsync(serial, cert, RemoteCertificatePath);
    }

    private async Task<bool> OpenCertificateInstallSettingsAsync(string serial)
    {
        var commands = new[]
        {
            "am start -a android.settings.WIFI_IP_SETTINGS",
            "am start -a android.settings.NETWORK_PROVIDER_SETTINGS",
            "am start -a android.settings.WIRELESS_SETTINGS",
            "am start -a android.settings.WIFI_SETTINGS",
            "am start -a android.settings.panel.action.INTERNET_CONNECTIVITY",
            "am start -a android.settings.SETTINGS",
        };
        return await TryStartSettingsPageAsync(serial, commands);
    }

    private async Task<bool> TryStartSettingsPageAsync(string serial, IReadOnlyList<string> commands)
    {
        var adb = CreateAdbService();
        AddLog($"准备在设备 {serial} 打开安卓设置页。");
        foreach (var command in commands)
        {
            try
            {
                var output = await adb.ShellAsync(serial, command);
                AddLog($"执行: {command}");
                if (!string.IsNullOrWhiteSpace(output))
                {
                    AddLog($"返回: {output}");
                }
                if (!output.Contains("Error:", StringComparison.OrdinalIgnoreCase) &&
                    !output.Contains("Exception", StringComparison.OrdinalIgnoreCase))
                {
                    AddLog("设置页跳转命令已被系统接受。");
                    return true;
                }
            }
            catch (Exception ex)
            {
                AddLog($"命令失败: {command} -> {ex.Message}");
            }
        }

        AddLog("所有设置页跳转命令均未成功。");
        return false;
    }

    private async Task<bool> RunHttpsDecryptProbeAsync(string serial)
    {
        var host = SelectedHostIp?.Address ?? throw new InvalidOperationException("请选择主机 IPv4。");
        Status = "正在检测 HTTPS 可解密状态...";
        await EnsureListenEndpointAvailableAsync(host, ProxyPort);
        var adb = CreateAdbService();
        var proxy = new ProxySettingsService(adb);
        var oldProxy = await proxy.ReadProxyAsync(serial);
        var probeTimeout = TimeSpan.FromSeconds(20);
        using var cts = new CancellationTokenSource(probeTimeout);

        try
        {
            await proxy.SetProxyAsync(serial, host, ProxyPort, cts.Token);
            await adb.ShellAsync(serial, "am start -a android.intent.action.VIEW -d https://example.com", cts.Token);
            var capture = await new MitmproxyService().CaptureOnceAsync(
                MitmdumpPath,
                RuntimePath("scripts", "https_probe.py"),
                RuntimePath("config", "url_rules.json"),
                _certificateService.GetDefaultConfDir(),
                host,
                ProxyPort,
                probeTimeout,
                AddLog,
                cts.Token);
            return capture is not null;
        }
        finally
        {
            await proxy.RestoreProxyAsync(serial, oldProxy);
        }
    }

    private async Task ReloadRulesAsync()
    {
        await RunUiAsync(async () =>
        {
            var path = RuntimePath("config", "url_rules.json");
            var rules = await _rulesLoader.LoadAsync(path);
            Status = $"规则重载成功，共 {rules.Count} 条";
            AddLog($"已重载规则文件：{path}");
        });
    }

    private async Task StartCaptureAsync()
    {
        await RunUiAsync(async () =>
        {
            var device = RequireDevice();
            var host = SelectedHostIp?.Address ?? throw new InvalidOperationException("请选择主机 IPv4。");
            await EnsureListenEndpointAvailableAsync(host, ProxyPort);
            _captureCts = new CancellationTokenSource();
            IsCapturing = true;
            _capturedUrl = null;
            CapturePreview = "";
            ProxyApplyStatus = "验证中...";
            TrafficStatus = "启动中...";
            var proxySet = false;

            try
            {
                var adb = CreateAdbService();
                var proxy = new ProxySettingsService(adb);
                var oldProxy = await proxy.ReadProxyAsync(device.Serial, _captureCts.Token);
                var expectedProxy = $"{host}:{ProxyPort}";
                if (string.Equals(oldProxy, expectedProxy, StringComparison.OrdinalIgnoreCase))
                {
                    AddLog($"检测到设备当前代理已是 {expectedProxy}，按残留代理处理。");
                    oldProxy = null;
                }
                await _stateStore.SavePendingProxyRestoreAsync(new ProxyState(device.Serial, oldProxy, host, ProxyPort, DateTimeOffset.Now), _captureCts.Token);
                HasPendingRestore = true;
                await proxy.SetProxyAsync(device.Serial, host, ProxyPort, _captureCts.Token);
                var proxyCurrent = await proxy.ReadProxyAsync(device.Serial, _captureCts.Token);
                ProxyApplyStatus = string.Equals(proxyCurrent, expectedProxy, StringComparison.OrdinalIgnoreCase)
                    ? $"已生效 ({expectedProxy})"
                    : $"读回不一致 (当前: {proxyCurrent ?? "null"})";
                proxySet = true;
                AddLog($"已设置模拟器代理：{host}:{ProxyPort}");

                Status = "mitmproxy 已启动，等待你在游戏中打开抽卡历史页";
                TrafficStatus = "代理进程已启动，等待连接";
                var capture = await new MitmproxyService().CaptureOnceAsync(
                    MitmdumpPath,
                    RuntimePath("scripts", "re1999_capture.py"),
                    RuntimePath("config", "url_rules.json"),
                    _certificateService.GetDefaultConfDir(),
                    host,
                    ProxyPort,
                    Timeout.InfiniteTimeSpan,
                    AddLog,
                    _captureCts.Token);

                if (capture is null)
                {
                    if (!TrafficStatus.StartsWith("已检测到", StringComparison.Ordinal))
                    {
                        TrafficStatus = "未检测到连接";
                    }
                    Status = "未捕获到 URL，可能未安装 CA 或当前版本不支持该方法";
                    return;
                }

                _capturedUrl = capture.Url;
                CapturePreview = capture.Url;
                Status = "已捕获抽卡历史 URL";
                AddLog($"Capture matched rule {capture.MatchedRule}");
                RaiseCommandStates();
            }
            finally
            {
                _captureCts.Dispose();
                _captureCts = null;

                if (proxySet)
                {
                    await RestoreProxyAsync(device.Serial);
                }

                IsCapturing = false;
            }
        });
    }

    private async Task StopCaptureAsync()
    {
        _captureCts?.Cancel();
        IsCapturing = false;
        if (!TrafficStatus.StartsWith("已检测到", StringComparison.Ordinal))
        {
            TrafficStatus = "未检测到连接";
        }
        Status = HasPendingRestore ? "已停止，但恢复代理失败，请点击“修复模拟器代理”重试" : "已停止并恢复代理";
    }

    private Task CopyUrlAsync()
    {
        return RunUiAsync(async () =>
        {
            if (!string.IsNullOrWhiteSpace(_capturedUrl))
            {
                await _clipboardService.SetTextAsync(_capturedUrl);
                Status = "已复制到剪贴板";
            }

        });
    }

    private async Task RecoverProxyAsync()
    {
        await RunUiAsync(async () =>
        {
            var device = RequireDevice();
            var wasCapturing = IsCapturing;
            _captureCts?.Cancel();
            IsCapturing = false;

            await new ProxySettingsService(CreateAdbService()).RestoreProxyAsync(device.Serial, oldProxy: null);
            _stateStore.ClearPendingProxyRestore();
            HasPendingRestore = false;
            if (!TrafficStatus.StartsWith("已检测到", StringComparison.Ordinal))
            {
                TrafficStatus = "未检测到连接";
            }

            Status = wasCapturing ? "已停止抓取并修复模拟器代理配置" : "已修复模拟器代理配置";
            AddLog(wasCapturing
                ? $"已停止抓取并执行网络修复：{device.Serial}"
                : $"已执行网络修复：{device.Serial}");
        });
    }

    private async Task RestoreProxyAsync(string serial)
    {
        try
        {
            var pending = await _stateStore.LoadPendingProxyRestoreAsync();
            if (pending is null)
            {
                HasPendingRestore = false;
                return;
            }

            await new ProxySettingsService(CreateAdbService()).RestoreProxyAsync(serial, pending?.OldProxy);
            _stateStore.ClearPendingProxyRestore();
            HasPendingRestore = false;
            AddLog("已恢复模拟器原代理");
        }
        catch (Exception ex)
        {
            HasPendingRestore = true;
            AddLog($"恢复代理失败：{ex.Message}");
        }
    }

    private async Task RunUiAsync(Func<Task> action)
    {
        IsBusy = true;
        try
        {
            await action();
        }
        catch (OperationCanceledException)
        {
            AddLog("操作已取消");
        }
        catch (Exception ex)
        {
            Status = ex.Message;
            AddLog(ex.Message);
        }
        finally
        {
            IsBusy = false;
            RaiseCommandStates();
        }
    }

    private AdbService CreateAdbService()
    {
        if (string.IsNullOrWhiteSpace(AdbPath))
        {
            throw new InvalidOperationException("未找到 adb，请安装或在路径框中指定。");
        }

        return new AdbService(AdbPath);
    }

    private EmulatorDiscoveryService CreateDiscoveryService()
    {
        return new EmulatorDiscoveryService(CreateAdbService());
    }

    private DeviceTarget RequireDevice()
    {
        return SelectedDevice ?? throw new InvalidOperationException("请先选择模拟器设备。");
    }

    private bool HasAdb() => !string.IsNullOrWhiteSpace(AdbPath);

    private bool HasMitmdump() => !string.IsNullOrWhiteSpace(MitmdumpPath);

    private bool CanStartCapture()
    {
        return HasAdb() && HasMitmdump() && SelectedDevice is not null && SelectedHostIp is not null && !IsCapturing;
    }

    private async Task EnsureListenEndpointAvailableAsync(string host, int port)
    {
        if (!IPAddress.TryParse(host, out var ip))
        {
            throw new InvalidOperationException($"主机 IP 无效：{host}");
        }

        if (CanBind(ip, port))
        {
            return;
        }

        var released = await TryReleaseStaleMitmdumpListenerAsync(host, port);
        if (released && CanBind(ip, port))
        {
            AddLog($"已自动清理残留 mitmdump 进程并释放端口：{host}:{port}");
            return;
        }

        throw new InvalidOperationException($"代理端口 {host}:{port} 已被占用，请更换端口或关闭占用进程后重试。");
    }

    private static bool CanBind(IPAddress ip, int port)
    {
        TcpListener? listener = null;
        try
        {
            listener = new TcpListener(ip, port);
            listener.Start();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
        finally
        {
            listener?.Stop();
        }
    }

    private async Task<bool> TryReleaseStaleMitmdumpListenerAsync(string host, int port)
    {
        var listenerPid = await GetListeningPidAsync(host, port);
        if (listenerPid is null)
        {
            return false;
        }

        try
        {
            var process = Process.GetProcessById(listenerPid.Value);
            if (!string.Equals(process.ProcessName, "mitmdump", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            process.Kill(true);
            await process.WaitForExitAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<int?> GetListeningPidAsync(string host, int port)
    {
        var startInfo = new ProcessStartInfo("netstat")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("-ano");
        startInfo.ArgumentList.Add("-p");
        startInfo.ArgumentList.Add("tcp");

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            return null;
        }

        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
        {
            return null;
        }

        var endpoint = $"{host}:{port}";
        foreach (var rawLine in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (!line.Contains(endpoint, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!line.Contains("LISTEN", StringComparison.OrdinalIgnoreCase) &&
                !line.Contains("侦听", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                continue;
            }

            if (int.TryParse(parts.Last(), out var pid))
            {
                return pid;
            }
        }

        return null;
    }

    private void AddLog(string message)
    {
        var line = $"[{DateTimeOffset.Now:HH:mm:ss}] {UrlMasker.MaskLogLine(message)}";
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            _ = dispatcher.InvokeAsync(() => AppendLog(line, message));
            return;
        }

        AppendLog(line, message);
    }

    private void AppendLog(string line, string rawMessage)
    {
        Logs.Add(line);
        if (IsCapturing &&
            rawMessage.Equals("MITM_READY", StringComparison.Ordinal))
        {
            TrafficStatus = "代理已监听，等待应用连接";
            return;
        }

        if (IsCapturing &&
            rawMessage.Contains("listening at", StringComparison.OrdinalIgnoreCase))
        {
            TrafficStatus = "代理已监听，等待应用连接";
            return;
        }

        if (IsCapturing &&
            !TrafficStatus.StartsWith("已检测到", StringComparison.Ordinal) &&
            rawMessage.Contains("client connect", StringComparison.OrdinalIgnoreCase))
        {
            TrafficStatus = "已检测到连接";
        }
    }

    private static string RuntimePath(params string[] parts)
    {
        var allParts = new string[parts.Length + 1];
        allParts[0] = AppContext.BaseDirectory;
        Array.Copy(parts, 0, allParts, 1, parts.Length);
        return Path.Combine(allParts);
    }

    private void RaiseCommandStates()
    {
        foreach (var command in new[]
                 {
                     ProbeEnvironmentCommand, DiscoverDevicesCommand, ConnectManualCommand, GenerateCertificateCommand,
                     PushCertificateCommand, SemiAutoInstallCaCommand, OpenCertificateDirectoryCommand,
                     ConfirmCertificateInstalledCommand,
                     ReloadRulesCommand, StartCaptureCommand, StopCaptureCommand, CopyUrlCommand,
                     RecoverProxyCommand,
                 })
        {
            if (command is AsyncRelayCommand relay)
            {
                relay.RaiseCanExecuteChanged();
            }
        }
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    private void ApplySavedSettings()
    {
        _suspendSettingsSave = true;
        try
        {
            var settings = _settingsStore.Load();
            _loadedSettings = settings;
            if (!string.IsNullOrWhiteSpace(settings.AdbPath))
            {
                _adbPath = settings.AdbPath;
            }

            if (!string.IsNullOrWhiteSpace(settings.MitmdumpPath))
            {
                _mitmdumpPath = settings.MitmdumpPath;
            }

            if (settings.ProxyPort > 0)
            {
                _proxyPort = settings.ProxyPort;
            }

            _isDarkTheme = settings.IsDarkTheme;
        }
        finally
        {
            _suspendSettingsSave = false;
        }
    }

    private void SaveSettings()
    {
        if (_suspendSettingsSave)
        {
            return;
        }

        _loadedSettings = new AppSettings(
            AdbPath,
            MitmdumpPath,
            SelectedHostIp?.Address,
            SelectedDevice?.Serial,
            ProxyPort,
            IsDarkTheme);
        _settingsStore.Save(_loadedSettings);
    }

    public void ClearSensitiveData()
    {
        _capturedUrl = null;
        CapturePreview = "";
    }
}
