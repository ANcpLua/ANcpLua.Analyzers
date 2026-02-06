namespace ANcpLua.Analyzers.AotReflection;

[Generator]
public sealed partial class AotReflectionGenerator : IIncrementalGenerator {
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var typeFlows = context.SyntaxProvider.ForAttributeWithMetadataName(
            "ANcpLua.Analyzers.AotReflection.AotReflectionAttribute",
            static (node, _) => node is TypeDeclarationSyntax,
            static (syntaxContext, cancellationToken) => TypeExtractor.ExtractTypeModel(syntaxContext, cancellationToken));

        var types = typeFlows.ReportAndStop(context);

        var files = types
            .Select(static (model, _) => OutputGenerator.GenerateOutput(model))
            .CollectAsEquatableArray();

        files.AddSources(context);
    }
}
