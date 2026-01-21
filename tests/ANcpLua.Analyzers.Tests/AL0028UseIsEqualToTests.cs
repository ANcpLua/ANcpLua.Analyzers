using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0028: Use IsEqualTo extension instead of SymbolEqualityComparer.Equals.
/// </summary>
public sealed partial class Al0028UseIsEqualToTests : AnalyzerTest<Al0028UseIsEqualToAnalyzer> {
    private const string RoslynPolyfill = """
                                          namespace Microsoft.CodeAnalysis {
                                              public interface ISymbol { }
                                              public interface ITypeSymbol : ISymbol { }
                                              public interface INamedTypeSymbol : ITypeSymbol { }
                                              public class SymbolEqualityComparer : System.Collections.Generic.IEqualityComparer<ISymbol> {
                                                  public static readonly SymbolEqualityComparer Default = new();
                                                  public static readonly SymbolEqualityComparer IncludeNullability = new();
                                                  public bool Equals(ISymbol? x, ISymbol? y) => true;
                                                  public int GetHashCode(ISymbol obj) => 0;
                                              }
                                          }
                                          """;

    [Fact]
    public Task ShouldReportSymbolEqualityComparerDefaultEquals() =>
        VerifyAsync($$"""
                      {{RoslynPolyfill}}

                      public class C {
                          void M(Microsoft.CodeAnalysis.ISymbol symbol1, Microsoft.CodeAnalysis.ISymbol symbol2) {
                              if ([|Microsoft.CodeAnalysis.SymbolEqualityComparer.Default.Equals(symbol1, symbol2)|]) { }
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportSymbolEqualityComparerIncludeNullabilityEquals() =>
        VerifyAsync($$"""
                      {{RoslynPolyfill}}

                      public class C {
                          void M(Microsoft.CodeAnalysis.ISymbol symbol1, Microsoft.CodeAnalysis.ISymbol symbol2) {
                              var result = [|Microsoft.CodeAnalysis.SymbolEqualityComparer.IncludeNullability.Equals(symbol1, symbol2)|];
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportWithTypeSymbols() =>
        VerifyAsync($$"""
                      {{RoslynPolyfill}}

                      public class C {
                          void M(Microsoft.CodeAnalysis.ITypeSymbol type1, Microsoft.CodeAnalysis.ITypeSymbol type2) {
                              if ([|Microsoft.CodeAnalysis.SymbolEqualityComparer.Default.Equals(type1, type2)|]) { }
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenRoslynNotReferenced() =>
        VerifyAsync("""
                    public class C {
                        void M(object obj1, object obj2) {
                            if (obj1.Equals(obj2)) { }
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportOtherEqualityComparers() =>
        VerifyAsync($$"""
                      {{RoslynPolyfill}}

                      public class C {
                          void M(string s1, string s2) {
                              if (System.Collections.Generic.EqualityComparer<string>.Default.Equals(s1, s2)) { }
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportGetHashCode() =>
        VerifyAsync($$"""
                      {{RoslynPolyfill}}

                      public class C {
                          void M(Microsoft.CodeAnalysis.ISymbol symbol) {
                              var hash = Microsoft.CodeAnalysis.SymbolEqualityComparer.Default.GetHashCode(symbol);
                          }
                      }
                      """);
}
