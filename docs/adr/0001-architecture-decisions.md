# Architecture Decision Record (ADR) 0001: Core Architecture & Performance Design

## Status
Accepted

## Context
The Space Station 14 launcher serves as the primary entry point for players across Linux, Windows, and macOS. To provide instant response times, fluid UI transitions, zero stutter, and seamless multi-server browsing, the architecture requires distinct separation of concerns and high-performance algorithmic primitives.

## Decisions

### 1. MVVM Pattern with Avalonia UI
- **Decision**: Use Avalonia UI (v11) with ReactiveUI / MVVM toolkit.
- **Rationale**: Provides cross-platform rendering (Skia backend on Linux/macOS, Direct3D/ANGLE on Windows) and strong declarative data binding.

### 2. High-Performance Modular Algorithms (`Utility.Algorithms`)
- **Decision**: Decompose all numerical and search algorithms into modular sub-namespaces:
  - `Filters`: 1D Kalman filter with Chi-squared gating for latency spike rejection and jitter estimation.
  - `Statistics`: Welford incremental sample statistics, P-Square quantile estimation, Holt linear trend forecasting.
  - `Search`: SIMD-accelerated ASCII lowercasing (`Vector256` / `Vector128`), Myers bit-parallel edit distance, FastServerSearchIndex.
  - `Caching`: Hybrid LRU + TTL cache (`HybridMemoryCache`).
  - `RateLimiting`: Token bucket rate limiter for hub query throttling.
  - `Network`: Exponentially smoothed throughput ETA estimator.
- **Rationale**: Prevents bloated God-objects, enables direct unit testing with zero mock dependencies, and guarantees zero-allocation on hot search paths.

### 3. Asynchronous Error Safety & Logging
- **Decision**: Disallow non-event-handler `async void`. Require all background services and models to return `Task`. Enforce structured logging with Serilog in all catch blocks.
- **Rationale**: Prevents unhandled task exceptions from killing the application process unexpectedly and provides clear diagnostics.

### 4. Process Isolation & Execution
- **Decision**: Delegate game process spawning, priority escalation, and pipe log streaming to `GameProcessRunner`.
- **Rationale**: Completely isolates game engine execution from launcher state management, enabling clean crash reporting and output capture.
