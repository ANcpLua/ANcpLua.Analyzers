using ANcpLua.Analyzers.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0073: Validates [Traced] attribute has non-empty ActivitySourceName.
/// </summary>
/// <remarks>
///     <para>
///         The [Traced] attribute requires a valid ActivitySourceName because:
///         <list type="bullet">
///             <item>The source name identifies where spans originate</item>
///             <item>It must match a registered ActivitySource in the tracing pipeline</item>
///             <item>Empty names prevent proper span correlation and filtering</item>
///         </list>
///     </para>
///     <para>
///         Example of correct usage:
///         <code>
///         [Traced("MyApp.Orders")]  // Valid: descriptive source name
///         public class OrderService { }
///
///         [Traced("")]  // Error: empty source name
///         public class BadService { }
///         </code>
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0073TracedActivitySourceNameAnalyzer : AlAnalyzer {
    private const string TracedAttributeFullName = "qyl.ServiceDefaults.Instrumentation.TracedAttribute";

    private static readonly DiagnosticDescriptor Rule = CreateRule(
        DiagnosticIds.TracedActivitySourceNameEmpty,
        DiagnosticCategories.OpenTelemetry,
        DiagnosticSeverities.RequiredFix);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers symbol actions to analyze types and methods with [Traced] attribute.</summary>
    protected override void RegisterActions(AnalysisContext context) {
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context) {
        var namedType = (INamedTypeSymbol)context.Symbol;
        AnalyzeSymbol(context, namedType, namedType.Name);
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context) {
        var method = (IMethodSymbol)context.Symbol;
        AnalyzeSymbol(context, method, method.Name);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context, ISymbol symbol, string symbolName) {
        if (context.Compilation.GetTypeByMetadataName(TracedAttributeFullName) is not { } tracedAttributeType) {
            return;
        }

        foreach (var attribute in symbol.GetAttributes()) {
            if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, tracedAttributeType)) {
                continue;
            }

            // Check if ActivitySourceName is provided via constructor or named argument
            string? activitySourceName = null;

            // Check first constructor argument (ActivitySourceName)
            if (attribute.ConstructorArguments.Length > 0 &&
                attribute.ConstructorArguments[0].Value is string ctorArg) {
                activitySourceName = ctorArg;
            }

            // Check named argument (ActivitySourceName = "...")
            foreach (var namedArg in attribute.NamedArguments) {
                if (namedArg.Key == "ActivitySourceName" && namedArg.Value.Value is string namedValue) {
                    activitySourceName = namedValue;
                    break;
                }
            }

            // Report if ActivitySourceName is empty/whitespace or not provided at all
            if (string.IsNullOrWhiteSpace(activitySourceName)) {
                ReportDiagnostic(context, attribute, symbolName);
            }
        }
    }

    private static void ReportDiagnostic(SymbolAnalysisContext context, AttributeData attribute, string symbolName) {
        var location = attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation()
                       ?? Location.None;

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            location,
            symbolName));
    }
}
