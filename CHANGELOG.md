# Changelog

All notable changes to ANcpLua.Analyzers will be documented in this file.

## [1.20.2] - 2026-03-20

### Fixed

- AL0032: Only fire when `OrEmpty()` extension method is available in the compilation
- AL0033: Only fire when `ToImmutableArrayOrEmpty()` extension method is available in the compilation

## [Unreleased]

### Added

- GitHub Actions workflow to remind about .NET 11 GA adoption on 2026-12-01
- Dependabot ignore rule for .NET 11 SDK previews/RCs

### Fixed

- NuGet package description out of sync with actual diagnostic/code fix counts
- Stale `nupkg/` directory (pre-UseArtifactsOutput artifact)

### Removed

- Telemetry-adjacent diagnostics and code fixes now homed in `ANcpLua.OpenTelemetry.SemanticConventions.Analyzers`: AL0013, AL0061, AL0063, AL0064, AL0065, AL0066, AL0067, AL0068, AL0069, AL0070, AL0071, AL0072, AL0073, AL0074, AL0075, AL0076, AL0077, AL0078, AL0079, AL0083, AL0085, AL0086, AL0088, AL0089, AL0090, AL0091, AL0092, AL0093, AL0096, AL0107, AL0108, AL0109, AL0110, AL0113, AL0124, AL0128, AL0129, AL0130, AL0131, AL0135
- `verify-published-versions.ps1` — superseded by AL0017/AL0018/AL0019 compile-time enforcement
