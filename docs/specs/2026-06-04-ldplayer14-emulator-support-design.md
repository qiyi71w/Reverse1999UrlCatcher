# LDPlayer 14 and Generic Emulator Support Design

## Context Checked

- `docs/specs/2026-05-14-mvp-design.md`
- `docs/plans/2026-05-14-mvp-implementation.md`
- Current implementation files under `src/Reverse1999UrlCatcher.Core/Services`, `src/Reverse1999UrlCatcher.Cli`, and `src/Reverse1999UrlCatcher.App`
- Current tests under `tests/Reverse1999UrlCatcher.Tests`

The MVP design targets Windows + MuMu and explicitly rejects certificate bypass, APK modification, root, system CA injection, transparent proxy, and driver-level capture. This design keeps those boundaries.

## Goal

Add explicit LDPlayer 14 support by turning the current MuMu-named discovery path into a generic Android emulator discovery path.

The app should present the workflow as supporting ADB-based Android emulators, with MuMu and LDPlayer 14 as known supported cases. It should not promise that CA installation is always unnecessary.

## Non-Goals

- No backward-compatible `discover-mumu` CLI alias. The app has no external compatibility requirement yet.
- No LDPlayer multi-instance config parsing.
- No registry or install database scanner.
- No certificate bypass, root workflow, system certificate injection, Frida, Magisk, Xposed, or APK modification.
- No change to URL matching rules.
- No change to capture storage or privacy behavior.

## Decisions

- Rename `MuMuDiscoveryService` to `EmulatorDiscoveryService`.
- Rename CLI `discover-mumu` to `discover-emulator`.
- Update UI, status messages, and docs from MuMu-specific wording to emulator wording.
- Keep MuMu and LDPlayer 14 ADB candidates in `ToolLocator`, plus app-local adb and Android platform-tools.
- Keep current loopback ADB port probing strategy because it already covers LDPlayer 14-style loopback ports in user testing.
- Treat CA installation as an available diagnostic/helper path, not as a required claim for every emulator.

## Components

### Core

`EmulatorDiscoveryService` keeps the existing behavior:

- list current `adb devices -l`
- enrich online devices with model, brand, and Android version
- support manual port connect
- try known historical port `7555`
- scan likely loopback emulator ports

The name changes from MuMu-specific to emulator-generic. The port scan remains best effort and must ignore failed candidate ports.

`ToolLocator` should find `adb.exe` from:

- explicit user path
- known MuMu paths
- known LDPlayer 14 paths
- Android platform-tools
- app-local bundled adb
- `PATH`

Known LDPlayer 14 paths should include the currently observed local install path:

- `D:\leidian\LDPlayer14\adb.exe`

If more LDPlayer default paths are obvious and low-risk, they can be added as static candidates. Do not implement a broad filesystem scan.

### CLI

Replace:

```text
discover-mumu [--port <adbPort>]
```

with:

```text
discover-emulator [--port <adbPort>]
```

Errors should say no running emulator was found, not no MuMu device was found.

### WPF App

Change visible text and logs to emulator-generic wording:

- `MuMu 设备` -> `模拟器设备`
- `自动发现 MuMu` -> `自动发现模拟器`
- `推送证书到 MuMu` -> `推送证书到模拟器`
- `修复 MuMu 无法上网` -> `修复模拟器代理`
- `不要使用 127.0.0.1 作为 MuMu 代理` -> `不要使用 127.0.0.1 作为模拟器代理`

The workflow stays the same: choose host IPv4, select emulator device, optionally generate/push/install CA, start capture, restore proxy on stop.

### Documentation

README and English README should describe supported environments as Windows + ADB-based Android emulator, with MuMu and LDPlayer 14 listed as known supported examples.

CA wording should be conservative:

- keep the certificate installation workflow
- state that some environments may capture successfully without installing CA
- avoid promising that no CA is required

## Testing

Add or update tests to cover:

- `AdbDevicesParser` handles LDPlayer-style `adb devices -l` output.
- No tests depend on the model name being `MuMu12`.
- Existing proxy restore, capture JSON, URL masking, and URL rules tests still pass.

Manual validation should include:

- `dotnet build`
- `dotnet test`
- CLI help or command execution path for `discover-emulator`

## Open Constraints

- User testing indicates LDPlayer 14 can be discovered by the current loopback scan and can capture without manual CA installation in a fresh install. This may depend on emulator, game, or network state and should not be documented as a guaranteed behavior.
- LDPlayer multi-instance port discovery may be improved later through a separate design if loopback scanning produces too many false candidates.
