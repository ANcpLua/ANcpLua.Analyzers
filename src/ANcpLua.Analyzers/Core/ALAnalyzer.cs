namespace ANcpLua.Analyzers.Core;

/// <summary>
///     Base class for all ANcpLua analyzers.
/// </summary>
public abstract class ALAnalyzer : DiagnosticAnalyzer
{
    protected const string HelpLinkBase = "https://github.com/ANcpLua/ANcpLua.Analyzers/blob/main/docs/";

    public sealed override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        RegisterActions(context);
    }

    protected abstract void RegisterActions(AnalysisContext context);
}