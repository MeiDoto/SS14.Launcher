# 🏛️ Architecture Decision Record (ADR) 0001: Core Architecture, Modularity & Performance Primitives

[🇬🇧 Read in English](0001-architecture-decisions.md) | [🇷🇺 Читать на русском](0001-architecture-decisions.ru.md)

- **Status**: Accepted
- **Date**: 2026-09-02 (Updated: 2026-09-08)
- **Scope**: Application Core, MVVM, Algorithm Suite, Process Isolation

---

## 📋 Context & Problem Statement

The **Space Station 14 Launcher** is the daily gateway for thousands of players across Windows, Linux, and macOS. Core architectural requirements include:
1. **Zero UI Stutter**: The GUI must remain fully responsive while polling hundreds of servers, rendering video backgrounds, and decompressing game archives.
2. **Modularity & Zero God-Objects**: Core logic, algorithmic primitives, and presentations must be cleanly separated and independently unit-testable without GUI mocks.
3. **High-Precision Network Telemetry**: Jitter suppression and outlier rejection for server ping measurements.
4. **Isolated Process Execution**: Launching child game processes safely without leaking sensitive host credentials or environment secrets.

---

## 🎯 Decisions

### 1. MVVM Architectural Pattern with Avalonia UI
- **Decision**: Standardize on **Avalonia UI 11** paired with **ReactiveUI** and `DynamicData`.
- **Rationale**:
  - Delivers cross-platform rendering via Skia (Linux/macOS) and Direct3D/ANGLE (Windows).
  - Reactive collections (`IObservableCache<T, K>`, `SourceList<T>`) enable thread-safe background updates without UI thread contention.
  - Strict separation: Views handle markup and animations; ViewModels manage state and commands; Models govern business rules and persistence.

### 2. Algorithmic Modularization (`Utility.Algorithms`)
- **Decision**: Decompose the legacy `AdvancedAlgorithms.cs` monolith into 10 focused classes under `Utility/Algorithms/`:
  - `Filters/KalmanLatencyTracker.cs`: 1D Kalman filter with Chi-squared gating ($p < 0.01$) to suppress transient socket spikes and deliver stable ping metrics.
  - `Search/FastServerSearchIndex.cs`: Concurrent prefix and token index utilizing `ConcurrentDictionary` and static `TokenSeparators`.
  - `Search/SimdStringHelper.cs`: Hardware-accelerated ASCII lowercasing via `Vector256<byte>` and `Vector128<byte>` intrinsics with scalar fallbacks.
  - `Search/StringMetrics.cs`: Bit-parallel Myers edit distance and Jaro-Winkler string similarity.
  - `Caching/HybridMemoryCache.cs`: Thread-safe hybrid cache combining LRU eviction under memory bounds with TTL invalidation.
  - `RateLimiting/TokenBucket.cs`: Token bucket rate limiter preventing hub query exhaustion and accidental IP bans.
  - `Statistics/RunningStatistics.cs`: Incremental $O(1)$ sample mean and variance computation via Welford's algorithm.
  - `Statistics/PSquareQuantileEstimator.cs`: Real-time tracking of p95 and p99 latency without buffer storage via the P-Square algorithm.
  - `Statistics/HoltLinearTrend.cs`: Exponential trend tracking for throughput and latency forecasting.
  - `Network/ThroughputEtaEstimator.cs`: Adaptive ETA calculations for downloads.
- A backward-compatible facade is maintained in `AdvancedAlgorithms.cs` to preserve existing callers.

### 3. ViewModel Decomposition (`DevelopmentTabViewModel`)
- **Decision**: Partition large ViewModels into partial classes:
  - `DevelopmentTabViewModel.cs`: Core CVar properties, reactive bindings.
  - `DevelopmentTabViewModel.Diagnostics.cs`: Algorithmic benchmarks, network probes, and system diagnostics.
  - `DevelopmentTabViewModel.Maintenance.cs`: Content cache purges, engine pruning, log cleanup, and configuration resets.

### 4. Process Isolation via `GameProcessRunner`
- **Decision**: Separate process execution, environment variable scrubbing, and output streaming from `Connector.cs` into `GameProcessRunner.cs`.
- **Rationale**: Provides a single point of process lifecycle control, eliminates token leaks to child processes, and simplifies crash telemetry.

---

## ⚖️ Consequences & Validation

- **Benefits**:
  - Total elimination of god-objects across the codebase.
  - 100% test pass rate across 138 unit tests covering all algorithm modules.
  - Negligible garbage collector pressure during server searching.
  - Significantly improved maintainability for community contributors.
- **Trade-offs**:
  - Increased file count (mitigated by logical folder structure).
