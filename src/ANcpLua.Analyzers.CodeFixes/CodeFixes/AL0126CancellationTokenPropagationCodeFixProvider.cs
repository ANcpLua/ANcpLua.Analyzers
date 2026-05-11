using ANcpLua.Analyzers.Analyzers;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Operations;

namespace ANcpLua.Analyzers.CodeFixes.CodeFixes;

/// <summary>
///     Code fix for AL0126: adds a named <see cref="CancellationToken" /> argument.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Al0126CancellationTokenPropagationCodeFixProvider))]
[Shared]
public sealed partial class Al0126CancellationTokenPropagationCodeFixProvider
    : AlCodeFixProvider<InvocationExpressionSyntax> {
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [Al0126CancellationTokenPropagationAnalyzer.DiagnosticId];

    protected override CodeAction CreateCodeAction(
        Document document,
        InvocationExpressionSyntax invocation,
        SyntaxNode root,
        Diagnostic diagnostic) =>
        CodeAction.Create(
            CodeFixResources.AL0126CodeFixTitle,
            ct => AddCancellationTokenAsync(document, invocation, root, ct),
            nameof(CodeFixResources.AL0126CodeFixTitle));

    private static async Task<Document> AddCancellationTokenAsync(
        Document document,
        InvocationExpressionSyntax invocation,
        SyntaxNode root,
        CancellationToken cancellationToken) {
        if (await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) is not
                { } semanticModel ||
            semanticModel.GetOperation(invocation, cancellationToken) is not IInvocationOperation operation ||
            semanticModel.Compilation.GetTypeByMetadataName("System.Threading.CancellationToken") is not
                INamedTypeSymbol cancellationTokenType) {
            return document;
        }

        var expressionType =
            semanticModel.Compilation.GetTypeByMetadataName("System.Linq.Expressions.Expression`1");

        if (!Al0126CancellationTokenPropagationAnalysis.TryFindSuggestion(
                operation,
                cancellationTokenType,
                expressionType,
                cancellationToken,
                out var suggestion)) {
            return document;
        }

        var explicitOperationArguments = operation.Arguments
            .Where(static argument => argument.ArgumentKind != ArgumentKind.DefaultValue)
            .ToImmutableArray();
        if (explicitOperationArguments.Length != invocation.ArgumentList.Arguments.Count) {
            return document;
        }

        var argumentsByOriginalOrdinal = new Dictionary<int, ArgumentSyntax>();
        for (var index = 0; index < explicitOperationArguments.Length; index++) {
            if (explicitOperationArguments[index].Parameter is not { } parameter ||
                parameter.IsParams ||
                argumentsByOriginalOrdinal.ContainsKey(parameter.Ordinal)) {
                return document;
            }

            argumentsByOriginalOrdinal.Add(parameter.Ordinal, invocation.ArgumentList.Arguments[index]);
        }

        if (suggestion.ReplaceExistingArgument) {
            if (!argumentsByOriginalOrdinal.TryGetValue(suggestion.ParameterIndex, out var sourceArgument)) {
                return document;
            }

            var replacement = sourceArgument.WithExpression(SyntaxFactory.ParseExpression(suggestion.ExpressionText));
            var replacementInvocation = invocation.ReplaceNode(sourceArgument, replacement)
                .WithAdditionalAnnotations(Formatter.Annotation);

            return document.WithSyntaxRoot(root.ReplaceNode(invocation, replacementInvocation));
        }

        var updatedArguments = new List<ArgumentSyntax>();
        for (var targetIndex = 0; targetIndex < suggestion.TargetParameterNames.Length; targetIndex++) {
            var targetParameterName = suggestion.TargetParameterNames[targetIndex];
            if (targetIndex == suggestion.ParameterIndex) {
                updatedArguments.Add(CreateNamedArgument(targetParameterName, suggestion.ExpressionText));
                continue;
            }

            var originalOrdinal = targetIndex < suggestion.ParameterIndex
                ? targetIndex
                : targetIndex - 1;
            if (!argumentsByOriginalOrdinal.TryGetValue(originalOrdinal, out var sourceArgument)) {
                continue;
            }

            updatedArguments.Add(targetIndex > suggestion.ParameterIndex || sourceArgument.NameColon is not null
                ? RenameArgument(sourceArgument, targetParameterName)
                : sourceArgument);
        }

        var updatedInvocation = invocation.WithArgumentList(
                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(updatedArguments)))
            .WithAdditionalAnnotations(Formatter.Annotation);

        return document.WithSyntaxRoot(root.ReplaceNode(invocation, updatedInvocation));
    }

    private static ArgumentSyntax CreateNamedArgument(string parameterName, string expressionText) =>
        SyntaxFactory.Argument(
                nameColon: SyntaxFactory.NameColon(parameterName),
                refKindKeyword: default,
                expression: SyntaxFactory.ParseExpression(expressionText))
            .WithAdditionalAnnotations(Formatter.Annotation);

    private static ArgumentSyntax RenameArgument(ArgumentSyntax argument, string parameterName) =>
        argument.WithNameColon(SyntaxFactory.NameColon(parameterName))
            .WithAdditionalAnnotations(Formatter.Annotation);
}
