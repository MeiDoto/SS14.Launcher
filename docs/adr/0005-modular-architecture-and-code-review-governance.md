# ADR 0005: Modular Architecture, Dependency Inversion, and Code Review Governance

* **Status:** Accepted
* **Date:** 2026-09-10
* **Author:** Space Station 14 Launcher Contributors
* **Context:** Fork core refactoring, upstream monolith decoupling, and quality governance standards

---

## 1. Context and Problem Statement

Historically, the `SS14.Launcher` codebase evolved as a fork of the official `space-wizards/SS14.Launcher`. In the upstream architecture, key subsystems (`DataManager`, `Updater`, `LoginManager`, `EngineManagerDynamic`) were tightly-coupled concrete classes. This imposed several constraints:
1. **Tight Coupling:** The presentation layer (ViewModels) and consumers (`Connector`, `LauncherCommands`) depended directly on concrete implementations, complicating isolated unit testing without live SQLite databases, sockets, or filesystem layouts.
2. **Low Core Testability:** Lack of interface abstractions prevented mocking and stubbing upstream components in automated tests.
3. **Absence of Formalized Code Review Governance:** Solo development risks blind spots, inconsistent code styles, and coverage regression without automated quality gates.

---

## 2. Decision

### 2.1. Dependency Inversion Principle (DIP)
All core services have been decoupled into explicit service contracts (interfaces) and registered into the `Splat` DI container:
* `IDataManager` (`SS14.Launcher.Models.Data.IDataManager`) — abstraction for config storage, CVars, favorites, history, and privacy policies.
* `IUpdater` (`SS14.Launcher.Models.IUpdater`) — contract for game engine updates and content bundle installation.
* `ILoginManager` (`SS14.Launcher.Models.Logins.ILoginManager`) — contract for account management, token refresh, and authentication status.
* `IEngineManager` (`SS14.Launcher.Models.EngineManager.IEngineManager`) — contract for managing native Robust Toolbox engine binaries.
* `IThemeService` (`SS14.Launcher.Utility.IThemeService`) — contract for dynamic palette switching, custom wallpaper shaders, and Avalonia themes.

### 2.2. Expansion of Test Coverage to Critical Components
* Implemented dedicated unit test suites for `DataManager` (10 tests), `LocalizationManager` (5 tests), `ServerListFiltersViewModel` (6 tests), `LauncherCommands` (2 tests), and `LauncherDiagnostics` (1 test).
* Expanded Avalonia Headless UI test suite (`Avalonia.Headless.NUnit`) to test UserControl layouts (`ConnectingOverlay`, `DungSpinner`, `RandomMessage`, `LauncherUpdatePromptOverlayView`).
* Total test count expanded to **225 tests** with real line coverage elevated to **20.00%** (over 4,050 covered lines).

### 2.3. Code Review Standards and Quality Governance
1. **Automated Quality Gate:** Created GitHub Actions workflow `.github/workflows/code-quality.yml` verifying `dotnet format --verify-no-changes`, zero compiler warnings (`-warnaserror:CS4014`), and full test runs with coverage reporting.
2. **Pull Request & Issue Templates:** Standardized PR checklist (`.github/pull_request_template.md`) enforcing architectural review, 100% localization parity (810/810), and documentation updates.
3. **Code Ownership:** Added `.github/CODEOWNERS` for critical build and architecture directories.

---

## 3. Consequences and Benefits

* **Modularity:** Services can now be mocked in tests without side effects.
* **Reliability:** Automated formatting and static analysis in CI maintain industrial quality standards for future contributors.
* **Clarity:** Distinct architectural boundary between upstream engine runners and custom launcher subsystems.
