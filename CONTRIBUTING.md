# 🛠️ Contributing to SS14.Launcher

Thank you for your interest in improving SS14.Launcher! Please follow these standards when contributing code or documentation.

---

## 📋 Code Quality Standards

1. **Async Patterns**:
   - Always return `Task` or `Task<T>` for asynchronous business methods.
   - Do **NOT** introduce `async void` methods unless strictly required for Avalonia UI event handlers.
   - For fire-and-forget background tasks, use explicit discards (`_ = DoWorkAsync();`) and wrap the internal execution with `try/catch` and structured logging (`Log.Error` / `Log.Debug`).

2. **Exception Handling**:
   - Never write empty catch blocks (`catch { }`).
   - Always log caught exceptions using Serilog: `Log.Debug(ex, "...")` or `Log.Warning(ex, "...")`.

3. **Performance & Memory**:
   - For string manipulations and search routines, use `ReadOnlySpan<char>` and SIMD intrinsics where applicable.
   - Use `ArrayPool<byte>.Shared` when buffering I/O streams to minimize GC pressure.
   - Avoid creating new `Exception` objects on hot execution paths; use `ValueResult<T>` instead.

4. **Testing**:
   - All new features and algorithm changes must include corresponding unit tests under `SS14.Launcher.Tests/`.
   - Run `dotnet test` before submitting changes and ensure 100% test pass rate.

5. **Internationalization (i18n)**:
   - All UI text must be defined in `Assets/Locale/en-US/text.ftl` and `Assets/Locale/ru/text.ftl`.
   - Never hardcode user-facing strings in XAML or ViewModels.

---

## 🚀 Pull Request Process

1. Create a feature branch (`git checkout -b feature/my-enhancement`).
2. Implement your changes following the architecture guidelines.
3. Verify the build and tests locally:
   ```bash
   dotnet build -c Release
   dotnet test -c Release
   ```
4. Push your branch and open a Pull Request against `master`.
