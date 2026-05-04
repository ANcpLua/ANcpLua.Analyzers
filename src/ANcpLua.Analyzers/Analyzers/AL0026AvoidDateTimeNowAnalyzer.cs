
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0026: Avoid System.DateTime and DateTimeOffset time accessors.
///     Suggests using TimeProvider.System.GetLocalNow() or GetUtcNow() for better testability.
///     Only reports when TimeProvider is available (.NET 8+).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0026AvoidDateTimeNowAnalyzer : AlAnalyzer {
    private enum KnownType { TimeProvider, DateTime, DateTimeOffset }

    private static readonly string[] s_knownTypeNames = [
        "System.TimeProvider",
        "System.DateTime",
        "System.DateTimeOffset"
    ];

    /// <summary>Property key for the source type (DateTime or DateTimeOffset).</summary>
    public const string PropertyIsDateTimeOffset = "IsDateTimeOffset";

    /// <summary>The diagnostic identifier for AL0026.</summary>
    public const string DiagnosticId = "AL0026";

    private static readonly DiagnosticDescriptor s_rule = CreateRule(
        DiagnosticId,
        DiagnosticCategories.Usage,
        DiagnosticSeverity.Warning);

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

    /// <summary>Registers compilation start action to analyze DateTime member access.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        var cache = new TypeCache<KnownType>(type => context.Compilation.GetTypeByMetadataName(s_knownTypeNames[(int)type]));

        if (cache.Get(KnownType.TimeProvider) is null) {
            return;
        }

        if (cache.Get(KnownType.DateTime) is null && cache.Get(KnownType.DateTimeOffset) is null) {
            return;
        }

        context.RegisterOperationAction(
            ctx => AnalyzePropertyReference(ctx, cache),
            OperationKind.PropertyReference);
    }

    private static void AnalyzePropertyReference(OperationAnalysisContext context, TypeCache<KnownType> cache) {
        if (context.Operation is not IPropertyReferenceOperation propertyRef) {
            return;
        }

        if (context.ContainingSymbol.ContainingType is { } enclosingType &&
            (cache.IsType(enclosingType, KnownType.TimeProvider) ||
             cache.ImplementsOrInheritsFrom(enclosingType, KnownType.TimeProvider))) {
            return;
        }

        var property = propertyRef.Property;
        var containingType = property.ContainingType;

        var isDateTime = cache.IsType(containingType, KnownType.DateTime);
        var isDateTimeOffset = cache.IsType(containingType, KnownType.DateTimeOffset);

        if (!isDateTime && !isDateTimeOffset) {
            return;
        }

        if (property.Name switch {
            "Now" => "GetLocalNow",
            "UtcNow" => "GetUtcNow",
            _ => null
        } is not { } replacement) {
            return;
        }

        var typeName = isDateTime ? "DateTime" : "DateTimeOffset";
        var properties = ImmutableDictionary.CreateBuilder<string, string?>();
        properties.Add(PropertyIsDateTimeOffset, isDateTimeOffset.ToString());
        context.ReportDiagnostic(Diagnostic.Create(s_rule, propertyRef.Syntax.GetLocation(), properties.ToImmutable(), typeName, property.Name, replacement));
    }
}
