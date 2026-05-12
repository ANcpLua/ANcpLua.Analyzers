#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Runs the DocsVerifier syntactic invariants over the AL00xx scenario classes.

.DESCRIPTION
    Lightweight check that complements the byte-identical guard in
    scripts/generate-docs.ps1 -ValidateNoChanges. Catches malformed scenarios
    (missing implementations, etc.) before the generator runs.
#>

$ErrorActionPreference = 'Stop'

function Verify-Docs {
    param ([string]$Project)

    Push-Location src
    try {
        Push-Location $Project
        try {
            dotnet run -c Release -- verify
            if ($LASTEXITCODE -ne 0) { throw "Docs verifier failed for $Project" }
        }
        finally { Pop-Location }
    }
    finally { Pop-Location }
}

Verify-Docs -Project ANcpLua.Analyzers.AnalyzerDocs
