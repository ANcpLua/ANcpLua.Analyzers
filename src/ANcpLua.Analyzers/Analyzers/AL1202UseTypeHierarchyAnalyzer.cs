using MsOperationExtensions = Microsoft.CodeAnalysis.Operations.OperationExtensions;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL1202: Suggests using type hierarchy extensions instead of manual loops.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>
///             <c>foreach (var iface in type.AllInterfaces) if (Equals(iface, target))</c> →
///             <c>type.Implements(target)</c>
///         </item>
///         <item>
///             <c>while (baseType != null) { if (Equals(baseType, target)) ... baseType = baseType.BaseType; }</c> →
///             <c>type.InheritsFrom(target)</c>
///         </item>
///     </list>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al1202UseTypeHierarchyAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL1202.</summary>
    public const string DiagnosticId = "AL1202";

    private enum KnownType { ITypeSymbol, SymbolEqualityComparer }

    private static readonly string[] s_knownTypeNames = [
        "Microsoft.CodeAnalysis.ITypeSymbol",
        "Microsoft.CodeAnalysis.SymbolEqualityComparer"
    ];

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverity.Info);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers compilation start action to analyze type hierarchy iteration patterns.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        var cache = new TypeCache<KnownType>(type => context.Compilation.GetTypeByMetadataName(s_knownTypeNames[(int)type]));

        if (cache.Get(KnownType.ITypeSymbol) is null) {
            return;
        }

        context.RegisterOperationAction(
            ctx => AnalyzeLoop(ctx, cache),
            OperationKind.Loop);
    }

    private static void AnalyzeLoop(OperationAnalysisContext context, TypeCache<KnownType> cache) {
        if (context.Operation is IForEachLoopOperation forEachLoop &&
            forEachLoop.Collection.GetCollectionSourceName() is "AllInterfaces" &&
            forEachLoop.Syntax is StatementSyntax forEachStatement &&
            IsPureForEachInterfaceCheck(forEachLoop, cache) &&
            HasFollowingBooleanFailureReturn(forEachStatement)) {
            context.ReportDiagnostic(s_rule, forEachLoop.Syntax.GetLocation(),
                "type.Implements(interfaceType)", "foreach over AllInterfaces");
            return;
        }

        if (context.Operation is IWhileLoopOperation whileLoop &&
            IsPureBaseTypeInheritanceCheck(whileLoop, cache) &&
            whileLoop.Syntax is WhileStatementSyntax whileStatement &&
            HasBaseTypeLoopLeadingDeclaration(whileStatement) &&
            HasFollowingBooleanFailureReturn(whileStatement)) {
            context.ReportDiagnostic(s_rule, whileLoop.Syntax.GetLocation(),
                "type.InheritsFrom(baseType)", "while loop walking BaseType");
        }
    }

    private static bool HasFollowingBooleanFailureReturn(StatementSyntax loopStatement) {
        if (loopStatement.Parent is not BlockSyntax block) {
            return false;
        }

        var statements = block.Statements;
        var loopIndex = statements.IndexOf(loopStatement);
        if (loopIndex < 0 || loopIndex + 1 >= statements.Count) {
            return false;
        }

        return statements[loopIndex + 1] is ReturnStatementSyntax returnStatement &&
               IsBooleanFailureReturn(returnStatement.Expression);
    }

    private static bool IsBooleanFailureReturn(ExpressionSyntax? expression) =>
        expression is not null && (
            expression is DefaultExpressionSyntax ||
            expression is LiteralExpressionSyntax literalExpression &&
            literalExpression.Kind() is SyntaxKind.FalseLiteralExpression or SyntaxKind.NullLiteralExpression);

    private static bool IsPureForEachInterfaceCheck(IForEachLoopOperation forEachLoop, TypeCache<KnownType> cache) {
        if (forEachLoop.Syntax is not ForEachStatementSyntax forEachStatement) {
            return false;
        }

        var iteratorName = forEachStatement.Identifier.Text;
        if (string.IsNullOrEmpty(iteratorName)) {
            return false;
        }

        IOperation loopBody;
        if (forEachLoop.Body is IBlockOperation { Operations: [var single] }) {
            loopBody = single;
        } else if (forEachLoop.Body is IConditionalOperation ifStatement) {
            loopBody = ifStatement;
        } else {
            return false;
        }

        if (GetPureExistenceIfForIterator(loopBody, iteratorName, cache) is false) {
            return false;
        }

        return true;
    }

    private static bool IsPureBaseTypeInheritanceCheck(IWhileLoopOperation whileLoop, TypeCache<KnownType> cache) {
        var iteratorName = GetConditionIteratorName(whileLoop.Condition);
        if (string.IsNullOrEmpty(iteratorName)) {
            return false;
        }

        if (whileLoop.Body is not IBlockOperation block || block.Operations.Length != 2) {
            return false;
        }

        IConditionalOperation? ifStatement = null;
        IAssignmentOperation? assignment = null;

        var foundIfStatement = false;
        var foundAssignment = false;

        foreach (var op in block.Operations) {
            if (op is IConditionalOperation ifOp && !foundIfStatement) {
                ifStatement = ifOp;
                foundIfStatement = true;
                continue;
            }

            if (op is IExpressionStatementOperation { Operation: IAssignmentOperation assignmentOp } && !foundAssignment) {
                assignment = assignmentOp;
                foundAssignment = true;
                continue;
            }

            return false;
        }

        if (!foundIfStatement || !foundAssignment) {
            return false;
        }

        if (TryGetAssignmentTargetName(assignment!.Target) is not { } assignmentTarget ||
            assignmentTarget != iteratorName) {
            return false;
        }

        if (assignment.Value is not IPropertyReferenceOperation {
            Instance: ILocalReferenceOperation { Local.Name: var baseValueName },
            Property.Name: "BaseType" } ||
            baseValueName != iteratorName) {
            return false;
        }

        return GetPureExistenceIfForIterator(ifStatement, iteratorName, cache);
    }

    private static string? GetConditionIteratorName(IOperation? condition) {
        if (condition is not IBinaryOperation { OperatorKind: BinaryOperatorKind.NotEquals } binary) {
            return null;
        }

        var (left, right) = (binary.LeftOperand.UnwrapAllConversions(), binary.RightOperand.UnwrapAllConversions());

        if (left is not null && IsNullLiteral(left) && TryGetIteratorName(right) is { } rightIterator) {
            return rightIterator;
        }

        if (right is not null && IsNullLiteral(right) && TryGetIteratorName(left) is { } leftIterator) {
            return leftIterator;
        }

        return null;
    }

    private static bool IsNullLiteral(IOperation operation) =>
        operation.ConstantValue.HasValue && operation.ConstantValue.Value is null;

    private static bool GetPureExistenceIfForIterator(
        IOperation? operation,
        string iteratorName,
        TypeCache<KnownType> cache) {
        if (operation is not IConditionalOperation ifStatement) {
            return false;
        }

        if (!IsBooleanReturnTrue(ifStatement.WhenTrue) || ifStatement.WhenFalse is not null) {
            return false;
        }

        return ContainsSymbolEqualityComparison(ifStatement.Condition, iteratorName, cache);
    }

    private static bool IsBooleanReturnTrue(IOperation? operation) {
        if (operation is not { } op) {
            return false;
        }

        return op switch {
            IReturnOperation { ReturnedValue: not null } ret =>
                ret.ReturnedValue.ConstantValue is { HasValue: true, Value: true },
            IBlockOperation { Operations.Length: 1, Operations: [var first] } =>
                IsBooleanReturnTrue(first),
            _ => false
        };
    }

    private static bool ContainsSymbolEqualityComparison(
        IOperation? body,
        string iteratorName,
        TypeCache<KnownType> cache) {
        if (body is null) {
            return false;
        }

        if (body is IInvocationOperation directInvocation &&
            IsSupportedComparisonInvocation(directInvocation, iteratorName, cache)) {
            return true;
        }

        foreach (var descendant in MsOperationExtensions.Descendants(body)) {
            if (descendant is not IInvocationOperation invocation) {
                continue;
            }

            return IsSupportedComparisonInvocation(invocation, iteratorName, cache);
        }

        return false;
    }

    private static bool IsSupportedComparisonInvocation(
        IInvocationOperation invocation,
        string iteratorName,
        TypeCache<KnownType> cache) {
        if (IsSymbolEqualityComparerEquals(invocation, cache)) {
            return IsIdentifierArgument(invocation.Arguments[0].Value.UnwrapAllConversions(), iteratorName) ||
                   IsIdentifierArgument(invocation.Arguments[1].Value.UnwrapAllConversions(), iteratorName);
        }

        if (invocation.TargetMethod is {
            Name: "IsEqualTo",
            IsExtensionMethod: true,
            Parameters.Length: 2
        }) {
            return IsIdentifierArgument(invocation.Arguments[0].Value.UnwrapAllConversions(), iteratorName) ||
                   IsIdentifierArgument(invocation.Arguments[1].Value.UnwrapAllConversions(), iteratorName);
        }

        return false;
    }

    private static bool HasBaseTypeLoopLeadingDeclaration(WhileStatementSyntax whileStatement) {
        if (whileStatement.Parent is not BlockSyntax block) {
            return false;
        }

        var index = block.Statements.IndexOf(whileStatement);
        if (index <= 0) {
            return false;
        }

        return block.Statements[index - 1] is LocalDeclarationStatementSyntax localDeclaration &&
               localDeclaration.Declaration.Variables.Any(static v =>
                   v.Initializer?.Value is MemberAccessExpressionSyntax { Name.Identifier.Text: "BaseType" });
    }

    private static bool IsSymbolEqualityComparerEquals(IInvocationOperation invocation, TypeCache<KnownType> cache) {
        var method = invocation.TargetMethod;
        if (method.Name != "Equals" || method.Parameters.Length != 2) {
            return false;
        }

        return cache.IsType(method.ContainingType, KnownType.SymbolEqualityComparer);
    }

    private static bool IsIdentifierArgument(IOperation? operation, string iteratorName) {
        if (operation is null) {
            return false;
        }

        if (operation.Syntax is IdentifierNameSyntax { Identifier.Text: var idName }) {
            return idName == iteratorName;
        }

        return operation switch {
            ILocalReferenceOperation { Local.Name: var localName } => localName == iteratorName,
            IParameterReferenceOperation { Parameter.Name: var parameterName } => parameterName == iteratorName,
            IPropertyReferenceOperation { Property.Name: var propertyName } => propertyName == iteratorName,
            _ => false
        };
    }

    private static string? TryGetIteratorName(IOperation? operation) {
        if (operation is null) {
            return null;
        }

        return operation switch {
            ILocalReferenceOperation { Local.Name: var name } => name,
            IParameterReferenceOperation { Parameter.Name: var name } => name,
            _ => operation.Syntax is IdentifierNameSyntax { Identifier.Text: var name } ? name : null
        };
    }

    private static string? TryGetAssignmentTargetName(IOperation target) {
        return target switch {
            ILocalReferenceOperation { Local.Name: var localName } => localName,
            IParameterReferenceOperation { Parameter.Name: var parameterName } => parameterName,
            _ => target.Syntax is IdentifierNameSyntax { Identifier.Text: var identifierName } ? identifierName : null
        };
    }
}
