# LDPlayer 14 Emulator Support Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add explicit LDPlayer 14 support by making the current MuMu-named workflow generic for ADB-based Android emulators.

**Architecture:** Keep the existing ADB, proxy, certificate, and mitmproxy flow. Rename the emulator discovery surface from MuMu-specific to generic emulator wording, add LDPlayer 14 adb path candidates, and update CLI/UI/docs so the product no longer presents MuMu as the only supported emulator.

**Tech Stack:** C# `net10.0`, WPF `net10.0-windows`, xUnit, Markdown docs.

---

## File Structure

- `src/Reverse1999UrlCatcher.Core/Services/EmulatorDiscoveryService.cs`: generic emulator discovery service, renamed from MuMu.
- `src/Reverse1999UrlCatcher.Core/Services/ToolLocator.cs`: adb candidate paths, including MuMu, LDPlayer 14, Android platform-tools, app-local adb, and PATH.
- `src/Reverse1999UrlCatcher.Cli/Program.cs`: rename `discover-mumu` command to `discover-emulator`.
- `src/Reverse1999UrlCatcher.App/ViewModels/MainViewModel.cs`: emulator-generic status/log/error text and discovery service type.
- `src/Reverse1999UrlCatcher.App/MainWindow.xaml`: emulator-generic visible labels.
- `README.md` and `readme_en.md`: supported emulator and CA wording updates.
- `tests/Reverse1999UrlCatcher.Tests/AdbDevicesParserTests.cs`: LDPlayer-style parser coverage.

## Task 1: Core and CLI Discovery Rename

**Files:**
- Rename: `src/Reverse1999UrlCatcher.Core/Services/MuMuDiscoveryService.cs` -> `src/Reverse1999UrlCatcher.Core/Services/EmulatorDiscoveryService.cs`
- Modify: `src/Reverse1999UrlCatcher.Core/Services/ToolLocator.cs`
- Modify: `src/Reverse1999UrlCatcher.Cli/Program.cs`
- Test: `tests/Reverse1999UrlCatcher.Tests/AdbDevicesParserTests.cs`

- [ ] Add a parser test for LDPlayer-style output:

```csharp
[Fact]
public void Parse_ReturnsLdPlayerDevice()
{
    const string output = """
    List of devices attached
    127.0.0.1:5555 device product:leidian model:LDPlayer14 device:android transport_id:2
    """;

    var devices = AdbDevicesParser.Parse(output);

    Assert.Single(devices);
    Assert.Equal("127.0.0.1:5555", devices[0].Serial);
    Assert.Equal(5555, devices[0].Port);
    Assert.Equal("LDPlayer14", devices[0].Model);
}
```

- [ ] Update the existing MuMu parser test so it validates generic online-device parsing without making `MuMu12` the only expected model. It may still use a MuMu sample as one known emulator example, but the test name and assertion intent should be generic.
- [ ] Run `dotnet test --no-restore --filter AdbDevicesParserTests`.
  Expected before implementation: test compile or execution failure if references still assume MuMu-only behavior.
- [ ] Rename `MuMuDiscoveryService` class and file to `EmulatorDiscoveryService`.
- [ ] Update references in CLI and WPF view model to the new class name.
- [ ] Rename `ToolLocator` adb candidate array from MuMu-specific to emulator-generic.
- [ ] Add LDPlayer 14 static adb candidates, including:

```csharp
@"D:\leidian\LDPlayer14\adb.exe"
```

- [ ] Rename CLI command dispatch from `discover-mumu` to `discover-emulator`.
- [ ] Update CLI method names, usage text, and error text to emulator-generic wording.
- [ ] Do not keep a `discover-mumu` alias.
- [ ] Add or run a CLI verification that proves `discover-mumu` is no longer a recognized command, either by checking it is absent from usage text or by running it and confirming the unknown-command path.
- [ ] Run `dotnet test --no-restore --filter AdbDevicesParserTests`.
  Expected after implementation: pass.

## Task 2: WPF Emulator Wording

**Files:**
- Modify: `src/Reverse1999UrlCatcher.App/ViewModels/MainViewModel.cs`
- Modify: `src/Reverse1999UrlCatcher.App/MainWindow.xaml`

- [ ] Replace visible UI labels from MuMu-specific wording to emulator wording:

```text
MuMu 设备 -> 模拟器设备
自动发现 MuMu -> 自动发现模拟器
推送证书到 MuMu -> 推送证书到模拟器
修复 MuMu 无法上网 -> 修复模拟器代理
不要使用 127.0.0.1 作为 MuMu 代理 -> 不要使用 127.0.0.1 作为模拟器代理
```

- [ ] Replace view-model status, log, and exception text from MuMu-specific wording to emulator wording.
- [ ] Keep the existing workflow and command bindings unchanged.
- [ ] Search for remaining `MuMu` references in `src/Reverse1999UrlCatcher.App`.
  Expected: no MuMu-specific UI/status text remains unless it is clearly about a known supported example.

## Task 3: README and Final Verification

**Files:**
- Modify: `README.md`
- Modify: `readme_en.md`

- [ ] Update the supported environment from Windows + MuMu to Windows + ADB-based Android emulator.
- [ ] List MuMu and LDPlayer 14 as known supported examples.
- [ ] Replace `discover-mumu` usage with `discover-emulator`.
- [ ] Keep CA guidance conservative:

```text
部分环境可能不安装 CA 也能捕获；证书安装和检测功能仍保留，用于需要 HTTPS 解密确认的环境。
```

- [ ] Do not promise that CA installation is never required.
- [ ] Run `dotnet build`.
  Expected: build succeeds.
- [ ] Run `dotnet test`.
  Expected: all tests pass.
- [ ] Run `python -m py_compile scripts/re1999_capture.py scripts/https_probe.py` using available Python.
  Expected: no syntax errors.
- [ ] Run a final search:

```powershell
rg -n "discover-mumu|MuMu" src README.md readme_en.md
```

Expected: no `discover-mumu`; `MuMu` may remain in README files only as a known supported emulator example.
