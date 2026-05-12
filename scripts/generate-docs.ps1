#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Regenerates docs/*.md from the AL00xx scenario classes in src/ANcpLua.Analyzers.AnalyzerDocs.

.DESCRIPTION
    Mirrors the FluentAssertions.Analyzers scripts/generate-docs.ps1 flow:
    push into the scenarios project directory (so the generator's CWD-relative
    Path.Combine(CWD, '<File>.cs') resolves) and invoke `dotnet run -- generate`.

    With -ValidateNoChanges, fails (exit 1) if regeneration produces any diff under docs/.
    CI runs this with -ValidateNoChanges to gate stale-docs PRs.

.PARAMETER ValidateNoChanges
    After regenerating, run `git diff -- docs` and throw if it has any output.

.EXAMPLE
    pwsh ./scripts/generate-docs.ps1                      # locally regenerate
    pwsh ./scripts/generate-docs.ps1 -ValidateNoChanges   # CI gate
#>

param (
    [switch]$ValidateNoChanges
)

$ErrorActionPreference = 'Stop'

function Generate-Docs {
    param ([string]$Project)

    Push-Location src
    try {
        Push-Location $Project
        try {
            dotnet run -c Release -- generate
            if ($LASTEXITCODE -ne 0) { throw "Docs generation failed for $Project" }
        }
        finally { Pop-Location }
    }
    finally { Pop-Location }
}

Generate-Docs -Project ANcpLua.Analyzers.AnalyzerDocs

if ($ValidateNoChanges) {
    $changed = git status --porcelain=v1 -- docs
    if ($changed) {
        $diff = git diff -- docs    # filter out CRLF-only churn
        if ($diff) {
            git --no-pager diff -- docs
            throw "docs/ is stale — re-run scripts/generate-docs.ps1 locally and commit the result."
        }
    }
}
