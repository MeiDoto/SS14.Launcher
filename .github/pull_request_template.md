## Description

<!-- Briefly describe what this PR does and why -->

## Changes

<!-- List the specific changes made -->
- 

## Testing

<!-- How was this tested? -->
- [ ] Unit tests pass (`dotnet test`)
- [ ] Build succeeds on all platforms
- [ ] Code formatting verified (`dotnet format --verify-no-changes`)

## Checklist

- [ ] No empty `catch {}` blocks — all exceptions are logged
- [ ] No `async void` outside UI event handlers
- [ ] Fire-and-forget tasks use `_ = Method()` discard pattern
- [ ] New public APIs have XML documentation
- [ ] Localization keys added for user-facing strings
