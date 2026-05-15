# Reverse1999UrlCatcher

Windows desktop tool for capturing the Reverse: 1999 Global summon history URL from MuMu through a local explicit HTTPS proxy.

The user still opens the game and summon history page manually. The tool handles environment checks, ADB device discovery, mitmproxy certificate generation, certificate push, MuMu proxy setup, capture, copy, and proxy restoration.

## Scope

Supported:

- Windows
- MuMu emulator
- Reverse: 1999 Global / EN summon history URL
- Explicit HTTP(S) proxy through `mitmdump`
- User-installed mitmproxy CA certificate

Not supported:

- certificate pinning bypass
- APK modification
- Frida, Magisk, Xposed, root, or system certificate injection
- transparent proxy capture
- uploading or sharing captured URLs
- saving full captured URLs to disk

If the current game version does not trust a user-installed CA, this tool stops and reports that the method is unsupported.

## Prerequisites

- .NET 10 SDK
- MuMu running with ADB enabled
- `adb.exe`
- `mitmdump.exe` from mitmproxy

Tool discovery order:

1. path typed in the app or CLI option
2. app-local `tools/` folder
3. known MuMu ADB paths
4. `PATH`

## CLI

```powershell
dotnet run --project src/Reverse1999UrlCatcher.Cli -- probe-env
dotnet run --project src/Reverse1999UrlCatcher.Cli -- discover-mumu --port 16384
dotnet run --project src/Reverse1999UrlCatcher.Cli -- gen-ca --port 8877
dotnet run --project src/Reverse1999UrlCatcher.Cli -- push-ca --serial 127.0.0.1:16384
dotnet run --project src/Reverse1999UrlCatcher.Cli -- proxy-on --serial 127.0.0.1:16384 --host 192.168.1.20 --port 8877
dotnet run --project src/Reverse1999UrlCatcher.Cli -- capture --serial 127.0.0.1:16384 --host 192.168.1.20 --port 8877
dotnet run --project src/Reverse1999UrlCatcher.Cli -- proxy-off --serial 127.0.0.1:16384
```

`capture` 命令在提供 `--serial` 时会自动设置并恢复代理；不提供 `--serial` 时只负责监听并捕获 URL。

Do not use `127.0.0.1` as the MuMu proxy host. Choose a Windows host IPv4 reachable from MuMu.

## GUI

```powershell
dotnet run --project src/Reverse1999UrlCatcher.App
```

Basic flow:

1. Detect environment.
2. Select a host IPv4 address.
3. Discover MuMu or connect a manual ADB port.
4. Generate CA certificate.
5. Push certificate to MuMu.
6. Install `mitmproxy-ca-cert.cer` in MuMu:
   `Security & privacy` -> `More security settings` -> `Encryption & credentials` -> `Install a certificate` -> `CA certificate`.
7. Start capture.
8. Open Reverse: 1999 summon history manually.
9. Copy the captured URL.
10. Stop and restore proxy.

UI extras:

- Reload `config/url_rules.json` without restarting the app.
- Persist `adb` path, `mitmdump` path, last host IP, last serial, and proxy port to local app settings.

Known working summon hosts in current rules:

- `game-re-en-service.sl916.com`
- `game-re-service.sl916.com`

## Privacy

- The full captured URL is only held in process memory.
- UI preview and logs mask query values.
- The tool does not save HAR, raw flows, cookies, headers, body, or full URLs.
- Pending proxy restoration state is encrypted with Windows DPAPI and cleared after restore.

## Build

```powershell
dotnet build
dotnet test
.\build\publish.ps1
```

Optional MSIX packaging is intentionally deferred in MVP. `build\package-msix.ps1` exits with a clear message until a packaging manifest is added.

This repository targets .NET 10. Install the .NET 10 SDK before building.
