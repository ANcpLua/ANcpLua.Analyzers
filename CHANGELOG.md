# Changelog

All notable changes to ANcpLua.Analyzers will be documented in this file.

## [2.0.0] - 2026-05-25

### Changed (BREAKING)

- **Diagnostic ID renumber.** All 89 active diagnostic IDs renumbered into nine 100-wide domain bands (`AL1000..AL1899`) to avoid collisions with sibling analyzer packages (`ANcpLua.AotReflection` AL0097–AL0100, `ANcpLua.ExtensibleEnumMirror` AL0200–AL0202, `ANcpLua.DiscriminatedUnion` AL0300–AL0303). Old `AL0xxx` IDs no longer fire from this assembly. Full old→new mapping in `docs/migration-catalog.md`. Consumers must rewrite any `.editorconfig` / `.globalconfig` / `#pragma warning disable` / `<NoWarn>` references to the new IDs.
- Resource keys in `Resources.resx` and `CodeFixResources.resx` renamed in lockstep (`AL{old}AnalyzerTitle` → `AL{new}AnalyzerTitle`, etc.).
- Help-link URLs follow the new IDs (`https://ancplua.mintlify.app/analyzers/rules/AL1000` etc.).
- Class names follow the new IDs (`Al1000…Analyzer`, `Al1000…CodeFixProvider`, `Al1000…Tests`).

### Removed

- 44 orphan resource-key prefix groups (≈132 keys) in `Resources.resx` and 13 orphan keys in `CodeFixResources.resx` — leftover from the OTel-analyzer fork that has long since migrated to `Qyl.OpenTelemetry.SemanticConventions.Analyzers` (AL0010, AL0012, AL0013, AL0061–AL0079, AL0083, AL0085–AL0093, AL0096, AL0107–AL0110, AL0113, AL0124, AL0131–AL0136). These keys had no consumer-visible behavior; they were emitting raw key names instead of localized strings.
- Stale `.editorconfig` severity blocks for `AL0010/AL0012/AL0013/AL0038/AL0074` (non-existent diagnostics in this repo).
- Dead `AL0096` mentions in `AotContext.cs` and `build/ANcpLua.Analyzers.props` AOT-rule comment.

### Migration

Use the mapping table in `docs/migration-catalog.md` to script-rewrite any `AL\d{4}` reference in consumer projects. Common rewires: `AL0001` → `AL1000`, `AL0026` → `AL1701`, `AL0030` → `AL1202`, `AL0126` → `AL1313`.

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

- Telemetry-adjacent diagnostics and code fixes now homed in `ANcpLua.OpenTelemetry.SemanticConventions.Analyzers`: AL0013, AL0061, AL0063, AL0064, AL0065, AL0066, AL0067, AL0068, AL0069, AL0070, AL0071, AL0072, AL0073, AL0074, AL0075, AL0076, AL0077, AL0078, AL0079, AL0083, AL0085, AL0086, AL0088, AL0089, AL0090, AL0091, AL0092, AL0093, AL0096, AL0107, AL0108, AL0109, AL0110, AL0113, AL0124, AL0131, AL0135
- `verify-published-versions.ps1` — superseded by AL0017/AL0018/AL0019 compile-time enforcement
