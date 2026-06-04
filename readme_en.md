<div align="center">

# Reverse1999UrlCatcher

Windows desktop utility for capturing Reverse: 1999 summon-history URL (ADB emulator + mitmproxy).

<a href="readme.md">简体中文</a> ｜ <a href="readme_en.md">English</a>

<img alt="License" src="https://img.shields.io/badge/license-MIT-97CA00?style=flat-square&labelColor=555555" />
<img alt=".NET" src="https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&labelColor=555555" />
<img alt="WPF" src="https://img.shields.io/badge/WPF-desktop-0A84FF?style=flat-square&labelColor=555555" />
<img alt="Platform" src="https://img.shields.io/badge/platform-windows-1F6FEB?style=flat-square&labelColor=555555" />

</div>

## Overview

Reverse1999UrlCatcher helps with the full flow on **Windows + ADB-based Android emulator**:

- Detect `adb` / `mitmdump`
- Discover and connect emulator ADB devices
- Generate and push mitmproxy CA certificate
- Start proxy capture and match summon-history URL
- Copy captured URL
- Restore proxy on stop, or repair emulator proxy settings

Repository layout:

- WPF GUI: `src/Reverse1999UrlCatcher.App`
- CLI: `src/Reverse1999UrlCatcher.Cli`
- Core: `src/Reverse1999UrlCatcher.Core`

## Disclaimer

> [!WARNING]
> This project is for learning and technical communication only.  
> Do not use it for any activity that violates laws, game terms, or platform policies.  
> You are solely responsible for any consequences.  
> If any content is infringing, please contact the author for removal (including repository/content deletion).

## Requirements

- Windows 10/11 x64
- .NET 10 SDK (for source build/run)
- ADB-based Android emulator (running instance; known supported examples: MuMu, LDPlayer 14)
- `adb.exe` (path can be set in the app)
- `mitmdump.exe` (path can be set in the app)

> You do not need system-wide PATH variables for `adb` or `mitmdump` if absolute paths are provided in UI.

## Quick Start (GUI)

1. Launch `Reverse1999UrlCatcher.App`.
2. Fill or confirm `adb` and `mitmdump` paths.
3. Click **Detect Environment**.
4. Click **Auto Discover Emulator** or connect by ADB port.
5. If certificate installation is needed, click **Semi-auto Install CA** in the certificate section and complete certificate installation in the emulator:
   `Settings -> Network & internet -> Internet -> Network preferences -> Install certificates`
6. Select host IP and click **Start Capture**.
7. Open summon history page in game and copy captured URL.

## CLI Commands

```bash
probe-env
discover-emulator [--port <adbPort>]
gen-ca [--port <proxyPort>] [--confdir <path>]
push-ca --serial <serial> [--confdir <path>]
proxy-on --serial <serial> --host <ip> [--port <proxyPort>]
proxy-off --serial <serial>
capture --host <ip> [--serial <serial>] [--port <proxyPort>] [--timeout <seconds>]
recover-proxy
```

## Build & Package

```powershell
dotnet build Reverse1999UrlCatcher.sln
dotnet test Reverse1999UrlCatcher.sln
powershell -ExecutionPolicy Bypass -File .\build\publish.ps1 -Configuration Release -Runtime win-x64 -Zip
```

Output is generated in `dist/`.

## Notes

- Capture success depends on game version, network conditions, and certificate trust state.
- Some environments may capture without installing a CA; certificate installation and checks remain available for environments that need HTTPS decryption confirmation.
- Logs may include non-target domains (crash reporting, H5 pages, support services). This is normal background traffic.
- When multiple emulators are running and ADB ports collide, auto discovery tries local loopback aliases to separate same-port instances. If the target instance still cannot be found, close other emulators first, then run auto discovery again or connect by ADB port manually.
- If the emulator loses network connectivity after capture, use the **Repair Emulator Proxy** action in app.
