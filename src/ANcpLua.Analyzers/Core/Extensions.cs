namespace ANcpLua.Analyzers.Core;

/// <summary>
/// Extension methods for diagnostic reporting.
/// </summary>
internal static class DiagnosticReportingExtensions
{
    public static void ReportDiagnostic(
        this SyntaxNodeAnalysisContext context,
        DiagnosticDescriptor descriptor,
        Location location,
        params object[] messageArgs)
        => context.ReportDiagnostic(Diagnostic.Create(descriptor, location, messageArgs));

    public static void ReportDiagnostic(
        this SyntaxNodeAnalysisContext context,
        DiagnosticDescriptor descriptor,
        Location location)
        => context.ReportDiagnostic(Diagnostic.Create(descriptor, location));

    public static void ReportDiagnostic(
        this OperationAnalysisContext context,
        DiagnosticDescriptor descriptor,
        Location location,
        params object[] messageArgs)
        => context.ReportDiagnostic(Diagnostic.Create(descriptor, location, messageArgs));

    public static void ReportDiagnostic(
        this OperationAnalysisContext context,
        DiagnosticDescriptor descriptor,
        Location location)
        => context.ReportDiagnostic(Diagnostic.Create(descriptor, location));

    public static void ReportDiagnostic(
        this SymbolAnalysisContext context,
        DiagnosticDescriptor descriptor,
        Location location,
        params object[] messageArgs)
        => context.ReportDiagnostic(Diagnostic.Create(descriptor, location, messageArgs));

    public static void ReportDiagnostic(
        this SymbolAnalysisContext context,
        DiagnosticDescriptor descriptor,
        Location location)
        => context.ReportDiagnostic(Diagnostic.Create(descriptor, location));
}

/// <summary>
/// Extension methods for SemanticModel.
/// </summary>
internal static class SemanticModelExtensions
{
    public static ISymbol? GetSymbol(
        this SemanticModel model,
        SyntaxNode node,
        CancellationToken cancellationToken = default)
        => model.GetSymbolInfo(node, cancellationToken).Symbol;
}

/// <summary>
/// Symbol equality comparer shorthand.
/// </summary>
internal static class Sec
{
    public static readonly SymbolEqualityComparer Default = SymbolEqualityComparer.Default;
}
