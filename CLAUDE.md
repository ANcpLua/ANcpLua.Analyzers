# CLAUDE.md — ANcpLua.Analyzers

## MISSION: Migrate to ANcpLua.NET.Sdk

**Current NuGet:** 1.3.0 | **Target SDK:** 1.3.0

### Rules

- ✅ Breaking changes FINE — pre-v1.0
- ✅ DELETE ANcpLua.Analyzers.Package/ — single-csproj pattern
- ✅ DELETE LangVersion/Nullable/ImplicitUsings from Directory.Build.props
- ❌ NO fallbacks, NO duplicate code

### Critical Facts

| Fact                               | Action                                       |
|------------------------------------|----------------------------------------------|
| SDK auto-injects ANcpLua.Analyzers | `PackageId=Dummy` REQUIRED                   |
| SDK provides polyfills             | DELETE any manual polyfills                  |
| Package needs BOTH dlls            | Pack Analyzers.dll AND CodeFixes.dll         |
| Meziantou pattern                  | Single csproj, multi-target, pack ns2.0 only |

### Target ANcpLua.Analyzers.csproj

```xml

<Project Sdk="ANcpLua.NET.Sdk">
    <PropertyGroup>
        <TargetFrameworks>net10.0;netstandard2.0</TargetFrameworks>
        <PackageId>Dummy</PackageId>
        <IncludeBuildOutput>false</IncludeBuildOutput>
        <GenerateDependencyFile>false</GenerateDependencyFile>
        <DevelopmentDependency>true</DevelopmentDependency>
        <NoPackageAnalysis>true</NoPackageAnalysis>
        <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Microsoft.CodeAnalysis.CSharp" PrivateAssets="all"/>
    </ItemGroup>

    <!-- CRITICAL: Pack BOTH assemblies to analyzers folder -->
    <ItemGroup>
        <None Include="$(OutputPath)\netstandard2.0\ANcpLua.Analyzers.dll"
              Pack="true" PackagePath="analyzers/dotnet/cs" Visible="false"/>
        <None Include="$(OutputPath)\netstandard2.0\ANcpLua.Analyzers.CodeFixes.dll"
              Pack="true" PackagePath="analyzers/dotnet/cs" Visible="false"/>
    </ItemGroup>
</Project>
```

### Commands

```bash
dotnet build
dotnet test --project tests/ANcpLua.Analyzers.Tests/ANcpLua.Analyzers.Tests.csproj
dotnet pack --output ./artifacts
unzip -l artifacts/Dummy.*.nupkg | grep analyzers  # Verify BOTH dlls present
```

### GitHub Actions Versions (Dec 2025)

```yaml
- uses: actions/checkout@v6
- uses: actions/setup-dotnet@v5
- uses: actions/upload-artifact@v6
```

### DELETE Checklist

- [ ] `src/ANcpLua.Analyzers.Package/` (entire directory)
- [ ] `<LangVersion>` from Directory.Build.props
- [ ] `<Nullable>` from Directory.Build.props
- [ ] Any polyfill references
- [ ] Solution reference to Package project