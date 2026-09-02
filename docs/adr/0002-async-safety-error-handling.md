# Architecture Decision Record 0002: Async Safety & Error Handling Policy

## Status
Accepted

## Context
The SS14.Launcher is a GUI desktop application built on Avalonia UI with ReactiveUI. It performs significant background I/O: HTTP requests to hub servers, TCP latency probes, file system operations (replay loading, content management), and process management. Uncaught exceptions in `async void` methods crash the application without recourse, and silent `catch {}` blocks hide bugs and network failures from diagnostics.

## Decision

### 1. No `async void` Outside Event Handlers
- All asynchronous business logic methods MUST return `Task` or `Task<T>`.
- `async void` is permitted ONLY for Avalonia UI event handlers that require the signature (e.g., `OnClick`).
- Fire-and-forget calls MUST use explicit task discards: `_ = DoWorkAsync();`

### 2. Structured Exception Logging
- Empty `catch { }` blocks are PROHIBITED.
- All catch blocks MUST log the exception using Serilog:
  - `Log.Debug(ex, "context")` — for expected/recoverable errors (DNS resolution, socket timeout)
  - `Log.Warning(ex, "context")` — for unexpected but non-fatal errors
  - `Log.Error(ex, "context")` — for errors that degrade functionality

### 3. CancellationToken Propagation
- All long-running async methods SHOULD accept a `CancellationToken` parameter.
- Network I/O methods MUST respect cancellation to enable clean shutdown.
- `CancellationTokenSource` with timeout SHOULD be used for network operations.

### 4. Task Discard Pattern
When a `Task`-returning method is intentionally called without `await` (fire-and-forget), use the discard pattern to suppress CS4014:

```csharp
// Explicit: we know this runs in the background
_ = RefreshServerListAsync(cancellationToken);
```

This makes the intent clear and enables the CS4014 warning to catch genuine mistakes.

## Consequences
- Application stability: unhandled task exceptions no longer crash the process.
- Diagnostics: all errors are logged with context, enabling post-mortem debugging.
- Code clarity: fire-and-forget intent is explicit via `_ =` discards.
