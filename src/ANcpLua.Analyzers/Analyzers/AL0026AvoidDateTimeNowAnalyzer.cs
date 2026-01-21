using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0026: Avoid System.DateTime and DateTimeOffset time accessors.
///     Suggests using TimeProvider.System.GetLocalNow() or GetUtcNow() for better testability.
///     Only reports when TimeProvider is available (.NET 8+).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0026AvoidDateTimeNowAnalyzer : AlAnalyzer {
    private const string TimeProviderMetadataName = "System.TimeProvider";
    private const string DateTimeMetadataName = "System.DateTime";
    private const string DateTimeOffsetMetadataName = "System.DateTimeOffset";

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
        if (context.Compilation.GetTypeByMetadataName(TimeProviderMetadataName) is not { } timeProviderType) {
            return;
        }

        var dateTimeType = context.Compilation.GetTypeByMetadataName(DateTimeMetadataName);
        var dateTimeOffsetType = context.Compilation.GetTypeByMetadataName(DateTimeOffsetMetadataName);

        // Need at least one of the types to analyze
        if (dateTimeType is null && dateTimeOffsetType is null) {
            return;
        }

        context.RegisterOperationAction(
            ctx => AnalyzePropertyReference(ctx, dateTimeType, dateTimeOffsetType, timeProviderType),
            OperationKind.PropertyReference);
    }

    private static void AnalyzePropertyReference(
        OperationAnalysisContext context,
        INamedTypeSymbol? dateTimeType,
        INamedTypeSymbol? dateTimeOffsetType,
        INamedTypeSymbol timeProviderType) {
        if (context.Operation is not IPropertyReferenceOperation propertyRef) {
            return;
        }

        // Skip if we're inside a TimeProvider implementation (polyfills need to call DateTime/DateTimeOffset)
        if (context.ContainingSymbol?.ContainingType is { } enclosingType &&
            InheritsFromOrEquals(enclosingType, timeProviderType)) {
            return;
        }

        var property = propertyRef.Property;
        var containingType = property.ContainingType;

        // Check if it's a DateTime or DateTimeOffset static time property
        var isDateTime = dateTimeType is not null &&
                         SymbolEqualityComparer.Default.Equals(containingType, dateTimeType);
        var isDateTimeOffset = dateTimeOffsetType is not null &&
                               SymbolEqualityComparer.Default.Equals(containingType, dateTimeOffsetType);

        if (!isDateTime && !isDateTimeOffset) {
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

        var typeName = isDateTime ? "DateTime" : "DateTimeOffset";
        context.ReportDiagnostic(Rule, propertyRef.Syntax.GetLocation(), typeName, property.Name, replacement);
    }

    private static bool InheritsFromOrEquals(INamedTypeSymbol type, INamedTypeSymbol baseType) {
        var current = type;
        while (current is not null) {
            if (SymbolEqualityComparer.Default.Equals(current, baseType)) {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }
}
