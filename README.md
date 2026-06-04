<div align="center">

# Reverse1999UrlCatcher

Windows 桌面工具：辅助抓取《重返未来：1999》抽卡历史 URL（ADB 模拟器 + mitmproxy）。

<a href="readme.md">简体中文</a> ｜ <a href="readme_en.md">English</a>

<img alt="License" src="https://img.shields.io/badge/license-MIT-97CA00?style=flat-square&labelColor=555555" />
<img alt=".NET" src="https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&labelColor=555555" />
<img alt="WPF" src="https://img.shields.io/badge/WPF-desktop-0A84FF?style=flat-square&labelColor=555555" />
<img alt="Platform" src="https://img.shields.io/badge/platform-windows-1F6FEB?style=flat-square&labelColor=555555" />

</div>

## 项目简介

Reverse1999UrlCatcher 用于在 **Windows + 基于 ADB 的 Android 模拟器** 场景下，协助完成：

- 检测 `adb` / `mitmdump` 环境
- 自动发现并连接模拟器 ADB 设备
- 生成并推送 mitmproxy CA 证书
- 自动配置模拟器代理
- 启动代理抓取并匹配抽卡历史 URL
- 一键复制 URL
- 停止后自动恢复原代理，或一键修复模拟器代理

当前仓库包含：

- WPF GUI：`src/Reverse1999UrlCatcher.App`
- CLI：`src/Reverse1999UrlCatcher.Cli`
- Core：`src/Reverse1999UrlCatcher.Core`

## 免责声明

> [!WARNING]
> 本项目仅供学习与技术交流使用，请勿用于任何违反法律法规、游戏用户协议或平台规则的用途。  
> 使用本项目产生的任何后果由使用者自行承担，作者与贡献者不承担任何责任。  
> 若本项目内容涉及侵权，请联系作者处理（包括删库/删除相关内容）。

## 系统要求

- Windows 10/11 x64
- .NET 10 SDK（源码运行时）
- 基于 ADB 的 Android 模拟器（已启动实例；已知支持示例：MuMu、LDPlayer 14）
- `adb.exe`（可在软件中手填路径）
- `mitmdump.exe`（可在软件中手填路径）

> 提示：不要求把 `adb` / `mitmdump` 写入系统环境变量，填绝对路径即可生效。

## 快速开始（GUI）

1. 运行 `Reverse1999UrlCatcher.App.exe`。
2. 在“环境”区域确认或填写 `adb` 与 `mitmdump` 路径。
3. 点击“检测环境”。
4. 点击“自动发现模拟器”或“连接 ADB 端口”。
5. 如需安装证书，在证书区域点击“半自动安装 CA（生成+推送+打开安装页）”，并按提示在模拟器中完成证书安装：
   `设置-网络和互联网-互联网-网络偏好设置-安装证书`
6. 选择主机 IP，点击“启动抓取”。
7. 进入游戏抽卡历史页，命中后复制结果 URL。

## CLI 用法

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

## 构建与打包

```powershell
dotnet build Reverse1999UrlCatcher.sln
dotnet test Reverse1999UrlCatcher.sln
powershell -ExecutionPolicy Bypass -File .\build\publish.ps1 -Configuration Release -Runtime win-x64 -Zip
```

输出目录默认为 `dist/`。

## 已知说明

- 抓取结果受游戏版本、网络环境、证书信任状态影响。
- 部分环境可能不安装 CA 也能捕获；证书安装和检测功能仍保留，用于需要 HTTPS 解密确认的环境。
- 日志中出现其他域名请求（如崩溃上报、H5/客服域名）属于设备正常网络流量，不代表规则命中错误。
- 若模拟器临时断网，可使用“修复模拟器代理”按钮清理代理残留配置。
