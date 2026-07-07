# CDriveSweep - C盘安全清理工具

[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Platform: Windows](https://img.shields.io/badge/Platform-Windows-blue.svg)]()
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)]()

一个安全的 Windows C 盘清理工具，支持 **18 个清理分类**，提供 GUI 图形界面和 CLI 命令行两种使用方式，内置中英文双语切换。

> **English** | [中文](#chinese)

---

## ✨ Features

- **18 cleanup categories** covering system temp, browser cache, chat apps, large files, duplicates, and more
- **Safe first, clean later** — scan before clean, risk level labels, manual confirmation required
- **GUI + CLI** — WPF desktop app for general users, command-line tool for power users
- **Bilingual** — Chinese and English, switchable in real-time at the click of a button
- **Junction-aware** — automatically skips `mklink /J` reparse points to avoid double-counting
- **Admin-aware** — prompts to run as administrator when permission is required

## 📸 Screenshots

![Main Window](assets/main-window.png)

![Scan Result](assets/scan-result.png)

## 🔧 Cleanup Categories

| Category | Risk | Description |
|----------|------|-------------|
| System Temp Files | Low | `%TEMP%` and `C:\Windows\Temp` |
| Recycle Bin | Low | Empty recycle bin on all drives |
| Browser Cache | Low | Chrome / Edge / Firefox cache |
| Windows Update Cache | Low | `SoftwareDistribution\Download` |
| Prefetch Cache | Low | `C:\Windows\Prefetch` |
| Thumbnail Cache | Low | `thumbcache_*.db` files |
| System Log Files | Low | `.log` / `.etl` in Windows\Logs |
| DNS Cache | Low | `ipconfig /flushdns` |
| Memory Dump Files | Low | `C:\Windows\MEMORY.DMP` |
| Delivery Optimization Cache | Low | Windows Update delivery cache |
| Error Report Files | Low | WER crash dumps |
| Old Windows Backup | Low | `C:\Windows.old` |
| WeChat / WeCom Cache | **Medium** | Chat images, videos, file caches |
| QQ Cache | **Medium** | Group chat image/video caches |
| NuGet Package Cache | **Medium** | Old NuGet package versions |
| Large File Scanner | Review | Files >100MB on C drive |
| Duplicate File Finder | Review | Files with same MD5 hash |
| Empty Folders | Review | Empty directories on C drive |

## 🚀 Quick Start

### Download (Recommended)

Go to [Releases](https://github.com/jiuzhouhai/CDriveSweep/releases) and download `CDriveSweep.exe`.

### Build from Source

```bash
git clone https://github.com/jiuzhouhai/CDriveSweep.git
cd CDriveSweep
dotnet build
```

**Launch GUI:**
```bash
dotnet run --project src/CDriveSweep.App
```

**Launch CLI:**
```bash
dotnet run --project src/CDriveSweep.Cli -- --scan
dotnet run --project src/CDriveSweep.Cli -- --clean -y
dotnet run --project src/CDriveSweep.Cli -- --lang en --scan
```

### Publish as Single EXE

```bash
dotnet publish src/CDriveSweep.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

## 📂 Project Structure

```
CDriveSweep/
├── src/
│   ├── CDriveSweep.Core/       # Core engine + 18 cleaners
│   │   ├── Cleaners/           # ICleaner implementations
│   │   ├── Localization/       # zh-CN / en-US resources
│   │   └── Models/             # ScanItem, ScanResult, CleanResult
│   ├── CDriveSweep.App/        # WPF GUI
│   │   ├── ViewModels/         # MVVM binding models
│   │   └── Converters/         # XAML value converters
│   └── CDriveSweep.Cli/        # CLI tool
├── LICENSE
└── README.md
```

## ⚠️ Disclaimer

This tool deletes files from your system. While we strive to only clean safe, non-essential files, **please back up important data before use**. The author assumes no responsibility for data loss.

---

<a id="chinese"></a>

## CDriveSweep - C盘安全清理工具

一个安全的 Windows C 盘清理工具，支持 **18 个清理分类**，提供 GUI 图形界面和 CLI 命令行两种使用方式，内置中英文双语切换。

### 功能特点

- **18 个清理分类** — 临时文件、浏览器缓存、聊天应用、大文件、重复文件等
- **安全优先** — 先扫描后清理，风险等级标注，需用户确认
- **GUI + CLI 双模式** — WPF 桌面端 + 命令行工具
- **中英双语** — 按钮一键切换，实时生效
- **Junction 感知** — 自动跳过 `mklink /J` 文件夹链接，避免重复计算
- **权限提示** — 需要管理员权限时自动提示

### 快速开始

从 [Releases](https://github.com/jiuzhouhai/CDriveSweep/releases) 下载 `CDriveSweep.exe`，或从源码构建：

```bash
git clone https://github.com/jiuzhouhai/CDriveSweep.git
cd CDriveSweep
dotnet run --project src/CDriveSweep.App
```

### 免责声明

本工具会删除系统文件。尽管我们只清理安全的非必要文件，**请在清理前备份重要数据**。作者不对数据丢失承担任何责任。

## 📄 License

MIT © [jiuzhouhai](https://github.com/jiuzhouhai)
