# ⚡ Architecture Decision Record (ADR) 0002: Async Safety & Error Handling Policy

[🇬🇧 Read in English](0002-async-safety-error-handling.md) | [🇷🇺 Читать на русском](0002-async-safety-error-handling.ru.md)

- **Status**: Accepted
- **Date**: 2026-09-02 (Updated: 2026-09-08)
- **Scope**: Async Patterns, Exception Trapping, Serilog Logging, Compiler Warning Enforcement

---

## 📋 Context & Problem Statement

As a rich desktop GUI client with extensive network and disk I/O, **SS14.Launcher** orchestrates dozens of concurrent background operations: HTTP hub aggregation, TCP latency probing, zip decompression, and SQLite config commits.

Previously, three classes of defects posed significant reliability risks:
1. **Unhandled exceptions in `async void` methods**: Any uncaught exception thrown from an `async void` method bypasses normal exception handling mechanisms and crashes the entire OS process immediately.
2. **Silent and bare `catch { }` blocks**: Swallowed critical diagnostic details during network timeouts, DNS failures, or file permission rejections.
3. **Compiler Warnings CS4014 (unawaited Tasks)**: Calling task-returning methods without `await` or discard expressions caused unobserved background task leaks.

---

## 🎯 Decisions

### 1. Strict Ban on `async void` Outside UI Event Handlers
- **Rule**: All asynchronous business logic, models, ViewModels, and services **must return `Task` or `Task<T>`**.
- **Exception**: `async void` is allowed **strictly** for Avalonia UI event handler signatures that mandate `void` (e.g., button click handlers `RoutedEventArgs`).
- Any non-UI background method (`LoginManager.Impl`, `HttpSelfTest.RunTests`) has been converted to return `Task` or wrapped in protective `try/catch` scopes.

### 2. Task Discard Pattern (`_ = Task`) & CS4014 Zero-Tolerance
- When deliberately launching background tasks without awaiting completion (fire-and-forget), developers must explicitly use the discard operator:
  ```csharp
  // CORRECT: Explicit intent, CS4014 suppressed
  _ = _dataManager.CommitConfig();
  _ = RefreshServerListAsync(cancellationToken);
  ```
- The build pipeline enforces zero CS4014 compiler warnings across the entire solution.

### 3. Elimination of Bare & Empty Catch Blocks with Structured Logging
- Bare `catch { }` blocks and untyped `catch` statements are strictly prohibited.
- All caught exceptions must be typed `(Exception ex)` and logged through **Serilog**:
  - `Log.Debug(ex, "...")` — for recoverable, expected conditions (transient ping probe timeouts, optional icon-hub 404s).
  - `Log.Warning(ex, "...")` — for abnormal but non-fatal conditions (partial hub synchronization errors).
  - `Log.Error(ex, "...")` — for serious subsystem degradation (database corruption, process launch failures).
- When the exception variable is unused in a safe fallback branch, `catch (Exception)` is used without declaring an unused variable name to prevent CS0168 warnings.

### 4. Global Exception Safety Nets
Multi-tier global exception handlers are established in `Program.cs`:
- `AppDomain.CurrentDomain.UnhandledException`: Flushes file log buffers via `Log.CloseAndFlush()`.
- `TaskScheduler.UnobservedTaskException`: An intelligent classification filter that distinguishes benign socket drops from severe application faults and marks them observed via `SetObserved()`.

---

## ⚖️ Consequences & Validation

- **Results**:
  - **0 CS4014 warnings** and **0 CS0168 warnings** in Release builds.
  - Zero crashes from background network timeouts or socket drops.
  - Comprehensive diagnostic trails available for user troubleshooting.
