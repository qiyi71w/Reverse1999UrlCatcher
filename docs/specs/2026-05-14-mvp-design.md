# Reverse1999UrlCatcher MVP Design

## Context Checked

- `docs/` did not exist before this spec.
- No existing source code, tests, plans, or regression contracts were present.
- `deep-research-report.md` is the source brief for this MVP.
- Current machine has .NET 10 SDK installed.
- `adb` and `mitmdump` can be discovered from known install paths.

## Goal

Build a Windows-only MVP that helps the user capture the Reverse: 1999 Global summon history URL from MuMu through an explicit local HTTPS proxy.

The tool manages the local workflow around ADB, mitmdump, certificate generation, certificate push, proxy setup, URL capture, copy-to-clipboard, and proxy restoration. The user still opens the game and summon history page manually.

## Non-Goals

- No certificate pinning bypass.
- No APK modification.
- No Frida, Magisk, Xposed, root, or system certificate injection.
- No transparent proxy or driver-level capture.
- No uploading, sharing, analytics, crash upload, or third-party site integration.
- No persistent storage of captured full URLs.
- No complete release polish beyond the MVP packaging scripts and documentation.

## Target Runtime

- GUI: WPF on `net10.0-windows`.
- CLI and tests: `net10.0`.
- Build requirement: .NET 10 SDK.
- The implementation does not multi-target .NET 8 unless requested later.

## Architecture

The solution uses four projects:

- `Reverse1999UrlCatcher.Core`
  - Domain models, configuration loading, URL masking, process output parsing, and service interfaces.
- `Reverse1999UrlCatcher.Cli`
  - Commands for environment probing, MuMu discovery, certificate generation/push, proxy on/off, capture, and recovery.
- `Reverse1999UrlCatcher.App`
  - WPF MVVM shell for the same workflow.
- `Reverse1999UrlCatcher.Tests`
  - Unit tests for pure logic and command-output parsing.

The GUI and CLI share the core services where practical. UI-specific clipboard and presentation state stay in the WPF app.

## Components

### Domain

- `DeviceTarget`: ADB serial, optional port, model, brand, Android version.
- `ProxyState`: selected serial, old proxy value, new proxy endpoint, pending restore timestamp.
- `CaptureResult`: full URL in memory, masked preview, host, path, matched rule.
- `UrlMatchRule`: rule name, environment, host allowlist, path contains list, method, HTTPS requirement, response status requirement, query keys.

### Infrastructure

- `ToolLocator`: finds `adb.exe` and `mitmdump.exe` from user settings, app-local tools, MuMu known path, then `PATH`.
- `AdbService`: wraps `adb devices`, `adb connect`, `adb push`, `adb shell`, and property reads.
- `MuMuDiscoveryService`: lists connected devices, supports manual port connect, tries historical MuMu port `7555`, and optionally loopback listener candidates.
- `LocalIpService`: lists usable IPv4 addresses and marks one recommended private address.
- `MitmproxyService`: starts `mitmdump`, parses `CAPTURE_JSON:` stdout, captures stderr as masked logs, and stops the process.
- `CertificateService`: generates mitmproxy confdir and locates `mitmproxy-ca-cert.cer`.
- `ProxySettingsService`: reads, sets, deletes, and restores Android `global http_proxy`.
- `ProtectedStateStore`: stores only pending proxy restore state with Windows DPAPI.
- `DiagnosticsService`: keeps masked in-memory logs and optional diagnostic text without full URLs.

## Data Flow

1. Detect `adb`, `mitmdump`, MuMu devices, and host IPv4 addresses.
2. User selects or connects a MuMu ADB target.
3. Generate a local mitmproxy confdir and CA certificate.
4. Push `mitmproxy-ca-cert.cer` to `/sdcard/Download/mitmproxy-ca-cert.cer`.
5. Show Android certificate installation instructions.
6. Read existing MuMu proxy value and save pending restore state via DPAPI.
7. Set MuMu proxy to the selected host IP and port.
8. Start `mitmdump` with `scripts/re1999_capture.py` and `config/url_rules.json`.
9. User opens the game summon history page manually.
10. The mitmproxy script emits one `CAPTURE_JSON:` line for the first matching URL.
11. The app stores the full URL only in memory, shows a masked preview, and enables copy.
12. Stop restores the previous proxy and clears pending restore state.
13. Next launch exposes recovery if a pending proxy state remains.

## URL Matching Contract

Default rules live in `config/url_rules.json`, not hardcoded as the only truth.

Initial rule:

- `name`: `global-default`
- `environment`: `official-global`
- `hosts`: `["game-re-en-service.sl916.com", "game-re-service.sl916.com"]`
- `pathContains`: `["/query/summon"]`
- `method`: `GET`
- `requireHttps`: `true`
- `requireStatusCode`: `200`

The mitmproxy script observes traffic only. It must not modify, replay, block, or save flows.

## Privacy Contract

- Full captured URL is only kept in process memory.
- Logs and UI previews mask query strings and common sensitive names.
- No HAR, raw flow, cookie, header, body, or full query persistence.
- Pending proxy restore state may be persisted, encrypted with DPAPI, and cleared after restore.

## Error Handling

The MVP must show actionable errors for:

- missing `adb`
- missing `mitmdump`
- no MuMu device found
- manual ADB connect failure
- no usable host IPv4
- certificate not generated
- certificate push failure
- proxy read/set/restore failure
- mitmdump port in use
- mitmdump exits unexpectedly
- capture timeout or likely CA trust/pinning failure

When CA trust or pinning failure is suspected, the tool stops at a clear unsupported message and does not attempt bypasses.

## Testing

Unit tests cover:

- `adb devices -l` parsing.
- Android proxy value normalization and restore decision.
- `CAPTURE_JSON:` parsing.
- URL masking.
- `url_rules.json` loading.
- pending restore serialization shape without storing captured URLs.

Manual MVP validation requires installed .NET 10 SDK, `adb`, `mitmdump`, and a running MuMu instance.

## Open Constraints

- Default host/path values come from community evidence in `deep-research-report.md`, not official game API documentation.
- If the current game build does not trust user-installed CA, the supported workflow ends with an unsupported message.
