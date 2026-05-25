; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
AL1000 | Design | Error | Al1000ProhibitPrimaryConstructorParameterReassignmentAnalyzer
AL1001 | Design | Warning | Al1001DontRepeatNegatedPatternAnalyzer
AL1002 | Reliability | Error | Al1002DontDivideByConstantZeroAnalyzer
AL1003 | Usage | Warning | Al1003ToAl1004SpanComparisonAnalyzer
AL1004 | Usage | Warning | Al1003ToAl1004SpanComparisonAnalyzer
AL1005 | Design | Warning | Al1005FieldNameConflictWithPrimaryConstructorAnalyzer
AL1006 | Usage | Error | Al1006ToAl1008IXmlSerializableAnalyzer
AL1007 | Usage | Error | Al1006ToAl1008IXmlSerializableAnalyzer
AL1008 | Usage | Error | Al1006ToAl1008IXmlSerializableAnalyzer
AL1009 | Threading | Warning | Al1009LockKeywordAnalyzer
AL1010 | Style | Warning | Al1010PreferPatternMatchingAnalyzer
AL1011 | Style | Info | Al1011NormalizeNullGuardStyleAnalyzer
AL1012 | Style | Info | Al1012CombineDeclarationWithNullCheckAnalyzer
AL1100 | ASP.NET Core | Error | Al1100ToAl1104FormBindingAnalyzer
AL1101 | ASP.NET Core | Error | Al1100ToAl1104FormBindingAnalyzer
AL1102 | ASP.NET Core | Error | Al1100ToAl1104FormBindingAnalyzer
AL1103 | ASP.NET Core | Error | Al1100ToAl1104FormBindingAnalyzer
AL1104 | ASP.NET Core | Error | Al1100ToAl1104FormBindingAnalyzer
AL1105 | ASP.NET Core | Warning | Al1105MissingResilienceConfigurationAnalyzer
AL1106 | ASP.NET Core | Warning | Al1106MissingHealthChecksAnalyzer
AL1107 | Configuration | Info | Al1107ConsiderConnectionStringAnalyzer
AL1108 | ASP.NET Core | Warning | Al1108MissingServiceDiscoveryAnalyzer
AL1109 | ASP.NET Core | Warning | Al1109AvoidTaskRunInAspNetCoreAnalyzer
AL1200 | Roslyn Utilities | Info | Al1200UseIsEqualToAnalyzer
AL1201 | Roslyn Utilities | Info | Al1201UseHasAttributeAnalyzer
AL1202 | Roslyn Utilities | Info | Al1202UseTypeHierarchyAnalyzer
AL1203 | Roslyn Utilities | Info | Al1203UseOperationExtensionsAnalyzer
AL1204 | Roslyn Utilities | Info | Al1204UseOrEmptyAnalyzer
AL1205 | Roslyn Utilities | Info | Al1205UseToImmutableArrayOrEmptyAnalyzer
AL1206 | Roslyn Utilities | Info | Al1206UseWhereNotNullAnalyzer
AL1207 | Roslyn Utilities | Info | Al1207UseToDisplayStringExtensionsAnalyzer
AL1208 | Roslyn Utilities | Warning | Al1208UseGuardNotNullAnalyzer
AL1209 | Roslyn Utilities | Warning | Al1209UseTryParseExtensionsAnalyzer
AL1210 | Roslyn Utilities | Warning | Al1210UseStringComparisonExtensionsAnalyzer
AL1211 | Roslyn Utilities | Warning | Al1211UseAttributeExtensionsAnalyzer
AL1212 | Roslyn Utilities | Warning | Al1212UseGuardNotNullOrEmptyAnalyzer
AL1213 | Roslyn Utilities | Warning | Al1213UseGuardNotNullOrWhiteSpaceAnalyzer
AL1214 | Roslyn Utilities | Warning | Al1214UseGuardNotZeroAnalyzer
AL1215 | Roslyn Utilities | Warning | Al1215UseGuardNotNegativeAnalyzer
AL1216 | Roslyn Utilities | Warning | Al1216UseGuardPositiveAnalyzer
AL1217 | Roslyn Utilities | Warning | Al1217UseGuardNotEmptyGuidAnalyzer
AL1218 | Roslyn Utilities | Warning | Al1218UseGuardDefinedEnumAnalyzer
AL1219 | Roslyn Utilities | Info | Al1219UseStringComparisonAnyExtensionsAnalyzer
AL1220 | Roslyn Utilities | Warning | Al1220UseGuardForThrowIfAnalyzer
AL1300 | Threading | Warning | Al1300ToAl1303ThreadingAnalyzer
AL1301 | Threading | Warning | Al1300ToAl1303ThreadingAnalyzer
AL1302 | Threading | Warning | Al1300ToAl1303ThreadingAnalyzer
AL1303 | Threading | Warning | Al1300ToAl1303ThreadingAnalyzer
AL1304 | Reliability | Warning | Al1304PreferAwaitUsingAnalyzer
AL1305 | Threading | Warning | Al1305AvoidBlockingCallsInAsyncAnalyzer
AL1306 | Reliability | Warning | Al1306SqlInterpolationInCommandTextAnalyzer
AL1307 | Reliability | Warning | Al1307FireAndForgetTaskAnalyzer
AL1308 | Reliability | Warning | Al1308PreferTryParseAnalyzer
AL1309 | Reliability | Warning | Al1309EmptyCatchBlockAnalyzer
AL1310 | Reliability | Warning | Al1310ExceptionLeakedInResponseAnalyzer
AL1311 | Usage | Info | Al1311UnnecessaryLinqMaterializationAnalyzer
AL1312 | Reliability | Warning | Al1312ReadModifyWriteWithoutTransactionAnalyzer
AL1313 | Reliability | Info | Al1313CancellationTokenPropagationAnalyzer
AL1314 | Reliability | Warning | Al1314UseExplicitMidpointRoundingAnalyzer
AL1400 | AOT Testing | Error | Al1400AotTestMustReturnIntAnalyzer
AL1401 | AOT Testing | Warning | Al1401AotTestExitCode100Analyzer
AL1402 | AOT Testing | Warning | Al1402TrimSafeViolationAnalyzer
AL1403 | AOT Testing | Warning | Al1403AotSafeViolationAnalyzer
AL1404 | AOT Testing | Error | Al1404AotSafeCallsAotUnsafeAnalyzer
AL1405 | AOT Testing | Warning | Al1405UnnecessaryAotUnsafeAnalyzer
AL1406 | AOT Testing | Warning | Al1406AvoidDynamicKeywordAnalyzer
AL1407 | AOT Testing | Warning | Al1407AvoidExpressionCompileAnalyzer
AL1408 | AOT Testing | Warning | Al1408AvoidActivatorCreateInstanceAnalyzer
AL1409 | AOT Testing | Warning | Al1409AvoidTypeGetTypeAnalyzer
AL1500 | Design | Warning | Al1500ClosedTypeHierarchySwitchAnalyzer
AL1501 | Roslyn Utilities | Warning | Al1501SymbolStoredInModelAnalyzer
AL1502 | Roslyn Utilities | Warning | Al1502UseIncrementalGeneratorAnalyzer
AL1503 | Roslyn Utilities | Warning | Al1503NormalizeWhitespaceAnalyzer
AL1504 | Design | Error | Al1504DuckDbTableMustBePartialAnalyzer
AL1505 | Design | Warning | Al1505DuckDbColumnConflictingOrdinalAnalyzer
AL1600 | VersionManagement | Warning | Al1600HardcodedPackageVersionAnalyzer
AL1601 | VersionManagement | Warning | Al1601VersionPropsNotImportedAnalyzer
AL1602 | VersionManagement | Warning | Al1602UndefinedVersionVariableAnalyzer
AL1603 | VersionManagement | Warning | Al1603ToAl1605DiagnosticsAlignmentAnalyzer
AL1604 | VersionManagement | Warning | Al1603ToAl1605DiagnosticsAlignmentAnalyzer
AL1605 | VersionManagement | Warning | Al1603ToAl1605DiagnosticsAlignmentAnalyzer
AL1606 | VersionManagement | Warning | Al1606OutdatedMafPackageVersionAnalyzer
AL1700 | Usage | Warning | Al1700PreferStaticLambdaAnalyzer
AL1701 | Usage | Warning | Al1701AvoidDateTimeNowAnalyzer
AL1702 | Usage | Warning | Al1702AvoidNewtonsoftJsonAnalyzer
AL1703 | Style | Warning | Al1703UseImplicitTypeWhenApparentAnalyzer
AL1800 | GenAI | Warning | Al1800DestructiveToolMustRequireApprovalAnalyzer
AL1801 | GenAI | Info | Al1801ToolMustDeclareSideEffectAnalyzer
AL1802 | GenAI | Info | Al1802ToolMustDeclareCapabilityAnalyzer
