# 🚀 Space Station 14 Launcher

[🇷🇺 Читать на русском](README.md) | [🇬🇧 Read in English](README.en.md)

Welcome! This is the official modern cross-platform game launcher for **[Space Station 14](https://spacestation14.com/)** — a multiplayer space role-playing sandbox game.

Built with **C# / .NET 10** and **Avalonia UI**, the launcher is fast, lightweight, highly customizable, and runs natively on Windows, Linux, and macOS.

---

## 📥 Download & Play

The easiest way to jump in is to download a ready-to-run package from our **[Releases Page](https://github.com/MeiDoto/SS14.Launcher/releases/latest)**:

- 🪟 **Windows** — download `SS14.Launcher_Windows.zip`, extract it to any directory, and run `Space Station 14 Launcher.exe`.
- 🐧 **Linux** — download `SS14.Launcher_Linux.tar.gz`, extract it, and run `./SS14.Launcher` (or install the desktop launcher shortcut via `./setup-desktop.sh`).

> [!TIP]
> All necessary .NET runtime components are self-contained inside the archive. You don't need to install any external runtimes!

---

## ✨ Features

### 🎮 For Players
- **Effortless Server Discovery** — instantaneous filtering by name, region, language, and active player counts.
- **Favorites & History** — bookmark your favorite servers and jump right in with a single click.
- **Smooth & Accurate Ping** — intelligent network latency telemetry powered by a 1D Kalman filter that eliminates misleading lag spikes.
- **Network Diagnostics Tool** — built-in DNS, TCP socket, TLS handshake, jitter, and packet loss analyzer with report export for community server support.
- **Storage & Cache Manager** — transparent storage breakdown (SQLite content DB, WAL journal, Robust engines, logs, replays) with selective pruning and database `VACUUM`.
- **Replay Inspector** — detailed metadata inspector for round recordings (server, map, duration, game mode, file manifest, uncompressed size).
- **Visual Customizer** — personalize your launcher: custom wallpapers (including video/animated backgrounds), accent colors, custom fonts, and themes.
- **Multi-Account Support** — seamlessly switch between multiple Space Station 14 accounts without typing passwords repeatedly.
- **Bilingual by Default** — high quality translations for English and Russian with dynamic live language switching.

### 🛠️ For Developers & Server Hosts
- **Development Tab** — in-game diagnostic toggles: HUD FPS counters, network graphs, physics bounds, lighting overlays, cache cleanup, and built-in benchmark tools.
- **Local Builds** — run and test custom engine/client builds without rebuilding the entire launcher.
- **Direct Connect & Protocols** — full support for `ss14://` and `ss14s://` deep links with strict command injection protection.

---

## 🖥️ Tested Platforms & Operating Systems

The launcher is actively verified and tested on the following operating systems and runtime environments:

| Platform | Distro / OS Version | Architecture | Display Server & Runtime | Status |
|---|---|---|---|:---:|
| **Linux** | **CachyOS** (Linux Kernel 6.x+, x86-64-v3 / Zen) | `x64` | .NET 10.0.11 + Wayland / X11 | ✅ Verified |
| **Linux** | **Arch Linux** | `x64` | .NET 10.0.11 + Wayland / X11 | ✅ Verified |
| **Linux** | **Ubuntu 24.04 / 22.04 LTS** | `x64`, `arm64` | .NET 10.0.11 + GNOME / X11 | ✅ Verified |
| **Linux** | **Debian 12 (Bookworm)** | `x64` | .NET 10.0.11 + GNOME / KDE | ✅ Verified |
| **Linux** | **Fedora 39 / 40 / 41** | `x64` | .NET 10.0.11 + GNOME Wayland | ✅ Verified |
| **Windows** | **Windows 11 / 10** (22H2+) | `x64`, `arm64` | .NET 10.0.11 (NativeAOT Bootstrap) | ✅ Verified |
| **macOS** | **macOS 12–15** (Monterey – Sequoia) | `x64`, `arm64` | .NET 10.0.11 (Universal Bundle) | ✅ Verified |

---

## 📂 Where Is Data Stored?

| OS | User Data & Installed Content | Logs Directory |
|---|---|---|
| **Windows** | `%APPDATA%\Space Station 14\launcher\` | `%APPDATA%\Space Station 14\launcher\logs\` |
| **Linux** | `~/.local/share/Space Station 14/launcher/` | `~/.local/share/Space Station 14/launcher/logs/` |
| **macOS** | `~/Library/Application Support/Space Station 14/launcher/` | `~/Library/Application Support/Space Station 14/launcher/logs/` |

---

## 💻 Building from Source

If you want to build the project yourself or contribute code:

### Prerequisites
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- Git

### Build Instructions

```bash
# Clone the repository with submodules
git clone --recursive https://github.com/MeiDoto/SS14.Launcher.git
cd SS14.Launcher

# Build the Release binary
dotnet build -c Release -p:UseSharedCompilation=false

# Run all 157 unit tests (100% pass rate)
dotnet test SS14.Launcher.Tests/SS14.Launcher.Tests.csproj -p:UseSharedCompilation=false

# Package standalone release archives
python3 publish.py windows linux
```

---

## 📚 Documentation & Architecture

Comprehensive bilingual documentation is available across several dedicated guides:

- 🏛️ **[Architecture & Subsystems](docs/ARCHITECTURE.md)** ([Русский](docs/ARCHITECTURE.ru.md)) — low-level breakdown of MVVM layers, fuzzy search algorithms, Kalman ping filtering, and process execution.
- 🌐 **[Networking Protocols & API](docs/NETWORKING.md)** ([Русский](docs/NETWORKING.ru.md)) — hub API specifications, Happy Eyeballs RFC 8305, status schema, and Token Bucket rate limiting.
- 🎨 **[Customization & Extensibility](docs/CUSTOMIZATION.md)** ([Русский](docs/CUSTOMIZATION.ru.md)) — UI personalization, launch options, GPU offloading, and developer tooling.
- 🛡️ **[Security Policy](SECURITY.md)** ([Русский](SECURITY.ru.md)) — STRIDE threat modeling, token cryptography, ZipSlip/TarSlip mitigation, and process environment scrubbing.
- 🤝 **[Contributor Guide](CONTRIBUTING.md)** ([Русский](CONTRIBUTING.ru.md)) — development setup, coding standards, async/await rules, and Fluent localization.
- 📐 **[Architectural Decision Records (ADRs)](docs/adr/)** — documented technical consensus:
  - [ADR 0001: Architecture Decisions](docs/adr/0001-architecture-decisions.md) ([Русский](docs/adr/0001-architecture-decisions.ru.md))
  - [ADR 0002: Async Safety & Error Handling](docs/adr/0002-async-safety-error-handling.md) ([Русский](docs/adr/0002-async-safety-error-handling.ru.md))
  - [ADR 0003: CI/CD Pipeline Design](docs/adr/0003-cicd-pipeline-design.md) ([Русский](docs/adr/0003-cicd-pipeline-design.ru.md))

---

## 📜 License

This project is licensed under the terms of the **[MIT License](LICENSE.txt)**.
Space Station 14 and related assets belong to their respective creators.
