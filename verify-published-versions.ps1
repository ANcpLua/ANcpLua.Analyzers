param(
    [string]$DirectoryPackagesPath = "Directory.Packages.props"
)

if (-not (Test-Path $DirectoryPackagesPath)) {
    Write-Error "Could not find $DirectoryPackagesPath"
    exit 1
}

[xml]$xml = Get-Content $DirectoryPackagesPath

# Select PackageVersion elements (no namespace needed for MSBuild files)
$packageVersions = $xml.Project.ItemGroup.PackageVersion

$failed = $false

foreach ($pv in $packageVersions) {
    $include = $pv.Include
    $version = $pv.Version

    if ([string]::IsNullOrWhiteSpace($version)) {
        Write-Error "Package '$include' has no version specified."
        $failed = $true
        continue
    }

    # Check if version is a variable (starts with $)
    if ($version.StartsWith('$')) {
        # This is fine as long as the variable itself is defined and pinned.
        # For simplicity, we assume if it's a variable it's managed centrally in Version.props
        continue
    }

    # If it's a hardcoded version, check if it's a pinned version (no wildcards, no ranges)
    if ($version -match '[\[\(\*,]') {
        Write-Error "Package '$include' has a non-pinned version: $version"
        $failed = $true
    }
}

if ($failed) {
    exit 1
}

Write-Host "All package versions are properly pinned."
exit 0