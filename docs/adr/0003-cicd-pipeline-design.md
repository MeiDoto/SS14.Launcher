# Architecture Decision Record 0003: CI/CD Pipeline Design

## Status
Accepted

## Context
The launcher targets Linux (x64), Windows (x64), and macOS (x64 + ARM64). Releases are triggered by git tags (`v*`). The CI pipeline must ensure code quality gates before any code reaches `master` or any release artifact is published.

## Decision

### Build & Test Pipeline (`build-test.yml`)
- **Trigger**: Push to `master`, Pull Requests to `master`
- **Matrix**: `ubuntu-latest`, `windows-latest`, `macos-latest`
- **Steps**:
  1. Checkout with recursive submodules
  2. Setup .NET 10.0
  3. NuGet package caching (keyed by OS + csproj hashes)
  4. `dotnet restore`
  5. `dotnet build` — both Debug and Release configurations
  6. `dotnet format --verify-no-changes` — code style enforcement
  7. `dotnet test` — both Debug and Release configurations
  8. Upload test results on failure (7-day retention)

### Release Pipeline (`publish-release.yml`)
- **Trigger**: Push of `v*` tags
- **Stages**:
  1. **Test Gate**: Full test suite must pass before any build artifacts are created
  2. **Build Matrix**: Per-platform `dotnet publish` with self-contained single-file binaries
  3. **Artifact Creation**: Platform-specific archives (`.tar.gz` for Linux, `.zip` for Windows)
  4. **Checksums**: SHA256 sums for integrity verification
  5. **GitHub Release**: Auto-generated release notes with all artifacts attached

### Security Analysis (`codeql.yml`)
- Weekly scheduled scans + on every PR
- Languages: C#, Python
- Query suites: `security-extended`, `security-and-quality`

### Dependency Review (`dependency-review.yml`)
- Runs on every PR
- Fails on `high` severity vulnerabilities

## Consequences
- Cross-platform regressions are caught before merge
- Release quality is gated by test passage
- Supply chain attacks are mitigated by dependency review
- Code style consistency is enforced automatically
