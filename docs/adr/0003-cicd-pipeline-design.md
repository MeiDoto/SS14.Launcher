# 🚀 Architecture Decision Record (ADR) 0003: CI/CD Pipeline Architecture & Release Automation

[🇬🇧 Read in English](0003-cicd-pipeline-design.md) | [🇷🇺 Читать на русском](0003-cicd-pipeline-design.ru.md)

- **Status**: Accepted
- **Date**: 2026-09-02 (Updated: 2026-09-08)
- **Scope**: GitHub Actions, Cross-Platform Matrix Compilation, Automated Testing, publish.py Packaging

---

## 📋 Context & Problem Statement

**Space Station 14 Launcher** targets three major operating systems: Linux (x64, ARM64), Windows (x64, ARM64), and macOS (x64, ARM64).
Key CI/CD pipeline requirements:
1. **Zero Cross-Platform Regressions**: Architecture or platform incompatibilities must be intercepted prior to merging code into `master`.
2. **Deterministic, Standalone Release Artifacts**: Every official release must provide self-contained archives bundling the official .NET 10 runtime, native bootstrapper, and cryptographic `SHA256SUMS.txt` manifests.
3. **Automated Supply Chain & SAST Security**: Continuous vulnerability auditing of NuGet packages and source code.

---

## 🎯 Decisions

### 1. Matrix Build & Test Pipeline (`build-test.yml`)
- **Triggers**: Push events to `master` and Pull Requests targeting `master`.
- **Environment Matrix**:
  - `ubuntu-latest`
  - `windows-latest`
  - `macos-latest`
- **Execution Flow**:
  1. Recursive submodule checkout (`submodules: recursive`).
  2. Setup .NET 10.0 SDK.
  3. NuGet caching indexed by operating system and project file hashes.
  4. Code style enforcement: `dotnet format --verify-no-changes`.
  5. Compilation across Debug and Release configurations with `-warnaserror:CS4014`.
  6. Execution of the complete 138-test unit suite across every matrix OS.

### 2. Standalone Release Packaging Script (`publish.py`)
- **Decision**: Employ a standardized, cross-platform Python 3 packaging driver (`publish.py`):
  - Automatically fetches the official Microsoft .NET 10 standalone runtime per target platform (`download_net_runtime.py`).
  - Executes `dotnet publish` with ILLink / trimming optimization to minimize final binary footprints.
  - Configures Win32 subsystem flags (`exe_set_subsystem.py`) to eliminate console window flashes on Windows.
  - Bundles ready-to-run release packages:
    - `SS14.Launcher_Windows.zip` (Windows x64 + ARM64, `Space Station 14 Launcher.exe`, icon, batch shortcuts).
    - `SS14.Launcher_Linux.tar.gz` (Linux x64 + ARM64, `SS14.Launcher` shell script, `.desktop` desktop file, icon, setup script).
  - Calculates SHA-256 digests and outputs `SHA256SUMS.txt`.

### 3. Tagged Release Pipeline (`publish-release.yml`)
- **Trigger**: Pushing a version tag (`v*`).
- **Test Gate**: Binary generation initiates **strictly** after the full test suite achieves a 100% pass rate on all platforms.
- **Publishing**: Automated uploading of platform archives and checksums directly to GitHub Releases.

### 4. Continuous Security & Quality Tooling
- **`codeql.yml`**: Scheduled weekly SAST code scans utilizing `security-extended` and `security-and-quality` rulesets.
- **`dependency-review.yml`**: Pull Request gate intercepting vulnerable or malicious package additions.
- **`dependabot.yml`**: Automated dependency maintenance for NuGet packages and GitHub Actions actions.

---

## ⚖️ Consequences & Validation

- **Results**:
  - Fully automated cross-platform release generation eliminating manual build errors.
  - End-users receive self-contained binaries runnable without pre-installed runtimes.
  - Strict test gates guarantee that defective builds never reach distribution channels.
