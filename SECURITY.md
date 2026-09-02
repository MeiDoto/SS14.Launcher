# Security Policy

## Supported Versions

| Version | Supported          |
|---------|--------------------|
| 1.2.x   | ✅ Active support  |
| 1.1.x   | ⚠️ Security fixes only |
| < 1.1   | ❌ End of life     |

## Reporting a Vulnerability

If you discover a security vulnerability in SS14.Launcher, please report it responsibly:

1. **DO NOT** open a public GitHub issue for security vulnerabilities.
2. Send an email to the project maintainers or use GitHub's private vulnerability reporting feature.
3. Include:
   - A description of the vulnerability
   - Steps to reproduce
   - Impact assessment
   - Suggested fix (if any)

We will acknowledge receipt within 48 hours and provide a fix timeline within 7 days.

## Security Measures

### Input Validation
- All `ss14://` and `ss14s://` URIs are validated through `UriHelper.TryParseSs14Uri()` which rejects:
  - Control characters (`\r`, `\n`, `\0`)
  - Shell injection characters (`;`, `&`, `|`, `` ` ``, `$`, `"`, `'`)
  - Non-ss14 URI schemes (http, ftp, etc.)

### Zip Traversal Protection (ZipSlip)
- All ZIP archive extraction (replays, content bundles) validates that extracted paths do not escape the target directory via path traversal (`../`).

### Process Isolation
- Game processes are launched via `GameProcessRunner` with controlled environment variables.
- Process priority elevation is gated behind explicit user opt-in CVars.

### Token & Credential Security
- Authentication tokens are stored in SQLite WAL with filesystem permissions.
- No credentials are logged or included in error reports.

### Network Security
- `ss14s://` connections use HTTPS for all API calls.
- Hub queries use rate limiting (`TokenBucket`) to prevent accidental DoS.
- Happy Eyeballs (RFC 8305) implementation for robust dual-stack connectivity.

### Supply Chain
- Dependencies are reviewed via GitHub's `dependency-review-action` on every PR.
- CodeQL static analysis runs weekly and on every PR (`security-extended` + `security-and-quality` query suites).
