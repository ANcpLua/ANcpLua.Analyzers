namespace ANcpLua.Analyzers.AotReflection;

internal static partial class DiagnosticDescriptors {
    public static readonly DiagnosticDescriptor InvalidTarget = new(
        "AL0097",
        "Invalid AOT reflection target",
        "AotReflectionAttribute can only be applied to classes or structs",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TypeMustBePartial = new(
        "AL0098",
        "Type must be partial",
        "Type '{0}' must be declared partial to use AotReflectionAttribute",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor IndexerNotSupported = new(
        "AL0099",
        "Indexer properties are not supported",
        "Indexer property '{0}' is not supported by AOT reflection and will be skipped",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor GenericMethodNotSupported = new(
        "AL0100",
        "Generic methods are not supported",
        "Generic method '{0}' is not supported by AOT reflection and will be skipped",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
