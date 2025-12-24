namespace ANcpLua.Analyzers.Core;

/// <summary>
/// Extension methods that simplify diagnostic reporting by combining
/// Diagnostic.Create and ReportDiagnostic into a single call.
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
