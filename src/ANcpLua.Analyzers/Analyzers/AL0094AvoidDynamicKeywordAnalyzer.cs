using ANcpLua.Analyzers.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0094: Avoid 'dynamic' keyword in AOT-published code.
/// </summary>
/// <remarks>
///     <para>
///         The 'dynamic' keyword requires System.Reflection.Emit at runtime to generate call sites.
///         Native AOT does not support System.Reflection.Emit, so any usage of 'dynamic' will fail
///         at runtime in AOT-published applications.
///     </para>
///     <para>
///         This analyzer detects dynamic member references, dynamic invocations, dynamic object creation,
///         and dynamic indexer access operations. These are not covered by the built-in IL2XXX/IL3XXX
///         analyzers because the built-in analyzers focus on reflection and code generation APIs,
///         not the C# 'dynamic' keyword specifically.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0094AvoidDynamicKeywordAnalyzer : AlAnalyzer {
    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticIds.AvoidDynamicKeyword,
        DiagnosticCategories.AotTesting,
        DiagnosticSeverities.Suggestion);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers operation actions to detect dynamic keyword usage.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(
            AnalyzeOperation,
            OperationKind.DynamicMemberReference,
            OperationKind.DynamicInvocation,
            OperationKind.DynamicObjectCreation,
            OperationKind.DynamicIndexerAccess);

    private static void AnalyzeOperation(OperationAnalysisContext context) {
        var operation = context.Operation;

        var description = operation.Kind switch {
            OperationKind.DynamicMemberReference => "dynamic member reference",
            OperationKind.DynamicInvocation => "dynamic invocation",
            OperationKind.DynamicObjectCreation => "dynamic object creation",
            OperationKind.DynamicIndexerAccess => "dynamic indexer access",
            _ => "dynamic"
        };

        context.ReportDiagnostic(Diagnostic.Create(Rule, operation.Syntax.GetLocation(), description));
    }
}
