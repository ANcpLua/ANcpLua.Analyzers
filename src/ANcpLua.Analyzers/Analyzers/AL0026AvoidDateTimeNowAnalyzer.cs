using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0026: Avoid System.DateTime time accessors.
///     Suggests using TimeProvider.System.GetLocalNow() or GetUtcNow() for better testability.
///     Only reports when TimeProvider is available (.NET 8+).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0026AvoidDateTimeNowAnalyzer : AlAnalyzer {
    private const string TimeProviderMetadataName = "System.TimeProvider";
    private const string DateTimeMetadataName = "System.DateTime";

    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0026AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0026AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0026AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.AvoidDateTimeNow,
        Title, MessageFormat, DiagnosticCategories.Usage,
        DiagnosticSeverity.Warning, true, Description,
        HelpLinkBase);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        // Only analyze if TimeProvider is available (.NET 8+)
        if (context.Compilation.GetTypeByMetadataName(TimeProviderMetadataName) is null) {
            return;
        }

        if (context.Compilation.GetTypeByMetadataName(DateTimeMetadataName) is not { } dateTimeType) {
            return;
        }

        context.RegisterOperationAction(
            ctx => AnalyzePropertyReference(ctx, dateTimeType),
            OperationKind.PropertyReference);
    }

    private static void AnalyzePropertyReference(OperationAnalysisContext context, INamedTypeSymbol dateTimeType) {
        if (context.Operation is not IPropertyReferenceOperation propertyRef) {
            return;
        }

        var property = propertyRef.Property;

        // Check if it's a DateTime static time property
        if (!SymbolEqualityComparer.Default.Equals(property.ContainingType, dateTimeType)) {
            return;
        }

        // Target the "Now" and "UtcNow" properties with correct replacements
        if (property.Name switch {
            "Now" => "GetLocalNow",
            "UtcNow" => "GetUtcNow",
            _ => null
        } is not { } replacement) {
            return;
        }

        context.ReportDiagnostic(Rule, propertyRef.Syntax.GetLocation(), property.Name, replacement);
    }
}
