[![NuGet](https://img.shields.io/nuget/v/ANcpLua.Analyzers?label=NuGet&color=0891B2)](https://www.nuget.org/packages/ANcpLua.Analyzers/)
[![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-512BD4)](https://dotnet.microsoft.com/platform/dotnet-standard)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

# ANcpLua.Analyzers

Roslyn analyzers for modern C# patterns, AOT safety, ASP.NET Core, reliability, GenAI tool governance, and ANcpLua ecosystem conventions.

## Installation

```bash
dotnet add package ANcpLua.Analyzers
```

> **Using ANcpLua.NET.Sdk?** This package is auto-injected - no installation needed.

## What you get

- **88 diagnostics** spanning design, reliability, usage, Roslyn utilities, ASP.NET Core, AOT, threading, style, configuration, GenAI, and version management.
- **36 automatic code fixes** for high-confidence transformations.
- **CI-friendly enforcement** through `.editorconfig` severity configuration.

## Rule coverage by category

| Category | Rules |
|:---------|------:|
| Roslyn Utilities | 24 |
| AOT Testing | 10 |
| Reliability | 10 |
| ASP.NET Core | 9 |
| Usage | 9 |
| VersionManagement | 7 |
| Design | 6 |
| Threading | 6 |
| GenAI | 3 |
| Style | 3 |
| Configuration | 1 |

## Full rule catalog

| Rule | Category | Severity | Analyzer |
|:-----|:---------|:--------:|:---------|
| [AL0001](https://ancplua.mintlify.app/analyzers/rules/AL0001) | Design | Error | `Al0001ProhibitPrimaryConstructorParameterReassignmentAnalyzer` |
| [AL0002](https://ancplua.mintlify.app/analyzers/rules/AL0002) | Design | Warning | `Al0002DontRepeatNegatedPatternAnalyzer` |
| [AL0003](https://ancplua.mintlify.app/analyzers/rules/AL0003) | Reliability | Error | `Al0003DontDivideByConstantZeroAnalyzer` |
| [AL0004](https://ancplua.mintlify.app/analyzers/rules/AL0004) | Usage | Warning | `Al0004ToAl0005SpanComparisonAnalyzer` |
| [AL0005](https://ancplua.mintlify.app/analyzers/rules/AL0005) | Usage | Warning | `Al0004ToAl0005SpanComparisonAnalyzer` |
| [AL0006](https://ancplua.mintlify.app/analyzers/rules/AL0006) | Design | Warning | `Al0006FieldNameConflictWithPrimaryConstructorAnalyzer` |
| [AL0007](https://ancplua.mintlify.app/analyzers/rules/AL0007) | Usage | Error | `Al0007ToAl0009IXmlSerializableAnalyzer` |
| [AL0008](https://ancplua.mintlify.app/analyzers/rules/AL0008) | Usage | Error | `Al0007ToAl0009IXmlSerializableAnalyzer` |
| [AL0009](https://ancplua.mintlify.app/analyzers/rules/AL0009) | Usage | Error | `Al0007ToAl0009IXmlSerializableAnalyzer` |
| [AL0011](https://ancplua.mintlify.app/analyzers/rules/AL0011) | Threading | Warning | `Al0011LockKeywordAnalyzer` |
| [AL0014](https://ancplua.mintlify.app/analyzers/rules/AL0014) | Style | Warning | `Al0014PreferPatternMatchingAnalyzer` |
| [AL0015](https://ancplua.mintlify.app/analyzers/rules/AL0015) | Style | Info | `Al0015NormalizeNullGuardStyleAnalyzer` |
| [AL0016](https://ancplua.mintlify.app/analyzers/rules/AL0016) | Style | Info | `Al0016CombineDeclarationWithNullCheckAnalyzer` |
| [AL0017](https://ancplua.mintlify.app/analyzers/rules/AL0017) | VersionManagement | Warning | `Al0017HardcodedPackageVersionAnalyzer` |
| [AL0018](https://ancplua.mintlify.app/analyzers/rules/AL0018) | VersionManagement | Warning | `Al0018VersionPropsNotImportedAnalyzer` |
| [AL0019](https://ancplua.mintlify.app/analyzers/rules/AL0019) | VersionManagement | Warning | `Al0019UndefinedVersionVariableAnalyzer` |
| [AL0020](https://ancplua.mintlify.app/analyzers/rules/AL0020) | ASP.NET Core | Error | `Al0020ToAl0024FormBindingAnalyzer` |
| [AL0021](https://ancplua.mintlify.app/analyzers/rules/AL0021) | ASP.NET Core | Error | `Al0020ToAl0024FormBindingAnalyzer` |
| [AL0022](https://ancplua.mintlify.app/analyzers/rules/AL0022) | ASP.NET Core | Error | `Al0020ToAl0024FormBindingAnalyzer` |
| [AL0023](https://ancplua.mintlify.app/analyzers/rules/AL0023) | ASP.NET Core | Error | `Al0020ToAl0024FormBindingAnalyzer` |
| [AL0024](https://ancplua.mintlify.app/analyzers/rules/AL0024) | ASP.NET Core | Error | `Al0020ToAl0024FormBindingAnalyzer` |
| [AL0025](https://ancplua.mintlify.app/analyzers/rules/AL0025) | Usage | Warning | `Al0025PreferStaticLambdaAnalyzer` |
| [AL0026](https://ancplua.mintlify.app/analyzers/rules/AL0026) | Usage | Warning | `Al0026AvoidDateTimeNowAnalyzer` |
| [AL0027](https://ancplua.mintlify.app/analyzers/rules/AL0027) | Usage | Warning | `Al0027AvoidNewtonsoftJsonAnalyzer` |
| [AL0028](https://ancplua.mintlify.app/analyzers/rules/AL0028) | Roslyn Utilities | Info | `Al0028UseIsEqualToAnalyzer` |
| [AL0029](https://ancplua.mintlify.app/analyzers/rules/AL0029) | Roslyn Utilities | Info | `Al0029UseHasAttributeAnalyzer` |
| [AL0030](https://ancplua.mintlify.app/analyzers/rules/AL0030) | Roslyn Utilities | Info | `Al0030UseTypeHierarchyAnalyzer` |
| [AL0031](https://ancplua.mintlify.app/analyzers/rules/AL0031) | Roslyn Utilities | Info | `Al0031UseOperationExtensionsAnalyzer` |
| [AL0032](https://ancplua.mintlify.app/analyzers/rules/AL0032) | Roslyn Utilities | Info | `Al0032UseOrEmptyAnalyzer` |
| [AL0033](https://ancplua.mintlify.app/analyzers/rules/AL0033) | Roslyn Utilities | Info | `Al0033UseToImmutableArrayOrEmptyAnalyzer` |
| [AL0034](https://ancplua.mintlify.app/analyzers/rules/AL0034) | Roslyn Utilities | Info | `Al0034UseWhereNotNullAnalyzer` |
| [AL0035](https://ancplua.mintlify.app/analyzers/rules/AL0035) | Roslyn Utilities | Info | `Al0035UseToDisplayStringExtensionsAnalyzer` |
| [AL0036](https://ancplua.mintlify.app/analyzers/rules/AL0036) | Roslyn Utilities | Warning | `Al0036UseGuardNotNullAnalyzer` |
| [AL0037](https://ancplua.mintlify.app/analyzers/rules/AL0037) | Roslyn Utilities | Warning | `Al0037UseTryParseExtensionsAnalyzer` |
| [AL0039](https://ancplua.mintlify.app/analyzers/rules/AL0039) | Roslyn Utilities | Warning | `Al0039UseStringComparisonExtensionsAnalyzer` |
| [AL0040](https://ancplua.mintlify.app/analyzers/rules/AL0040) | Roslyn Utilities | Warning | `Al0040UseAttributeExtensionsAnalyzer` |
| [AL0041](https://ancplua.mintlify.app/analyzers/rules/AL0041) | AOT Testing | Error | `Al0041AotTestMustReturnIntAnalyzer` |
| [AL0042](https://ancplua.mintlify.app/analyzers/rules/AL0042) | AOT Testing | Warning | `Al0042AotTestExitCode100Analyzer` |
| [AL0043](https://ancplua.mintlify.app/analyzers/rules/AL0043) | AOT Testing | Warning | `Al0043TrimSafeViolationAnalyzer` |
| [AL0044](https://ancplua.mintlify.app/analyzers/rules/AL0044) | AOT Testing | Warning | `Al0044AotSafeViolationAnalyzer` |
| [AL0045](https://ancplua.mintlify.app/analyzers/rules/AL0045) | Roslyn Utilities | Warning | `Al0045UseGuardNotNullOrEmptyAnalyzer` |
| [AL0046](https://ancplua.mintlify.app/analyzers/rules/AL0046) | Roslyn Utilities | Warning | `Al0046UseGuardNotNullOrWhiteSpaceAnalyzer` |
| [AL0047](https://ancplua.mintlify.app/analyzers/rules/AL0047) | Roslyn Utilities | Warning | `Al0047UseGuardNotZeroAnalyzer` |
| [AL0048](https://ancplua.mintlify.app/analyzers/rules/AL0048) | Roslyn Utilities | Warning | `Al0048UseGuardNotNegativeAnalyzer` |
| [AL0049](https://ancplua.mintlify.app/analyzers/rules/AL0049) | Roslyn Utilities | Warning | `Al0049UseGuardPositiveAnalyzer` |
| [AL0050](https://ancplua.mintlify.app/analyzers/rules/AL0050) | Roslyn Utilities | Warning | `Al0050UseGuardNotEmptyGuidAnalyzer` |
| [AL0051](https://ancplua.mintlify.app/analyzers/rules/AL0051) | Roslyn Utilities | Warning | `Al0051UseGuardDefinedEnumAnalyzer` |
| [AL0052](https://ancplua.mintlify.app/analyzers/rules/AL0052) | AOT Testing | Error | `Al0052AotSafeCallsAotUnsafeAnalyzer` |
| [AL0053](https://ancplua.mintlify.app/analyzers/rules/AL0053) | AOT Testing | Warning | `Al0053UnnecessaryAotUnsafeAnalyzer` |
| [AL0054](https://ancplua.mintlify.app/analyzers/rules/AL0054) | VersionManagement | Warning | `Al0054ToAl0056DiagnosticsAlignmentAnalyzer` |
| [AL0055](https://ancplua.mintlify.app/analyzers/rules/AL0055) | VersionManagement | Warning | `Al0054ToAl0056DiagnosticsAlignmentAnalyzer` |
| [AL0056](https://ancplua.mintlify.app/analyzers/rules/AL0056) | VersionManagement | Warning | `Al0054ToAl0056DiagnosticsAlignmentAnalyzer` |
| [AL0057](https://ancplua.mintlify.app/analyzers/rules/AL0057) | Threading | Warning | `Al0057ToAl0060ThreadingAnalyzer` |
| [AL0058](https://ancplua.mintlify.app/analyzers/rules/AL0058) | Threading | Warning | `Al0057ToAl0060ThreadingAnalyzer` |
| [AL0059](https://ancplua.mintlify.app/analyzers/rules/AL0059) | Threading | Warning | `Al0057ToAl0060ThreadingAnalyzer` |
| [AL0060](https://ancplua.mintlify.app/analyzers/rules/AL0060) | Threading | Warning | `Al0057ToAl0060ThreadingAnalyzer` |
| [AL0080](https://ancplua.mintlify.app/analyzers/rules/AL0080) | ASP.NET Core | Warning | `Al0080MissingResilienceConfigurationAnalyzer` |
| [AL0081](https://ancplua.mintlify.app/analyzers/rules/AL0081) | ASP.NET Core | Warning | `Al0081MissingHealthChecksAnalyzer` |
| [AL0082](https://ancplua.mintlify.app/analyzers/rules/AL0082) | Configuration | Info | `Al0082ConsiderConnectionStringAnalyzer` |
| [AL0084](https://ancplua.mintlify.app/analyzers/rules/AL0084) | ASP.NET Core | Warning | `Al0084MissingServiceDiscoveryAnalyzer` |
| [AL0094](https://ancplua.mintlify.app/analyzers/rules/AL0094) | AOT Testing | Warning | `Al0094AvoidDynamicKeywordAnalyzer` |
| [AL0095](https://ancplua.mintlify.app/analyzers/rules/AL0095) | AOT Testing | Warning | `Al0095AvoidExpressionCompileAnalyzer` |
| [AL0101](https://ancplua.mintlify.app/analyzers/rules/AL0101) | AOT Testing | Warning | `Al0101AvoidActivatorCreateInstanceAnalyzer` |
| [AL0102](https://ancplua.mintlify.app/analyzers/rules/AL0102) | AOT Testing | Warning | `Al0102AvoidTypeGetTypeAnalyzer` |
| [AL0103](https://ancplua.mintlify.app/analyzers/rules/AL0103) | Design | Warning | `Al0103ClosedTypeHierarchySwitchAnalyzer` |
| [AL0104](https://ancplua.mintlify.app/analyzers/rules/AL0104) | Reliability | Warning | `Al0104PreferAwaitUsingAnalyzer` |
| [AL0105](https://ancplua.mintlify.app/analyzers/rules/AL0105) | Threading | Warning | `Al0105AvoidBlockingCallsInAsyncAnalyzer` |
| [AL0106](https://ancplua.mintlify.app/analyzers/rules/AL0106) | ASP.NET Core | Warning | `Al0106AvoidTaskRunInAspNetCoreAnalyzer` |
| [AL0111](https://ancplua.mintlify.app/analyzers/rules/AL0111) | Reliability | Warning | `Al0111SqlInterpolationInCommandTextAnalyzer` |
| [AL0112](https://ancplua.mintlify.app/analyzers/rules/AL0112) | Reliability | Warning | `Al0112FireAndForgetTaskAnalyzer` |
| [AL0114](https://ancplua.mintlify.app/analyzers/rules/AL0114) | Reliability | Warning | `Al0114PreferTryParseAnalyzer` |
| [AL0115](https://ancplua.mintlify.app/analyzers/rules/AL0115) | Reliability | Warning | `Al0115EmptyCatchBlockAnalyzer` |
| [AL0116](https://ancplua.mintlify.app/analyzers/rules/AL0116) | Reliability | Warning | `Al0116ExceptionLeakedInResponseAnalyzer` |
| [AL0117](https://ancplua.mintlify.app/analyzers/rules/AL0117) | Usage | Info | `Al0117UnnecessaryLinqMaterializationAnalyzer` |
| [AL0118](https://ancplua.mintlify.app/analyzers/rules/AL0118) | Reliability | Warning | `Al0118ReadModifyWriteWithoutTransactionAnalyzer` |
| [AL0119](https://ancplua.mintlify.app/analyzers/rules/AL0119) | Roslyn Utilities | Warning | `Al0119SymbolStoredInModelAnalyzer` |
| [AL0120](https://ancplua.mintlify.app/analyzers/rules/AL0120) | Roslyn Utilities | Warning | `Al0120UseIncrementalGeneratorAnalyzer` |
| [AL0121](https://ancplua.mintlify.app/analyzers/rules/AL0121) | Roslyn Utilities | Warning | `Al0121NormalizeWhitespaceAnalyzer` |
| [AL0122](https://ancplua.mintlify.app/analyzers/rules/AL0122) | Design | Error | `Al0122DuckDbTableMustBePartialAnalyzer` |
| [AL0123](https://ancplua.mintlify.app/analyzers/rules/AL0123) | Design | Warning | `Al0123DuckDbColumnConflictingOrdinalAnalyzer` |
| [AL0125](https://ancplua.mintlify.app/analyzers/rules/AL0125) | Roslyn Utilities | Info | `Al0125UseStringComparisonAnyExtensionsAnalyzer` |
| [AL0126](https://ancplua.mintlify.app/analyzers/rules/AL0126) | Reliability | Info | `Al0126CancellationTokenPropagationAnalyzer` |
| [AL0127](https://ancplua.mintlify.app/analyzers/rules/AL0127) | VersionManagement | Warning | `Al0127OutdatedMafPackageVersionAnalyzer` |
| [AL0128](https://ancplua.mintlify.app/analyzers/rules/AL0128) | GenAI | Warning | `Al0128DestructiveToolMustRequireApprovalAnalyzer` |
| [AL0129](https://ancplua.mintlify.app/analyzers/rules/AL0129) | GenAI | Info | `Al0129ToolMustDeclareSideEffectAnalyzer` |
| [AL0130](https://ancplua.mintlify.app/analyzers/rules/AL0130) | GenAI | Info | `Al0130ToolMustDeclareCapabilityAnalyzer` |
| [AL0137](https://ancplua.mintlify.app/analyzers/rules/AL0137) | Roslyn Utilities | Warning | `Al0137UseGuardForThrowIfAnalyzer` |
| [AL0138](https://ancplua.mintlify.app/analyzers/rules/AL0138) | Reliability | Warning | `Al0138UseExplicitMidpointRoundingAnalyzer` |

**Legend:** `Error` = build error, `Warning` = build warning, `Info` = IDE suggestion, `Disabled` = off by default.

## Code fixes

Automatic fixes are currently available for:

AL0002, AL0004, AL0005, AL0008, AL0011, AL0014, AL0015, AL0016, AL0025, AL0026, AL0027, AL0028, AL0029, AL0030, AL0031, AL0032, AL0033, AL0034, AL0035, AL0036, AL0037, AL0039, AL0040, AL0045, AL0046, AL0047, AL0048, AL0049, AL0050, AL0051, AL0103, AL0121, AL0122, AL0126, AL0137, AL0138

## Configuration

```editorconfig
[*.cs]
dotnet_diagnostic.AL0001.severity = error
dotnet_diagnostic.AL0014.severity = none
```

## Development commands

```bash
# Build
dotnet build ANcpLua.Analyzers.slnx -c Release

# Test
dotnet test --project tests/ANcpLua.Analyzers.Tests/ANcpLua.Analyzers.Tests.csproj

# Pack
dotnet pack src/ANcpLua.Analyzers/ANcpLua.Analyzers.csproj -c Release -o artifacts -p:PackageId=ANcpLua.Analyzers
```

## Documentation

- Overview: [ancplua.mintlify.app/analyzers/overview](https://ancplua.mintlify.app/analyzers/overview)
- Rule docs: [ancplua.mintlify.app/analyzers/rules](https://ancplua.mintlify.app/analyzers/rules)

## Related projects

- [ANcpLua.NET.Sdk](https://github.com/ANcpLua/ANcpLua.NET.Sdk) - MSBuild SDK (auto-injects this analyzer package)
- [ANcpLua.Roslyn.Utilities](https://github.com/ANcpLua/ANcpLua.Roslyn.Utilities) - Shared Roslyn helpers and extensions
- [ANcpLua.Agents](https://github.com/ANcpLua/ANcpLua.Agents) - MAF runtime helpers + agent test infrastructure

## License

[MIT](LICENSE)
