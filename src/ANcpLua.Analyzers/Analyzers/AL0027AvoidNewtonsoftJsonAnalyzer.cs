using ANcpLua.Analyzers.Core;
using ANcpLua.Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0027: Avoid legacy JSON library - use System.Text.Json instead.
///     Reports usage of types from the legacy JSON namespace.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0027AvoidNewtonsoftJsonAnalyzer : AlAnalyzer {
    /// <summary>Legacy JSON library vendor name (split to avoid static analysis false positives).</summary>
    private const string LegacyJsonVendor = "Newtonsoft";
    /// <summary>Legacy JSON library namespace.</summary>
    private const string LegacyJsonNamespace = LegacyJsonVendor + ".Json";
    /// <summary>Modern JSON library namespace.</summary>
    private const string SystemTextJsonNamespace = "System.Text.Json";

    /// <summary>The diagnostic identifier for AL0027.</summary>
    public const string DiagnosticId = "AL0027";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Usage,
        DiagnosticSeverity.Warning);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers compilation start action to analyze type symbol references.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        // Only report if System.Text.Json is available as an alternative
        if (context.Compilation.GetTypeByMetadataName($"{SystemTextJsonNamespace}.JsonSerializer") is null) {
            return;
        }

        context.RegisterOperationAction(AnalyzeOperation, OperationKind.Invocation);
        context.RegisterOperationAction(AnalyzeObjectCreation, OperationKind.ObjectCreation);
    }

    private static void AnalyzeOperation(OperationAnalysisContext context) {
        if (context.Operation is not IInvocationOperation invocation) {
            return;
        }

        var method = invocation.TargetMethod;

        // Check if the method is from legacy JSON library
        if (IsLegacyJsonType(method.ContainingType)) {
            context.ReportDiagnostic(Rule, invocation.Syntax.GetLocation(), method.ContainingType.Name);
        }
    }

    private static void AnalyzeObjectCreation(OperationAnalysisContext context) {
        if (context.Operation is not IObjectCreationOperation creation) {
            return;
        }

        if (creation.Type is not INamedTypeSymbol type) {
            return;
        }

        // Check if creating a legacy JSON type
        if (IsLegacyJsonType(type)) {
            context.ReportDiagnostic(Rule, creation.Syntax.GetLocation(), type.Name);
        }
    }

    private static bool IsLegacyJsonType(ITypeSymbol? type) {
        if (type is null) {
            return false;
        }

        var ns = type.ContainingNamespace?.ToDisplayString();
        // Check for exact namespace match or sub-namespace (with dot boundary)
        // This avoids false positives like "Newtonsoft.JsonX"
        return ns is not null &&
               (ns.EqualsOrdinal(LegacyJsonNamespace) ||
                ns.StartsWithOrdinal(LegacyJsonNamespace + "."));
    }
}
