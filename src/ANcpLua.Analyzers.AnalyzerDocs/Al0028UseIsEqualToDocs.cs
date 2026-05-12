using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.AnalyzerDocsGenerator;

namespace ANcpLua.Analyzers.AnalyzerDocs;

/// <summary>
///     Docs scenarios for AL0028: Use IsEqualTo extension instead of SymbolEqualityComparer.Equals.
///     Each <see cref="ScenarioAttribute" />-tagged method's body is the markdown source — its
///     verbatim text becomes a fenced <c>cs</c> block under <c>### scenario: {MethodName}</c>.
///     Each <c>{Name}_Failure</c> twin runs the analyzer over the diagnostic-triggering form so
///     the generator inlines the analyzer's real diagnostic message into the
///     <c>#### Failure messages</c> block — keeping the docs in lockstep with the analyzer.
/// </summary>
public sealed partial class Al0028UseIsEqualToDocs
{
    private const string Polyfill = """
                                    namespace Microsoft.CodeAnalysis {
                                        public interface ISymbol { }
                                        public class SymbolEqualityComparer : System.Collections.Generic.IEqualityComparer<ISymbol> {
                                            public static readonly SymbolEqualityComparer Default = new();
                                            public bool Equals(ISymbol? x, ISymbol? y) => true;
                                            public int GetHashCode(ISymbol obj) => 0;
                                        }
                                    }
                                    """;

    [Scenario]
    public void SymbolEqualityComparerDefaultEquals()
    {
        // Rule:     AL0028 (Roslyn Utilities, Info)
        // Fix:      Replaces SymbolEqualityComparer.Default.Equals(a, b) with a.IsEqualTo(b).
        //
        // Before (flagged):
        //   if (SymbolEqualityComparer.Default.Equals(symbol1, symbol2)) { }
        //
        // After (clean):
        //   if (symbol1.IsEqualTo(symbol2)) { }
    }

    [Scenario]
    public Task SymbolEqualityComparerDefaultEquals_Failure() =>
        AnalyzerDocsUtils.RunAndCaptureDiagnosticAsync<Al0028UseIsEqualToAnalyzer>($$"""
            {{Polyfill}}
            public class C {
                void M(Microsoft.CodeAnalysis.ISymbol symbol1, Microsoft.CodeAnalysis.ISymbol symbol2) {
                    if (Microsoft.CodeAnalysis.SymbolEqualityComparer.Default.Equals(symbol1, symbol2)) { }
                }
            }
            """);
}
