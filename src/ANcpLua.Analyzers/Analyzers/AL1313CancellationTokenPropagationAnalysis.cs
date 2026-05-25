namespace ANcpLua.Analyzers.Analyzers;

/// <summary>Describes the named argument AL1313 should add.</summary>
public readonly partial record struct Al1313Suggestion(
    string ParameterName,
    int ParameterIndex,
    ImmutableArray<string> TargetParameterNames,
    string ExpressionText,
    bool ReplaceExistingArgument = false);

/// <summary>
///     Shared analysis helpers for AL1313.
/// </summary>
public static partial class Al1313CancellationTokenPropagationAnalysis {
    private const string CancellationTokenSourceMetadataName = "System.Threading.CancellationTokenSource";
    private const string XunitTestContextMetadataName = "Xunit.TestContext";

    private readonly partial record struct ParameterMatch(
        string ParameterName,
        int ParameterIndex,
        ImmutableArray<string> TargetParameterNames);

    /// <summary>Tries to find a safe AL1313 suggestion for the specified invocation.</summary>
    public static bool TryFindSuggestion(
        IInvocationOperation invocation,
        INamedTypeSymbol cancellationTokenType,
        INamedTypeSymbol? expressionType,
        CancellationToken cancellationToken,
        out Al1313Suggestion suggestion) {
        suggestion = default;

        if (invocation.TargetMethod.MethodKind == MethodKind.DelegateInvoke ||
            invocation.IsInExpressionTree(expressionType) ||
            IsConditionalAccessInvocation(invocation) ||
            IsInsideNameof(invocation) ||
            IsInMockingContext(invocation)) {
            return false;
        }

        if (TryFindDefaultCancellationTokenArgument(invocation, cancellationTokenType, out var defaultArgumentMatch) &&
            FindAvailableTokenExpression(invocation, cancellationTokenType, cancellationToken) is { } replacementText &&
            !ShouldSuppressForContainingContract(invocation, cancellationTokenType, cancellationToken)) {
            suggestion = new Al1313Suggestion(
                defaultArgumentMatch.ParameterName,
                defaultArgumentMatch.ParameterIndex,
                defaultArgumentMatch.TargetParameterNames,
                replacementText,
                ReplaceExistingArgument: true);
            return true;
        }

        if (!TryFindTargetParameter(
                invocation,
                cancellationTokenType,
                cancellationToken,
                out var parameterMatch) ||
            FindAvailableTokenExpression(invocation, cancellationTokenType, cancellationToken) is not { } expressionText ||
            ShouldSuppressForContainingContract(invocation, cancellationTokenType, cancellationToken)) {
            return false;
        }

        suggestion = new Al1313Suggestion(
            parameterMatch.ParameterName,
            parameterMatch.ParameterIndex,
            parameterMatch.TargetParameterNames,
            expressionText);
        return true;
    }

    private static bool TryFindDefaultCancellationTokenArgument(
        IInvocationOperation invocation,
        INamedTypeSymbol cancellationTokenType,
        out ParameterMatch parameterMatch) {
        foreach (var argument in invocation.Arguments) {
            if (argument.ArgumentKind == ArgumentKind.DefaultValue ||
                argument.Parameter is not { } parameter ||
                !IsCancellationTokenParameter(parameter, cancellationTokenType) ||
                !IsDefaultCancellationTokenArgument(argument.Value, cancellationTokenType)) {
                continue;
            }

            parameterMatch = new ParameterMatch(
                parameter.Name,
                parameter.Ordinal,
                [.. invocation.TargetMethod.Parameters.Select(static parameter => parameter.Name)]);
            return true;
        }

        parameterMatch = default;
        return false;
    }

    private static bool IsDefaultCancellationTokenArgument(
        IOperation operation,
        INamedTypeSymbol cancellationTokenType) {
        operation = operation.UnwrapAllConversions().UnwrapParenthesized();

        if (operation.Syntax.IsKind(SyntaxKind.DefaultExpression) ||
            operation.Syntax.IsKind(SyntaxKind.DefaultLiteralExpression)) {
            return true;
        }

        return operation is IPropertyReferenceOperation {
            Property: { Name: "None", ContainingType: { } containingType }
        } && containingType.IsEqualTo(cancellationTokenType);
    }

    private static bool TryFindTargetParameter(
        IInvocationOperation invocation,
        INamedTypeSymbol cancellationTokenType,
        CancellationToken cancellationToken,
        out ParameterMatch parameterMatch) {
        if (TryFindOmittedCancellationTokenParameter(invocation, cancellationTokenType, out parameterMatch)) {
            return true;
        }

        return TryFindAccessibleOverloadParameter(
            invocation,
            cancellationTokenType,
            cancellationToken,
            out parameterMatch);
    }

    private static bool TryFindOmittedCancellationTokenParameter(
        IInvocationOperation invocation,
        INamedTypeSymbol cancellationTokenType,
        out ParameterMatch parameterMatch) {
        for (var index = 0; index < invocation.TargetMethod.Parameters.Length; index++) {
            var parameter = invocation.TargetMethod.Parameters[index];
            if (!IsCancellationTokenParameter(parameter, cancellationTokenType)) {
                continue;
            }

            var supplied = false;
            foreach (var argument in invocation.Arguments) {
                if (argument.Parameter?.IsEqualTo(parameter) == true) {
                    supplied = true;
                    break;
                }
            }

            if (!supplied) {
                parameterMatch = new ParameterMatch(
                    parameter.Name,
                    index,
                    [.. invocation.TargetMethod.Parameters.Select(static parameter => parameter.Name)]);
                return true;
            }
        }

        parameterMatch = default;
        return false;
    }

    private static bool TryFindAccessibleOverloadParameter(
        IInvocationOperation invocation,
        INamedTypeSymbol cancellationTokenType,
        CancellationToken cancellationToken,
        out ParameterMatch parameterMatch) {
        parameterMatch = default;

        if (invocation.SemanticModel is null || invocation.Syntax is not InvocationExpressionSyntax invocationSyntax) {
            return false;
        }

        foreach (var symbol in invocation.SemanticModel.GetMemberGroup(invocationSyntax.Expression, cancellationToken)) {
            if (symbol is not IMethodSymbol candidate ||
                candidate.IsEqualTo(invocation.TargetMethod)) {
                continue;
            }

            if (TryMatchCancellationTokenOverload(
                    invocation.TargetMethod,
                    candidate,
                    cancellationTokenType,
                    out parameterMatch)) {
                return true;
            }
        }

        for (var current = invocation.TargetMethod.ContainingType; current is not null; current = current.BaseType) {
            foreach (var member in current.GetMembers(invocation.TargetMethod.Name)) {
                if (member is IMethodSymbol candidate &&
                    !candidate.IsEqualTo(invocation.TargetMethod) &&
                    TryMatchCancellationTokenOverload(
                        invocation.TargetMethod,
                        candidate,
                        cancellationTokenType,
                        out parameterMatch)) {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryMatchCancellationTokenOverload(
        IMethodSymbol originalMethod,
        IMethodSymbol candidateMethod,
        INamedTypeSymbol cancellationTokenType,
        out ParameterMatch parameterMatch) {
        parameterMatch = default;

        if (candidateMethod.Arity != originalMethod.Arity ||
            !candidateMethod.ReturnType.IsEqualTo(originalMethod.ReturnType) ||
            candidateMethod.Parameters.Length != originalMethod.Parameters.Length + 1 ||
            IsObsolete(candidateMethod)) {
            return false;
        }

        for (var candidateIndex = 0; candidateIndex < candidateMethod.Parameters.Length; candidateIndex++) {
            var candidateParameter = candidateMethod.Parameters[candidateIndex];
            if (!IsCancellationTokenParameter(candidateParameter, cancellationTokenType) ||
                !ParametersMatchWithInsertion(originalMethod.Parameters, candidateMethod.Parameters, candidateIndex)) {
                continue;
            }

            parameterMatch = new ParameterMatch(
                candidateParameter.Name,
                candidateIndex,
                [.. candidateMethod.Parameters.Select(static parameter => parameter.Name)]);
            return true;
        }

        return false;
    }

    private static bool ParametersMatchWithInsertion(
        ImmutableArray<IParameterSymbol> originalParameters,
        ImmutableArray<IParameterSymbol> candidateParameters,
        int insertionIndex) {
        var originalIndex = 0;

        for (var candidateIndex = 0; candidateIndex < candidateParameters.Length; candidateIndex++) {
            if (candidateIndex == insertionIndex) {
                continue;
            }

            if (originalIndex >= originalParameters.Length ||
                !AreEquivalentParameters(originalParameters[originalIndex], candidateParameters[candidateIndex])) {
                return false;
            }

            originalIndex++;
        }

        return originalIndex == originalParameters.Length;
    }

    private static bool AreEquivalentParameters(IParameterSymbol originalParameter, IParameterSymbol candidateParameter) =>
        originalParameter.Type.IsEqualTo(candidateParameter.Type) &&
        originalParameter.RefKind == candidateParameter.RefKind &&
        originalParameter.IsParams == candidateParameter.IsParams;

    private static bool IsCancellationTokenParameter(IParameterSymbol parameter, INamedTypeSymbol cancellationTokenType) =>
        parameter is { RefKind: RefKind.None, IsParams: false } &&
        parameter.Type.IsEqualTo(cancellationTokenType);

    private static bool IsObsolete(ISymbol symbol) {
        foreach (var attribute in symbol.GetAttributes()) {
            if (attribute.AttributeClass?.ToDisplayString() == "System.ObsoleteAttribute") {
                return true;
            }
        }

        return false;
    }

    private static string? FindAvailableTokenExpression(
        IInvocationOperation invocation,
        INamedTypeSymbol cancellationTokenType,
        CancellationToken cancellationToken) {
        if (invocation.SemanticModel is not { } semanticModel) {
            return null;
        }

        var cancellationTokenSourceType =
            semanticModel.Compilation.GetTypeByMetadataName(CancellationTokenSourceMetadataName);
        var symbols = semanticModel.LookupSymbols(invocation.Syntax.SpanStart);
        var currentMethod = invocation.GetContainingMethod(cancellationToken);

        if (TryFindCurrentMethodParameter(symbols, currentMethod, cancellationTokenType, out var expressionText) ||
            TryFindLocalToken(symbols, cancellationTokenType, out expressionText) ||
            TryFindOuterParameter(symbols, currentMethod, cancellationTokenType, out expressionText) ||
            TryFindCancellationTokenSource(symbols, cancellationTokenSourceType, out expressionText) ||
            TryFindMemberToken(symbols, cancellationTokenType, out expressionText) ||
            TryFindHttpContextRequestAborted(invocation, cancellationTokenType, cancellationToken, out expressionText)) {
            return expressionText;
        }

        return HasXunitTestContext(semanticModel.Compilation, cancellationTokenType)
            ? "global::Xunit.TestContext.Current.CancellationToken"
            : null;
    }

    private static bool TryFindCurrentMethodParameter(
        IEnumerable<ISymbol> symbols,
        IMethodSymbol? currentMethod,
        INamedTypeSymbol cancellationTokenType,
        out string expressionText) {
        foreach (var symbol in symbols) {
            if (symbol is IParameterSymbol parameter &&
                currentMethod is not null &&
                parameter.ContainingSymbol.IsEqualTo(currentMethod) &&
                parameter.Type.IsEqualTo(cancellationTokenType)) {
                expressionText = parameter.Name;
                return true;
            }
        }

        expressionText = string.Empty;
        return false;
    }

    private static bool TryFindLocalToken(
        IEnumerable<ISymbol> symbols,
        INamedTypeSymbol cancellationTokenType,
        out string expressionText) {
        foreach (var symbol in symbols) {
            if (symbol is ILocalSymbol { IsImplicitlyDeclared: false } local &&
                local.Type.IsEqualTo(cancellationTokenType)) {
                expressionText = local.Name;
                return true;
            }
        }

        expressionText = string.Empty;
        return false;
    }

    private static bool TryFindOuterParameter(
        IEnumerable<ISymbol> symbols,
        IMethodSymbol? currentMethod,
        INamedTypeSymbol cancellationTokenType,
        out string expressionText) {
        foreach (var symbol in symbols) {
            if (symbol is IParameterSymbol parameter &&
                (currentMethod is null || !parameter.ContainingSymbol.IsEqualTo(currentMethod)) &&
                parameter.Type.IsEqualTo(cancellationTokenType)) {
                expressionText = parameter.Name;
                return true;
            }
        }

        expressionText = string.Empty;
        return false;
    }

    private static bool TryFindCancellationTokenSource(
        IEnumerable<ISymbol> symbols,
        INamedTypeSymbol? cancellationTokenSourceType,
        out string expressionText) {
        if (cancellationTokenSourceType is null) {
            expressionText = string.Empty;
            return false;
        }

        foreach (var symbol in symbols) {
            switch (symbol) {
                case IParameterSymbol parameter when parameter.Type.IsEqualTo(cancellationTokenSourceType):
                    expressionText = parameter.Name + ".Token";
                    return true;
                case ILocalSymbol { IsImplicitlyDeclared: false } local when local.Type.IsEqualTo(cancellationTokenSourceType):
                    expressionText = local.Name + ".Token";
                    return true;
                case IFieldSymbol { IsImplicitlyDeclared: false, IsStatic: false } field
                    when field.Type.IsEqualTo(cancellationTokenSourceType):
                    expressionText = field.Name + ".Token";
                    return true;
                case IPropertySymbol { IsStatic: false, GetMethod: not null } property
                    when property.Type.IsEqualTo(cancellationTokenSourceType):
                    expressionText = property.Name + ".Token";
                    return true;
            }
        }

        expressionText = string.Empty;
        return false;
    }

    private static bool TryFindMemberToken(
        IEnumerable<ISymbol> symbols,
        INamedTypeSymbol cancellationTokenType,
        out string expressionText) {
        foreach (var symbol in symbols) {
            switch (symbol) {
                case IFieldSymbol { IsImplicitlyDeclared: false, IsStatic: false } field
                    when field.Type.IsEqualTo(cancellationTokenType):
                    expressionText = field.Name;
                    return true;
                case IPropertySymbol { IsStatic: false, GetMethod: not null } property
                    when property.Type.IsEqualTo(cancellationTokenType):
                    expressionText = property.Name;
                    return true;
            }
        }

        expressionText = string.Empty;
        return false;
    }

    private static bool TryFindHttpContextRequestAborted(
        IInvocationOperation invocation,
        INamedTypeSymbol cancellationTokenType,
        CancellationToken cancellationToken,
        out string expressionText) {
        for (var current = invocation.GetContainingType(cancellationToken); current is not null; current = current.BaseType) {
            foreach (var member in current.GetMembers("HttpContext")) {
                if (member is not IPropertySymbol { GetMethod: not null, Type: INamedTypeSymbol httpContextType }) {
                    continue;
                }

                foreach (var httpContextMember in httpContextType.GetMembers("RequestAborted")) {
                    if (httpContextMember is IPropertySymbol { GetMethod: not null } requestAborted &&
                        requestAborted.Type.IsEqualTo(cancellationTokenType)) {
                        expressionText = "HttpContext.RequestAborted";
                        return true;
                    }
                }
            }
        }

        expressionText = string.Empty;
        return false;
    }

    private static bool HasXunitTestContext(Compilation compilation, INamedTypeSymbol cancellationTokenType) {
        if (compilation.GetTypeByMetadataName(XunitTestContextMetadataName) is not INamedTypeSymbol testContextType) {
            return false;
        }

        var hasCurrent = testContextType.GetMembers("Current")
            .OfType<IPropertySymbol>()
            .Any(property => property.IsStatic && property.Type.IsEqualTo(testContextType));
        if (!hasCurrent) {
            return false;
        }

        return testContextType.GetMembers("CancellationToken")
            .OfType<IPropertySymbol>()
            .Any(property => property.GetMethod is not null && property.Type.IsEqualTo(cancellationTokenType));
    }

    private static bool IsConditionalAccessInvocation(IInvocationOperation invocation) {
        for (var current = invocation.Parent; current is not null; current = current.Parent) {
            if (current is IConditionalAccessOperation or IConditionalAccessInstanceOperation) {
                return true;
            }
        }

        foreach (var syntax in invocation.Syntax.AncestorsAndSelf()) {
            if (syntax is ConditionalAccessExpressionSyntax) {
                return true;
            }
        }

        return false;
    }

    private static bool IsInsideNameof(IOperation operation) {
        for (var current = operation.Parent; current is not null; current = current.Parent) {
            if (current is INameOfOperation) {
                return true;
            }
        }

        return false;
    }

    private static bool ShouldSuppressForContainingContract(
        IInvocationOperation invocation,
        INamedTypeSymbol cancellationTokenType,
        CancellationToken cancellationToken) {
        if ((invocation.GetContainingMethod(cancellationToken) ??
             FindContainingMethod(invocation, cancellationToken)) is not { } currentMethod) {
            return false;
        }

        if (IsInterfaceImplementation(currentMethod) &&
            !HasCancellationTokenParameter(currentMethod, cancellationTokenType)) {
            return true;
        }

        return currentMethod.OverriddenMethod is not null &&
               !HasCancellationTokenParameter(currentMethod.OverriddenMethod, cancellationTokenType);
    }

    private static bool HasCancellationTokenParameter(IMethodSymbol method, INamedTypeSymbol cancellationTokenType) {
        foreach (var parameter in method.Parameters) {
            if (parameter.Type.IsEqualTo(cancellationTokenType)) {
                return true;
            }
        }

        return false;
    }

    private static IMethodSymbol? FindContainingMethod(IOperation operation, CancellationToken cancellationToken) {
        if (operation.SemanticModel is null) {
            return null;
        }

        foreach (var syntax in operation.Syntax.AncestorsAndSelf()) {
            switch (syntax) {
                case MethodDeclarationSyntax methodDeclaration:
                    return operation.SemanticModel.GetDeclaredSymbol(methodDeclaration, cancellationToken);
                case LocalFunctionStatementSyntax localFunction:
                    return operation.SemanticModel.GetDeclaredSymbol(localFunction, cancellationToken);
                case AccessorDeclarationSyntax accessor:
                    return operation.SemanticModel.GetDeclaredSymbol(accessor, cancellationToken);
                case LambdaExpressionSyntax lambda:
                    return operation.SemanticModel.GetSymbolInfo(lambda, cancellationToken).Symbol as IMethodSymbol;
                case AnonymousMethodExpressionSyntax anonymousMethod:
                    return operation.SemanticModel.GetSymbolInfo(anonymousMethod, cancellationToken).Symbol as IMethodSymbol;
            }
        }

        return null;
    }

    private static bool IsInterfaceImplementation(IMethodSymbol method) {
        if (method.ExplicitInterfaceImplementations.Length > 0) {
            return true;
        }

        if (method.ContainingType is null) {
            return false;
        }

        foreach (var iface in method.ContainingType.AllInterfaces) {
            foreach (var member in iface.GetMembers(method.Name)) {
                if (member is IMethodSymbol interfaceMethod &&
                    method.ContainingType.FindImplementationForInterfaceMember(interfaceMethod) is IMethodSymbol implementation &&
                    implementation.IsEqualTo(method)) {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsInMockingContext(IInvocationOperation invocation) {
        for (var current = invocation.Parent; current is not null; current = current.Parent) {
            if (current is IAnonymousFunctionOperation or ILocalFunctionOperation) {
                break;
            }

            if (current is not IInvocationOperation outerInvocation) {
                continue;
            }

            var namespaceName = outerInvocation.TargetMethod.ContainingNamespace.ToDisplayString();
            if (namespaceName is "Moq" or "NSubstitute" or "FakeItEasy" ||
                namespaceName.StartsWithOrdinal("Moq.") ||
                namespaceName.StartsWithOrdinal("NSubstitute.") ||
                namespaceName.StartsWithOrdinal("FakeItEasy.")) {
                return true;
            }
        }

        return false;
    }
}
