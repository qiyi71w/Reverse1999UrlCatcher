# Reverse1999UrlCatcher MVP Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available and explicitly authorized) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a Windows WPF + CLI MVP that captures a Reverse: 1999 Global summon history URL from MuMu through mitmdump and ADB-managed explicit proxy.

**Architecture:** Create a .NET 10 solution with shared core logic, a CLI wrapper, a WPF MVVM app, and focused unit tests. Keep captured full URLs in memory only, with all logging and UI previews masked.

**Tech Stack:** C# `net10.0`, WPF `net10.0-windows`, xUnit, mitmdump addon script, JSON configuration, Windows DPAPI.

---

## File Structure

- `Reverse1999UrlCatcher.sln`: solution file.
- `src/Reverse1999UrlCatcher.Core`: shared domain, config, parsing, privacy, process, ADB, proxy, certificate, and mitmproxy services.
- `src/Reverse1999UrlCatcher.Cli`: CLI commands calling core services.
- `src/Reverse1999UrlCatcher.App`: WPF shell and view model.
- `tests/Reverse1999UrlCatcher.Tests`: unit tests for pure logic and parsers.
- `config/url_rules.json`: default URL match rules copied to output.
- `scripts/re1999_capture.py`: mitmproxy addon script.
- `build/publish.ps1`: self-contained publish helper.
- `build/package-msix.ps1`: placeholder MSIX helper with explicit environment checks.
- `README.md`: usage, limitations, prerequisites, and privacy notes.

## Task 1: Project Skeleton

**Files:**
- Create: `Reverse1999UrlCatcher.sln`
- Create: `src/Reverse1999UrlCatcher.Core/Reverse1999UrlCatcher.Core.csproj`
- Create: `src/Reverse1999UrlCatcher.Cli/Reverse1999UrlCatcher.Cli.csproj`
- Create: `src/Reverse1999UrlCatcher.App/Reverse1999UrlCatcher.App.csproj`
- Create: `tests/Reverse1999UrlCatcher.Tests/Reverse1999UrlCatcher.Tests.csproj`

- [ ] Create SDK-style project files targeting .NET 10.
- [ ] Add project references from CLI/App/Tests to Core.
- [ ] Add WPF settings to the App project.
- [ ] Verify with `dotnet build` when .NET 10 SDK is available.

## Task 2: Domain and Privacy Core

**Files:**
- Create: `src/Reverse1999UrlCatcher.Core/Domain/*.cs`
- Create: `src/Reverse1999UrlCatcher.Core/Privacy/UrlMasker.cs`
- Test: `tests/Reverse1999UrlCatcher.Tests/UrlMaskerTests.cs`

- [ ] Add records for device, proxy, capture result, tool status, and URL rules.
- [ ] Implement URL masking that keeps scheme, host, path and hides query values.
- [ ] Add tests for masked query strings and non-URL input.

## Task 3: Configuration and Parsers

**Files:**
- Create: `src/Reverse1999UrlCatcher.Core/Config/UrlRulesLoader.cs`
- Create: `src/Reverse1999UrlCatcher.Core/Parsing/AdbDevicesParser.cs`
- Create: `src/Reverse1999UrlCatcher.Core/Parsing/CaptureJsonParser.cs`
- Test: `tests/Reverse1999UrlCatcher.Tests/*ParserTests.cs`

- [ ] Load `url_rules.json` into strongly typed rules.
- [ ] Parse `adb devices -l` output into `DeviceTarget`.
- [ ] Parse only `CAPTURE_JSON:` stdout lines.
- [ ] Test valid and invalid parser inputs.

## Task 4: Process and Tool Services

**Files:**
- Create: `src/Reverse1999UrlCatcher.Core/Services/ProcessRunner.cs`
- Create: `src/Reverse1999UrlCatcher.Core/Services/ToolLocator.cs`
- Create: `src/Reverse1999UrlCatcher.Core/Services/AdbService.cs`
- Create: `src/Reverse1999UrlCatcher.Core/Services/LocalIpService.cs`

- [ ] Implement async process execution with stdout, stderr, timeout, and exit code.
- [ ] Locate tools from explicit path, app-local tools, known MuMu ADB path, then `PATH`.
- [ ] Wrap ADB commands without shell string concatenation.
- [ ] Enumerate non-loopback IPv4 addresses.

## Task 5: Proxy, Certificate, Mitmproxy, and Recovery

**Files:**
- Create: `src/Reverse1999UrlCatcher.Core/Services/ProxySettingsService.cs`
- Create: `src/Reverse1999UrlCatcher.Core/Services/CertificateService.cs`
- Create: `src/Reverse1999UrlCatcher.Core/Services/MitmproxyService.cs`
- Create: `src/Reverse1999UrlCatcher.Core/Services/ProtectedStateStore.cs`
- Test: `tests/Reverse1999UrlCatcher.Tests/ProxyRestoreTests.cs`

- [ ] Normalize Android proxy values and decide restore command.
- [ ] Generate CA through mitmdump confdir startup.
- [ ] Start mitmdump with `scripts/re1999_capture.py` and parse capture events.
- [ ] Persist only pending proxy restore state with DPAPI.
- [ ] Test restore decision behavior.

## Task 6: CLI MVP

**Files:**
- Create: `src/Reverse1999UrlCatcher.Cli/Program.cs`

- [ ] Implement `probe-env`.
- [ ] Implement `discover-mumu`.
- [ ] Implement `gen-ca`.
- [ ] Implement `push-ca --serial <serial>`.
- [ ] Implement `proxy-on --serial <serial> --host <ip> --port <port>`.
- [ ] Implement `proxy-off --serial <serial>`.
- [ ] Implement `capture --serial <serial> --host <ip> --port <port>`.
- [ ] Implement `recover-proxy`.

## Task 7: WPF MVP

**Files:**
- Create: `src/Reverse1999UrlCatcher.App/App.xaml`
- Create: `src/Reverse1999UrlCatcher.App/App.xaml.cs`
- Create: `src/Reverse1999UrlCatcher.App/MainWindow.xaml`
- Create: `src/Reverse1999UrlCatcher.App/MainWindow.xaml.cs`
- Create: `src/Reverse1999UrlCatcher.App/ViewModels/MainViewModel.cs`
- Create: `src/Reverse1999UrlCatcher.App/Commands/AsyncRelayCommand.cs`
- Create: `src/Reverse1999UrlCatcher.App/Services/ClipboardService.cs`

- [ ] Build a single-window operational UI.
- [ ] Wire environment detection, device discovery, manual connect, certificate generation/push, capture start/stop, recovery, clear, and copy.
- [ ] Keep logs masked and in memory.
- [ ] Keep full URL only in view-model memory.

## Task 8: Mitmproxy Script and Defaults

**Files:**
- Create: `scripts/re1999_capture.py`
- Create: `config/url_rules.json`

- [ ] Read rules from JSON file.
- [ ] Match method, HTTPS, host allowlist, path contains, status code, and optional query keys.
- [ ] Emit only one `CAPTURE_JSON:` line for the first valid capture.
- [ ] Avoid file writes and flow mutation.

## Task 9: Docs and Build Scripts

**Files:**
- Create: `README.md`
- Create: `build/publish.ps1`
- Create: `build/package-msix.ps1`

- [ ] Document prerequisites, usage, limitations, privacy, and troubleshooting.
- [ ] Add a self-contained single-file publish script for `win-x64`.
- [ ] Add an MSIX script that fails clearly when packaging tools are unavailable.

## Task 10: Verification

- [ ] Run `dotnet build` when .NET 10 SDK is installed.
- [ ] Run `dotnet test` when .NET 10 SDK is installed.
- [ ] Run Python syntax check for `scripts/re1999_capture.py`.
- [ ] Confirm no source writes full captured URL to disk.
- [ ] Document skipped validation if local SDK/tools are missing.
