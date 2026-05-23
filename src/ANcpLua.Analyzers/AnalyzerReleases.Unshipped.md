; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
AL0001 | Design | Error | Al0001ProhibitPrimaryConstructorParameterReassignmentAnalyzer
AL0002 | Design | Warning | Al0002DontRepeatNegatedPatternAnalyzer
AL0003 | Reliability | Error | Al0003DontDivideByConstantZeroAnalyzer
AL0004 | Usage | Warning | Al0004ToAl0005SpanComparisonAnalyzer
AL0005 | Usage | Warning | Al0004ToAl0005SpanComparisonAnalyzer
AL0006 | Design | Warning | Al0006FieldNameConflictWithPrimaryConstructorAnalyzer
AL0007 | Usage | Error | Al0007ToAl0009IXmlSerializableAnalyzer
AL0008 | Usage | Error | Al0007ToAl0009IXmlSerializableAnalyzer
AL0009 | Usage | Error | Al0007ToAl0009IXmlSerializableAnalyzer
AL0011 | Threading | Warning | Al0011LockKeywordAnalyzer
AL0014 | Style | Warning | Al0014PreferPatternMatchingAnalyzer
AL0015 | Style | Info | Al0015NormalizeNullGuardStyleAnalyzer
AL0016 | Style | Info | Al0016CombineDeclarationWithNullCheckAnalyzer
AL0017 | VersionManagement | Warning | Al0017HardcodedPackageVersionAnalyzer
AL0018 | VersionManagement | Warning | Al0018VersionPropsNotImportedAnalyzer
AL0019 | VersionManagement | Warning | Al0019UndefinedVersionVariableAnalyzer
AL0020 | ASP.NET Core | Error | Al0020ToAl0024FormBindingAnalyzer
AL0021 | ASP.NET Core | Error | Al0020ToAl0024FormBindingAnalyzer
AL0022 | ASP.NET Core | Error | Al0020ToAl0024FormBindingAnalyzer
AL0023 | ASP.NET Core | Error | Al0020ToAl0024FormBindingAnalyzer
AL0024 | ASP.NET Core | Error | Al0020ToAl0024FormBindingAnalyzer
AL0025 | Usage | Warning | Al0025PreferStaticLambdaAnalyzer
AL0026 | Usage | Warning | Al0026AvoidDateTimeNowAnalyzer
AL0027 | Usage | Warning | Al0027AvoidNewtonsoftJsonAnalyzer
AL0028 | Roslyn Utilities | Info | Al0028UseIsEqualToAnalyzer
AL0029 | Roslyn Utilities | Info | Al0029UseHasAttributeAnalyzer
AL0030 | Roslyn Utilities | Info | Al0030UseTypeHierarchyAnalyzer
AL0031 | Roslyn Utilities | Info | Al0031UseOperationExtensionsAnalyzer
AL0032 | Roslyn Utilities | Info | Al0032UseOrEmptyAnalyzer
AL0033 | Roslyn Utilities | Info | Al0033UseToImmutableArrayOrEmptyAnalyzer
AL0034 | Roslyn Utilities | Info | Al0034UseWhereNotNullAnalyzer
AL0035 | Roslyn Utilities | Info | Al0035UseToDisplayStringExtensionsAnalyzer
AL0036 | Roslyn Utilities | Warning | Al0036UseGuardNotNullAnalyzer
AL0037 | Roslyn Utilities | Warning | Al0037UseTryParseExtensionsAnalyzer
AL0039 | Roslyn Utilities | Warning | Al0039UseStringComparisonExtensionsAnalyzer
AL0040 | Roslyn Utilities | Warning | Al0040UseAttributeExtensionsAnalyzer
AL0041 | AOT Testing | Error | Al0041AotTestMustReturnIntAnalyzer
AL0042 | AOT Testing | Warning | Al0042AotTestExitCode100Analyzer
AL0043 | AOT Testing | Warning | Al0043TrimSafeViolationAnalyzer
AL0044 | AOT Testing | Warning | Al0044AotSafeViolationAnalyzer
AL0045 | Roslyn Utilities | Warning | Al0045UseGuardNotNullOrEmptyAnalyzer
AL0046 | Roslyn Utilities | Warning | Al0046UseGuardNotNullOrWhiteSpaceAnalyzer
AL0047 | Roslyn Utilities | Warning | Al0047UseGuardNotZeroAnalyzer
AL0048 | Roslyn Utilities | Warning | Al0048UseGuardNotNegativeAnalyzer
AL0049 | Roslyn Utilities | Warning | Al0049UseGuardPositiveAnalyzer
AL0050 | Roslyn Utilities | Warning | Al0050UseGuardNotEmptyGuidAnalyzer
AL0051 | Roslyn Utilities | Warning | Al0051UseGuardDefinedEnumAnalyzer
AL0052 | AOT Testing | Error | Al0052AotSafeCallsAotUnsafeAnalyzer
AL0053 | AOT Testing | Warning | Al0053UnnecessaryAotUnsafeAnalyzer
AL0054 | VersionManagement | Warning | Al0054ToAl0056DiagnosticsAlignmentAnalyzer
AL0055 | VersionManagement | Warning | Al0054ToAl0056DiagnosticsAlignmentAnalyzer
AL0056 | VersionManagement | Warning | Al0054ToAl0056DiagnosticsAlignmentAnalyzer
AL0057 | Threading | Warning | Al0057ToAl0060ThreadingAnalyzer
AL0058 | Threading | Warning | Al0057ToAl0060ThreadingAnalyzer
AL0059 | Threading | Warning | Al0057ToAl0060ThreadingAnalyzer
AL0060 | Threading | Warning | Al0057ToAl0060ThreadingAnalyzer
AL0080 | ASP.NET Core | Warning | Al0080MissingResilienceConfigurationAnalyzer
AL0081 | ASP.NET Core | Warning | Al0081MissingHealthChecksAnalyzer
AL0082 | Configuration | Info | Al0082ConsiderConnectionStringAnalyzer
AL0084 | ASP.NET Core | Warning | Al0084MissingServiceDiscoveryAnalyzer
AL0094 | AOT Testing | Warning | Al0094AvoidDynamicKeywordAnalyzer
AL0095 | AOT Testing | Warning | Al0095AvoidExpressionCompileAnalyzer
AL0101 | AOT Testing | Warning | Al0101AvoidActivatorCreateInstanceAnalyzer
AL0102 | AOT Testing | Warning | Al0102AvoidTypeGetTypeAnalyzer
AL0103 | Design | Warning | Al0103ClosedTypeHierarchySwitchAnalyzer
AL0104 | Reliability | Warning | Al0104PreferAwaitUsingAnalyzer
AL0105 | Threading | Warning | Al0105AvoidBlockingCallsInAsyncAnalyzer
AL0106 | ASP.NET Core | Warning | Al0106AvoidTaskRunInAspNetCoreAnalyzer
AL0111 | Reliability | Warning | Al0111SqlInterpolationInCommandTextAnalyzer
AL0112 | Reliability | Warning | Al0112FireAndForgetTaskAnalyzer
AL0114 | Reliability | Warning | Al0114PreferTryParseAnalyzer
AL0115 | Reliability | Warning | Al0115EmptyCatchBlockAnalyzer
AL0116 | Reliability | Warning | Al0116ExceptionLeakedInResponseAnalyzer
AL0117 | Usage | Info | Al0117UnnecessaryLinqMaterializationAnalyzer
AL0118 | Reliability | Warning | Al0118ReadModifyWriteWithoutTransactionAnalyzer
AL0119 | Roslyn Utilities | Warning | Al0119SymbolStoredInModelAnalyzer
AL0120 | Roslyn Utilities | Warning | Al0120UseIncrementalGeneratorAnalyzer
AL0121 | Roslyn Utilities | Warning | Al0121NormalizeWhitespaceAnalyzer
AL0122 | Design | Error | Al0122DuckDbTableMustBePartialAnalyzer
AL0123 | Design | Warning | Al0123DuckDbColumnConflictingOrdinalAnalyzer
AL0125 | Roslyn Utilities | Info | Al0125UseStringComparisonAnyExtensionsAnalyzer
AL0126 | Reliability | Info | Al0126CancellationTokenPropagationAnalyzer
AL0127 | VersionManagement | Warning | Al0127OutdatedMafPackageVersionAnalyzer
AL0128 | GenAI | Warning | Al0128DestructiveToolMustRequireApprovalAnalyzer
AL0129 | GenAI | Info | Al0129ToolMustDeclareSideEffectAnalyzer
AL0130 | GenAI | Info | Al0130ToolMustDeclareCapabilityAnalyzer
AL0137 | Roslyn Utilities | Warning | Al0137UseGuardForThrowIfAnalyzer
AL0138 | Reliability | Warning | Al0138UseExplicitMidpointRoundingAnalyzer
AL0139 | Style | Warning | Al0139UseImplicitTypeWhenApparentAnalyzer
