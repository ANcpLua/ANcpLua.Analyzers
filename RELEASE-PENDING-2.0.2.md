# Release pending: ANcpLua.Analyzers 2.0.2 → SDK → Paperless

Status snapshot: 2026-06-01. **Delete this file once step 4 is complete.**

This release is paused between "built" and "published". A human or agent picking
this up later: do the steps **in order** — each is gated on the previous.

## What is already done

- **Root-cause fix (AL1200 band).** 16 "use an `ANcpLua.Roslyn.Utilities` helper"
  rules (AL1204–AL1220, including AL1210) were firing on plain consumer code and
  offering code fixes that rewrite to symbols a consumer cannot resolve (the helper
  DLL is on the analyzer load path, not a compile reference). Each analyzer now gates
  on its helper type being present **and** accessible
  (`GetTypeByMetadataName` + `IsSymbolAccessibleWithin`); the code fixes insert
  `using ANcpLua.Roslyn.Utilities;` when missing.
- **On `main`:** commits `76203e1`, `7a2dc1b`, `df94843`. Tag `v2.0.2` pushed (→ `df94843`).
- **Verified locally:** clean Release build (analyzers + docs drift guard) + 779/779 tests.
- **Package built & verified:** `artifacts/ANcpLua.Analyzers.2.0.2.nupkg`
  (both analyzer DLLs + bundled `ANcpLua.Roslyn.Utilities.dll` + editorconfig profiles, stamped 2.0.2).

## Blocker

2.0.2 is **not yet on nuget.org** — the tag-driven `nuget-publish.yml` run could not
complete. Every step below is gated on 2.0.2 being indexed.

## Resume steps (in order)

**1. Publish ANcpLua.Analyzers 2.0.2.** Either re-run the `v2.0.2` "Publish to NuGet"
workflow run, or push the local package:
```bash
cd /Users/ancplua/RiderProjects/ANcpLua.Analyzers
# rebuild the nupkg if artifacts/ was cleaned:
# dotnet pack src/ANcpLua.Analyzers/ANcpLua.Analyzers.csproj -c Release -o artifacts -p:Version=2.0.2 -p:PackageId=ANcpLua.Analyzers
dotnet nuget push artifacts/ANcpLua.Analyzers.2.0.2.nupkg \
  --source https://api.nuget.org/v3/index.json --api-key "$NUGET_API_KEY" --skip-duplicate
```

**2. Confirm 2.0.2 is indexed:**
```bash
curl -s https://api.nuget.org/v3-flatcontainer/ancplua.analyzers/index.json
```

**3. ANcpLua.NET.Sdk — ship the prepped bump (only after step 2):**
- Branch `bump-analyzers-2.0.2` (`5636e69`) already sets `ANcpLuaAnalyzersVersion`
  2.0.1 → 2.0.2 in `src/Build/Common/Version.props`.
- Merge it to `main`, then tag the SDK release (CI stamps the SDK version from the tag and publishes).
- Do **not** do this before step 2 — restoring an unpublished analyzer version fails with `NU1102`.

**4. Paperless — roll the SDK forward (only after step 3's SDK is published):**
- In `global.json`, bump `ANcpLua.NET.Sdk` (+ `.Web`, `.Test`) from `3.4.39` to the new SDK version; build/test.
- Then **drop** the `dotnet_diagnostic.AL1210.severity = none` workaround from PR #37 —
  AL1210 and its 15 siblings are now correctly silent in Paperless on their own.

## Notes
- `v2.0.2` is already tagged/pushed, so step 1 needs no re-tag.
- The SDK branch's commit message documents the `NU1102` gate in-place.
