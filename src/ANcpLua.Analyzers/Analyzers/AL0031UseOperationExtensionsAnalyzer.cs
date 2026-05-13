using RoslynOperationExtensions = Microsoft.CodeAnalysis.Operations.OperationExtensions;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0031: Suggests using operation extensions instead of verbose patterns.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>invocation.TargetMethod.Name == "name"</c> → <c>invocation.IsMethodNamed("type","name")</c></item>
///         <item>
///             <c>operation.ConstantValue.HasValue &amp;&amp; operation.ConstantValue.Value is T</c> →
///             <c>operation.TryGetConstantValue&lt;T&gt;(out value)</c>
///         </item>
///     </list>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0031UseOperationExtensionsAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL0031.</summary>
    public const string DiagnosticId = "AL0031";
    /// <summary>Diagnostic property key for the proven containing type.</summary>
    public const string PropertyContainingType = "ContainingType";

    private const string IInvocationOperationTypeName = "Microsoft.CodeAnalysis.Operations.IInvocationOperation";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverity.Info);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers syntax or operation actions for analysis.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        if (context.Compilation.GetTypeByMetadataName(IInvocationOperationTypeName) is null) {
            return;
        }

        context.RegisterOperationAction(AnalyzeBinaryOperator, OperationKind.BinaryOperator);
        context.RegisterSyntaxNodeAction(AnalyzeBinarySyntax, SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression);
    }

    private static void AnalyzeBinaryOperator(OperationAnalysisContext context) {
        if (context.Operation is not IBinaryOperation binary) {
            return;
        }

        if (binary.OperatorKind == BinaryOperatorKind.ConditionalAnd && IsConstantValueHasValueCheck(binary)) {
            context.ReportDiagnostic(Diagnostic.Create(
                s_rule,
                binary.Syntax.GetLocation(),
                "operation.TryGetConstantValue<T>(out value)",
                "ConstantValue.HasValue check"));
        }
    }

    private static void AnalyzeBinarySyntax(SyntaxNodeAnalysisContext context) {
        if (context.Node is not BinaryExpressionSyntax binary ||
            !TryGetTargetMethodNameComparisonSyntax(binary, out var methodName, out var targetMethodAccess) ||
            !TryGetContainingTypeFromSiblingSyntax(binary, targetMethodAccess, out var containingType)) {
            return;
        }

        var suggestion = binary.IsKind(SyntaxKind.EqualsExpression)
            ? $"invocation.IsMethodNamed(\"{containingType}\", \"{methodName}\")"
            : $"!invocation.IsMethodNamed(\"{containingType}\", \"{methodName}\")";
        var properties = ImmutableDictionary.CreateBuilder<string, string?>();
        properties.Add(PropertyContainingType, containingType);
        context.ReportDiagnostic(Diagnostic.Create(
            s_rule,
            binary.GetLocation(),
            properties.ToImmutable(),
            suggestion,
            "TargetMethod.Name == comparison"));
    }

    private static bool TryGetTargetMethodNameComparisonSyntax(
        BinaryExpressionSyntax binary,
        [NotNullWhen(true)] out string? methodName,
        [NotNullWhen(true)] out MemberAccessExpressionSyntax? targetMethodAccess) {
        methodName = null;
        targetMethodAccess = null;

        var (memberAccess, literal) = GetMemberAccessAndStringLiteral(binary);
        if (memberAccess is not {
                Name.Identifier.Text: "Name",
                Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "TargetMethod" } targetMethod
            }) {
            return false;
        }

        methodName = literal.Token.ValueText;
        targetMethodAccess = targetMethod;
        return true;
    }

    private static bool TryGetContainingTypeFromSiblingSyntax(
        SyntaxNode currentSyntax,
        SyntaxNode targetMethodSyntax,
        [NotNullWhen(true)] out string? containingType) {
        containingType = null;

        if (currentSyntax is not ExpressionSyntax currentExpression) {
            return false;
        }

        SyntaxNode currentNode = currentExpression;
        while (currentNode.Parent is { } parentNode) {
            if (parentNode is ParenthesizedExpressionSyntax parenthesized) {
                currentNode = parenthesized;
                continue;
            }

            if (parentNode is not BinaryExpressionSyntax parent ||
                !parent.IsKind(SyntaxKind.LogicalAndExpression)) {
                break;
            }

            var sibling = Contains(parent.Left, currentNode)
                ? parent.Right
                : Contains(parent.Right, currentNode)
                    ? parent.Left
                    : null;

            if (sibling is not null &&
                TryGetContainingTypeFromSyntax(sibling, targetMethodSyntax, out containingType)) {
                return true;
            }

            currentNode = parent;
        }

        return false;
    }

    private static bool Contains(SyntaxNode node, SyntaxNode child) =>
        node.SpanStart <= child.SpanStart && node.Span.End >= child.Span.End;

    private static bool TryGetContainingTypeFromSyntax(
        ExpressionSyntax expression,
        SyntaxNode targetMethodSyntax,
        [NotNullWhen(true)] out string? containingType) {
        containingType = null;

        while (expression is ParenthesizedExpressionSyntax parenthesized) {
            expression = parenthesized.Expression;
        }

        if (expression is not BinaryExpressionSyntax binary ||
            !binary.IsKind(SyntaxKind.EqualsExpression)) {
            return false;
        }

        var (memberAccess, literal) = GetMemberAccessAndStringLiteral(binary);
        if (memberAccess is not {
                Name.Identifier.Text: "Name" or "MetadataName",
                Expression: MemberAccessExpressionSyntax {
                    Name.Identifier.Text: "ContainingType",
                    Expression: var candidateTargetMethod
                }
            } ||
            candidateTargetMethod.ToString() != targetMethodSyntax.ToString()) {
            return false;
        }

        containingType = literal.Token.ValueText;
        return true;
    }

    private static (MemberAccessExpressionSyntax? memberAccess, LiteralExpressionSyntax literal)
        GetMemberAccessAndStringLiteral(BinaryExpressionSyntax binary) {
        if (binary.Left is MemberAccessExpressionSyntax leftMember &&
            binary.Right is LiteralExpressionSyntax rightLiteral &&
            rightLiteral.IsKind(SyntaxKind.StringLiteralExpression)) {
            return (leftMember, rightLiteral);
        }

        if (binary.Right is MemberAccessExpressionSyntax rightMember &&
            binary.Left is LiteralExpressionSyntax leftLiteral &&
            leftLiteral.IsKind(SyntaxKind.StringLiteralExpression)) {
            return (rightMember, leftLiteral);
        }

        return (null, SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression));
    }

    private static bool IsConstantValueHasValueCheck(IBinaryOperation binary) {
        var hasValueSources = new HashSet<string>(StringComparer.Ordinal);
        var valueAccessSources = new HashSet<string>(StringComparer.Ordinal);

        foreach (var descendant in RoslynOperationExtensions.Descendants(binary)) {
            if (descendant is not IPropertyReferenceOperation propRef) {
                continue;
            }

            if (!TryGetConstantValueSource(propRef, out var source)) {
                continue;
            }

            switch (propRef.Property.Name) {
                case "HasValue":
                    hasValueSources.Add(source);
                    break;
                case "Value":
                    valueAccessSources.Add(source);
                    break;
            }
        }

        // .HasValue alone is a valid pattern; only suggest TryGetConstantValue when both are accessed
        hasValueSources.IntersectWith(valueAccessSources);
        return hasValueSources.Count > 0;
    }

    private static bool TryGetConstantValueSource(
        IPropertyReferenceOperation propRef,
        [NotNullWhen(true)] out string? source) {
        source = null;

        if (propRef.Instance?.UnwrapAllConversions() is not IPropertyReferenceOperation {
                Property.Name: "ConstantValue",
                Instance.Syntax: { } sourceSyntax
            }) {
            return false;
        }

        source = sourceSyntax.ToString();
        return true;
    }
}
