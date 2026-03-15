# Changelog

All notable changes to ANcpLua.Analyzers will be documented in this file.

## [Unreleased]

### Added

- AL0107: Orphaned `[TracedTag]` on parameters of methods without `[Traced]` attribute
- AL0108: Redundant `[NoTrace]` on methods that are not interceptable
- AL0109: `[Traced]` on non-interceptable methods (non-partial, private, static non-partial)
- AL0110: `[TracedTag]` on `out`/`ref` parameters (cannot be captured for span attributes)
- Code fixes for AL0107, AL0108, AL0109, AL0110
- Pre-commit documentation sync guard (`.claude/hooks/pre-commit-guard.sh`)
- GitHub Actions workflow to remind about .NET 11 GA adoption on 2026-12-01
- Dependabot ignore rule for .NET 11 SDK previews/RCs

### Fixed

- README.md diagnostic count (102 → 106) and code fix count (38 → 42)
- README.md OpenTelemetry category count (19 → 23)
- README.md rule table missing AL0107-AL0110 rows
- NuGet package description out of sync with actual diagnostic/code fix counts
- Stale `nupkg/` directory (pre-UseArtifactsOutput artifact)

### Removed

- `verify-published-versions.ps1` — superseded by AL0017/AL0018/AL0019 compile-time enforcement
