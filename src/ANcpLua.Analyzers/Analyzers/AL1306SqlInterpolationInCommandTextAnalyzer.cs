
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL1306: Flags interpolated strings assigned to CommandText — SQL injection risk.
/// </summary>
/// <remarks>
///     <para>
///         Assigning an interpolated string (<c>$"..."</c> or <c>$"""..."""</c>) to a property
///         named <c>CommandText</c> is a SQL injection vector. Values should be passed via
///         parameterized queries (<c>@param</c>, <c>$1</c>) instead of being interpolated
///         directly into the command string.
///     </para>
///     <para>
///         This analyzer detects simple assignment expressions where the left-hand side is a
///         member access ending in <c>CommandText</c> and the right-hand side is an
///         <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.InterpolatedStringExpressionSyntax"/>.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al1306SqlInterpolationInCommandTextAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL1306.</summary>
    private const string DiagnosticId = "AL1306";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Reliability,
        DiagnosticSeverity.Warning);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers a syntax node action on simple assignment expressions.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.SimpleAssignmentExpression);

    private static void AnalyzeAssignment(SyntaxNodeAnalysisContext context) {
        var assignment = (AssignmentExpressionSyntax)context.Node;

        // Left side must be a member access ending in CommandText
        if (assignment.Left is not MemberAccessExpressionSyntax memberAccess ||
            !string.Equals(memberAccess.Name.Identifier.Text, "CommandText", StringComparison.Ordinal)) {
            return;
        }

        var propertySymbol = context.SemanticModel.GetSymbolInfo(memberAccess).Symbol as IPropertySymbol;
        if (propertySymbol is null || !IsDbCommandLike(propertySymbol.ContainingType)) {
            return;
        }

        // Right side must be an interpolated string (covers both $"..." and $"""...""")
        if (assignment.Right is not InterpolatedStringExpressionSyntax interpolatedString) {
            return;
        }

        // Only report when at least one interpolation hole is non-constant.
        if (!ContainsNonConstantHole(interpolatedString, context.SemanticModel)) {
            return;
        }

        context.ReportDiagnostic(s_rule, interpolatedString.GetLocation());
    }

    private static bool IsDbCommandLike(ITypeSymbol containingType) {
        for (var current = containingType; current is not null; current = current.BaseType) {
            if (IsDbCommandNamedType(current) || ImplementsDbCommandInterface(current)) {
                return true;
            }
        }

        return false;
    }

    private static bool IsDbCommandNamedType(ITypeSymbol type) =>
        type is INamedTypeSymbol { Name: "DbCommand", ContainingNamespace.Name: "Common", ContainingNamespace.ContainingNamespace.Name: "Data" }
            && type.ContainingNamespace.ContainingNamespace.ContainingNamespace?.Name is "System";

    private static bool ImplementsDbCommandInterface(ITypeSymbol type) {
        foreach (var interfaceType in type.AllInterfaces) {
            if (interfaceType is INamedTypeSymbol { Name: "IDbCommand", ContainingNamespace.Name: "Data" } &&
                interfaceType.ContainingNamespace.ContainingNamespace?.Name is "System") {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsNonConstantHole(InterpolatedStringExpressionSyntax interpolatedString, SemanticModel semanticModel) {
        foreach (var content in interpolatedString.Contents) {
            if (content is not InterpolationSyntax interpolation) {
                continue;
            }

            if (interpolation.Expression is null) {
                continue;
            }

            if (!semanticModel.GetConstantValue(interpolation.Expression).HasValue) {
                return true;
            }
        }

        return false;
    }
}
