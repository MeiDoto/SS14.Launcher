# 📜 Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.2.9] - 2026-09-11

### Security
- **Known: GHSA-xrw6-gwf8-vvr9 (Tmds.DBus.Protocol)**: Documented and suppressed — patched versions (0.92.0+) break Avalonia 11.2.4 API (`Connection` class removed). Risk: local D-Bus peer spoofing (requires local machine access). Will be resolved when Avalonia upgrades its D-Bus dependency.
- **Zip Bomb Protection**: Added entry count limit (50,000) and uncompressed size limit (4 GB) to `ExtractZipToDirectory`, preventing decompression bombs.
- **OpenUri Scheme Allowlist**: `Helpers.OpenUri()` now validates URI schemes against an allowlist (`http`, `https`, `mailto`) before invoking `Process.Start`, blocking `file://`, `ssh://`, `javascript:`, and other dangerous scheme handlers.
- **URI Input Hardening**: Extended `TryParseSs14Uri` character blocklist with `<`, `>`, `\`, `\t`, `\b` to prevent HTML injection, path traversal, and tab-based obfuscation. Added `HostNameType` and port validation.

### Added
- **OpenUri Security Tests** (17 tests): Validates scheme allowlist blocks `file://`, `ssh://`, `telnet://`, `ldap://`, `gopher://`, `javascript:`, `data:` URI schemes.
- **Extended URI Injection Tests** (4 tests): Validates rejection of `<script>`, path traversal via `\`, tab injection vectors.
- **Extended Zip Security Tests** (2 tests): Validates zip bomb entry count protection and deeply nested directory traversal (`../../../../../../`).

### Changed
- **Dependency audit**: All vulnerabilities reviewed; 1 known (`Tmds.DBus.Protocol` GHSA-xrw6-gwf8-vvr9) documented and suppressed due to Avalonia compatibility constraint.
- Test suite expanded from 225 → 248 tests (100% pass rate).

---

## [1.2.8] - 2026-09-09

### Added
- **Headless UI Testing Suite**: Integrated `Avalonia.Headless.NUnit` into automated test suite with full headless rendering tests for UserControls, `AngleBox`, and theme resource dictionaries (195 tests total, 100% pass rate).
- **Code Coverage in CI/CD**: Integrated `coverlet.collector` into test project and GitHub Actions pipelines (`build-test.yml`, `publish-release.yml`) with automated `coverage.cobertura.xml` artifact generation.
- **Dependency Injection**: Registered `IThemeService` into Splat DI locator; decoupled and injected via interface in `MainWindowViewModel`.
- **Theme Resources**: Extracted wallpaper dimming overlay color into `ThemeBackgroundOverlayColor` and `ThemeBackgroundOverlayBrush` dynamic resources.
- **Complete Localization Parity**: Added missing auth-override keys to `ru/text.ftl` (`login-login-auth-server-changed`, `main-window-auth-override-*`) achieving 100% key parity (810/810 keys) enforced by an automated test.

### Fixed
- **Table Zebra Transparency**: Resolved bug where alternating server and replay list rows (`:nth-child(2n)`) remained opaque `#262626` over custom wallpapers and animated backgrounds; dynamic alpha blending applied.
- **Theme Font Resolution**: Corrected relative font URI in `Theme.xaml` to absolute `avares://SS14.Launcher/Assets/Fonts/noto_sans/*.ttf#Noto Sans`, preventing typeface loading errors.
- **Localization Extension Safety**: Made `LocExtension` null-safe against uninitialized service locators.
- **Technical Debt**: Audited and formalized all 13 legacy upstream `// TODO` comments into architectural specifications.

---

## [1.2.7] - 2026-09-09

### Added
- **Architectural Decision Record**: Published ADR 0004 (`0004-replay-subsystem-and-user-architecture`) covering zero-allocation I/O, IPC architecture, and engine isolation.
- **Direct Custom Replay Downloads**: Support for direct HTTP/HTTPS replay archives and customizable URL template patterns with `{roundId}` placeholder.
- **Smart Clipboard Detection**: Intelligent paste button in replay download dialog that automatically parses round IDs or URLs from system clipboard.

### Changed
- **Unified Dialog Design**: Redesigned `DownloadReplayDialog` to match canonical SS14 Launcher design system (vector `AngleBox` buttons, `Card` styles, gold accent).
- **Decoupling**: Completely removed external dependency on `spacestories.club` and friends feature, making the launcher 100% standalone and privacy-focused.

### Fixed
- **Graceful Cancellation**: Window closing (`OnClosing`) during active replay download safely cancels background tasks and cleans up partial `.downloading` files.
- **Linux Shutdown DBus Hotfix**: Resolved unobserved `TaskCanceledException` on Linux shutdown by upgrading `Tmds.DBus.Protocol` and setting X11 platform options.

---

## [1.2.6] - 2026-09-08

### Added
- **AppStream Metainfo**: Added `org.spacestation14.launcher.metainfo.xml` with automatic CI validation.
- **Flatpak & AppImage**: Packaging manifests for native Linux application distribution.
- **Packaging Suite**: Created `build-appimage.sh` and Flatpak build recipes.
- **Diagnostic Telemetry**: Added OS detection, CPU instruction set reporting, and desktop environment metadata.

### Fixed
- **Linux Wayland Icon**: Installed desktop icons to system hicolor and pixmaps paths to ensure window icon displays under Wayland compositors.
- **URI Scheme Mapping**: Corrected handling of `ss14://` and `ss14s://` protocol schemes in favorite server prefetch.

---

## [1.2.5] - 2026-09-08

### Added
- **Storage Manager**: Dialog for inspecting launcher disk usage, cache directories, and engine version installations.
- **Network Diagnostics Tool**: Ping probes, DNS resolution testing, and latency jitter measurement.
- **Replay Inspector**: Advanced metadata inspection tool for Space Station 14 replays.
- **ADR 0001, 0002, 0003**: Documented architecture decisions, async error safety patterns, and CI/CD pipeline design.

### Changed
- **Bilingual Documentation**: Comprehensive Russian and English documentation across all architectural guides.

---

## [1.2.4] - 2026-09-01

### Added
- **Language Selector UI**: Hot-reloading language switcher in options tab.
- **Expanded Test Suite**: Grew automated test suite with unit tests for localization and URI helpers.

---

## [1.2.3] - 2026-09-01

### Added
- **Performance & DEV Controls**: Comprehensive toggles for GC server mode, socket pooling, ping probe intervals, and engine arguments.

---

## [1.2.2] - 2026-09-01

### Added
- **CI/CD Security Workflows**: CodeQL SAST scanning, Dependabot dependency review, conflict labeler, welcome bot, and stale bot.

---

## [1.2.1] - 2026-09-01

### Fixed
- Maintenance bug fixes and engine dependency updates.

---

## [1.2.0] - 2026-08-31

### Added
- **Custom Branding & Visuals**: Support for custom window titles, logos, wallpaper images/animations, and palette coloring.

---

## [1.1.9] - 2026-08-31

### Fixed
- **Network Resilience**: Global handling of Unobserved Task Exceptions during transient connection dropouts.
- **Documentation**: Initial bilingual README overhaul.
