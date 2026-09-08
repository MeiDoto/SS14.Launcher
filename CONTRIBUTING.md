# 🛠️ Contributing to SS14.Launcher

[🇬🇧 Read in English](CONTRIBUTING.md) | [🇷🇺 Читать на русском](CONTRIBUTING.ru.md)

Thank you for your interest in improving the **Space Station 14 Launcher**! This guide provides in-depth information about the launcher's architecture, internal subsystems, coding standards, internationalization, and pull request procedures.

---

## 📑 Table of Contents

1. [Architectural Overview](#-architectural-overview)
2. [Internal Subsystems & Data Flows](#-internal-subsystems--data-flows)
3. [Developer Environment Setup](#-developer-environment-setup)
4. [Code Quality & Architecture Standards](#-code-quality--architecture-standards)
5. [Localization Guide (i18n)](#-localization-guide-i18n)
6. [Testing & Quality Verification](#-testing--quality-verification)
7. [Submitting a Pull Request](#-submitting-a-pull-request)

---

## 🏛️ Architectural Overview

The repository consists of several modular projects structured within a single unified .NET 10 solution:

| Project / Directory | Description & Purpose |
|---|---|
| **`SS14.Launcher`** | Main client application. Built on **Avalonia UI** utilizing the **MVVM** (Model-View-ViewModel) architectural pattern. Handles user interactions, hub aggregation, theme customization, replay viewing, and CVar persistence. |
| **`SS14.Loader`** | Injected runtime assembly loader. Responsible for loading Robust Toolbox engine assemblies into an isolated `AssemblyLoadContext` to eliminate dependency conflicts. |
| **`Robust.LoaderApi`** | Lightweight interop contract defining communication between the launcher and the Robust engine. |
| **`SS14.Launcher.Bootstrap`** | Native Windows C++ bootstrap executable. Sets proper Win32 subsystem flags (GUI/Console) dynamically to eliminate spurious console window flashing and ensure clean process startup. |
| **`SS14.Launcher.Tests`** | Automated test suite powered by **NUnit** (138 unit tests). Validates algorithm precision, cache eviction, URI sanitization, and data integrity. |
| **`SS14.Launcher/Utility/Algorithms/`** | High-performance modular algorithm suite: SIMD search (`Search/`), Kalman latency filtering (`Filters/`), hybrid LRU+TTL memory cache (`Caching/`), trend estimation (`Statistics/`), and token bucket rate limiting (`RateLimiting/`). |

---

## 🔄 Internal Subsystems & Data Flows

### 1. Authentication & Account Management (`Models/Logins/`)
- Interacts with the Space Station 14 authentication servers via HTTPS.
- Authentication tokens are stored securely with encryption. A background timer proactively refreshes active tokens before expiration (`TokenRefreshInterval`).
- Supports Two-Factor Authentication (2FA) and seamless multi-account profile switching.

### 2. Server Discovery & Telemetry (`Models/ServerStatus/`)
- Server lists are fetched concurrently from the official hub (`hub.spacestation14.com`) and any custom hubs configured by the user.
- Employs **Happy Eyeballs** (RFC 8305) dual-stack algorithm to eliminate connection latency between IPv4 and IPv6 networks.
- Latency probes (ping) are smoothed using a **1D Kalman Filter with Chi-squared outlier gating**, suppressing transient spikes and delivering dependable telemetry.
- Server search leverages a tokenized index accelerated with **SIMD hardware intrinsics** (Vector256 / Vector128).

### 3. Content Delivery & Engine Versioning (`Models/ContentManagement/`, `Models/EngineManager/`)
- Upon connection, the launcher queries the server's build manifest. Missing engine builds are fetched on-demand from the Robust CDN.
- Game content is downloaded via chunked zstd/brotli streams and indexed inside an embedded **SQLite** database configured in **WAL** (Write-Ahead Logging) mode, enabling concurrent downloads while a game client is running.

### 4. Process Isolation & Execution (`Utility/GameProcessRunner.cs`)
- Game instances are launched with isolated environment variables to prevent credential leakage.
- Linux GPU offloading hints are injected automatically (`DRI_PRIME=1`, `__NV_PRIME_RENDER_OFFLOAD=1`, `__GLX_VENDOR_LIBRARY_NAME=nvidia`).
- Supports process priority elevation to reduce frame-time jitter when enabled.

---

## 💻 Developer Environment Setup

### Prerequisites
- **[.NET 10.0 SDK](https://dotnet.microsoft.com/download)** (or newer)
- **Git**
- Recommended IDE: **JetBrains Rider**, **Visual Studio 2022+** (.NET 10 ready), or **VS Code** with C# Dev Kit.

### Clone & Build

```bash
# Clone recursively to pull in all required submodules
git clone --recursive https://github.com/MeiDoto/SS14.Launcher.git
cd SS14.Launcher

# Build Release binary
dotnet build -c Release -p:UseSharedCompilation=false

# Run unit tests
dotnet test SS14.Launcher.Tests/SS14.Launcher.Tests.csproj -p:UseSharedCompilation=false
```

### Running in Development Mode
To isolate your development data from your personal launcher profile, specify `--launcher-data-dir`:

```bash
dotnet run --project SS14.Launcher -- --launcher-data-dir ./dev-userdata
```

---

## 🎯 Code Quality & Architecture Standards

We maintain a strict **10/10 quality standard** with zero compiler warnings permitted:

### 1. Asynchronous Safety (Async Safety)
- All public asynchronous business methods **must return `Task` or `Task<T>`**.
- `async void` is **strictly forbidden** outside Avalonia UI event handlers (`RoutedEventArgs`).
- Fire-and-forget background tasks must always use the discard operator (`_ =`):
  ```csharp
  // CORRECT:
  _ = CommitConfig();
  _ = BackgroundWorkerAsync();

  // INCORRECT (generates compiler warning CS4014):
  CommitConfig();
  ```
- Any fire-and-forget method must catch and log all unhandled exceptions internally to protect against process crashes.

### 2. Error Handling & Structured Logging
- **No empty catch blocks (`catch { }`)!**
- All caught exceptions must be typed and logged using `Serilog`:
  ```csharp
  try
  {
      ExecuteAction();
  }
  catch (Exception ex)
  {
      Log.Warning(ex, "Failed to execute action for {ServerAddress}", address);
  }
  ```
- If an exception is deliberately ignored in a safe fallback branch, write `catch (Exception)` without declaring an unused variable name to prevent CS0168 warnings, accompanied by an explanatory comment or `Log.Debug`.

### 3. Memory & High Performance
- Use `ReadOnlySpan<char>` and pre-allocated static separator arrays instead of frequent `string.Split()` allocations.
- Buffer stream I/O using `ArrayPool<byte>.Shared`.
- Avoid throwing exceptions for control flow in hot paths; return `ValueResult<T>` instead.

---

## 🌐 Localization Guide (i18n)

SS14.Launcher uses Mozilla's **Project Fluent** localization framework.

Translation files are stored in:
- `SS14.Launcher/Assets/Locale/en-US/text.ftl` (English reference)
- `SS14.Launcher/Assets/Locale/ru/text.ftl` (Russian translation)

### Rules for adding strings:
1. **Never hardcode user-facing strings in XAML or C#!**
2. In XAML, bind using the `{Loc ...}` markup extension:
   ```xml
   <TextBlock Text="{Loc my-feature-title}" />
   ```
3. In ViewModels, fetch strings through `LocalizationManager`:
   ```csharp
   var text = LocalizationManager.Instance.GetString("my-feature-status", ("count", count));
   ```
4. Define keys in both `.ftl` files:
   ```fluent
   # en-US/text.ftl
   my-feature-title = Advanced Diagnostics
   my-feature-status = Processed { $count } items successfully.

   # ru/text.ftl
   my-feature-title = Расширенная диагностика
   my-feature-status = Успешно обработано элементов: { $count }.
   ```

---

## 🧪 Testing & Quality Verification

Before submitting any code, verify that all tests pass and that there are zero warnings:

```bash
# 1. Compile with zero warnings
dotnet build SS14.Launcher/SS14.Launcher.csproj -p:UseSharedCompilation=false

# 2. Run all unit tests
dotnet test SS14.Launcher.Tests/SS14.Launcher.Tests.csproj -p:UseSharedCompilation=false

# 3. Test release packaging
python3 publish.py windows linux
```

---

## 🚀 Submitting a Pull Request

1. Fork the repository and create a feature branch (`feature/fast-search`).
2. Write clean, documented code and include unit tests for new behavior.
3. Verify that `dotnet test` achieves 100% pass rate.
4. Submit a Pull Request targeting `master` and fill out the PR template checklist.

Thank you for contributing to Space Station 14! 🚀
