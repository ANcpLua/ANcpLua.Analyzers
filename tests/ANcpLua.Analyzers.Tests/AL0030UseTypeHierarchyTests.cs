using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0030: Use Implements/InheritsFrom extensions instead of manual loops.
/// </summary>
public sealed partial class Al0030UseTypeHierarchyTests : AnalyzerTest<Al0030UseTypeHierarchyAnalyzer> {
    private const string RoslynPolyfill = """
                                          namespace Microsoft.CodeAnalysis {
                                              public interface ISymbol { }
                                              public interface ITypeSymbol : ISymbol {
                                                  INamedTypeSymbol? BaseType { get; }
                                                  System.Collections.Immutable.ImmutableArray<INamedTypeSymbol> AllInterfaces { get; }
                                              }
                                              public interface INamedTypeSymbol : ITypeSymbol { }
                                              public class SymbolEqualityComparer : System.Collections.Generic.IEqualityComparer<ISymbol> {
                                                  public static readonly SymbolEqualityComparer Default = new();
                                                  public bool Equals(ISymbol? x, ISymbol? y) => true;
                                                  public int GetHashCode(ISymbol obj) => 0;
                                              }
                                          }
                                          namespace System.Collections.Immutable {
                                              public struct ImmutableArray<T> : System.Collections.Generic.IEnumerable<T> {
                                                  public int Length => 0;
                                                  public System.Collections.Generic.IEnumerator<T> GetEnumerator() => null!;
                                                  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => null!;
                                              }
                                          }
                                          """;

    [Fact]
    public Task ShouldReportForeachOverAllInterfaces() =>
        VerifyAsync($$"""
                      {{RoslynPolyfill}}

                      public class C {
                          bool M(Microsoft.CodeAnalysis.ITypeSymbol type, Microsoft.CodeAnalysis.INamedTypeSymbol target) {
                              [|foreach (var iface in type.AllInterfaces) {
                                  if (Microsoft.CodeAnalysis.SymbolEqualityComparer.Default.Equals(iface, target)) {
                                      return true;
                                  }
                              }|]
                              return false;
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportForeachAllInterfacesWithoutEqualityCheck() =>
        VerifyAsync($$"""
                      {{RoslynPolyfill}}

                      public class C {
                          void M(Microsoft.CodeAnalysis.ITypeSymbol type) {
                              foreach (var iface in type.AllInterfaces) {
                                  System.Console.WriteLine(iface);
                              }
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportWhileBaseTypeLoop() =>
        VerifyAsync($$"""
                      {{RoslynPolyfill}}

                      public class C {
                          bool M(Microsoft.CodeAnalysis.ITypeSymbol type, Microsoft.CodeAnalysis.INamedTypeSymbol target) {
                              var current = type.BaseType;
                              [|while (current != null) {
                                  if (Microsoft.CodeAnalysis.SymbolEqualityComparer.Default.Equals(current, target)) {
                                      return true;
                                  }
                                  current = current.BaseType;
                              }|]
                              return false;
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhileLoopWithoutBaseTypeAssignment() =>
        VerifyAsync($$"""
                      {{RoslynPolyfill}}

                      public class C {
                          void M(Microsoft.CodeAnalysis.ITypeSymbol type) {
                              var current = type.BaseType;
                              while (current != null) {
                                  System.Console.WriteLine(current);
                                  break;
                              }
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportInvertedForeachReturnValue() =>
        VerifyAsync($$"""
                      {{RoslynPolyfill}}

                      public class C {
                          bool M(Microsoft.CodeAnalysis.ITypeSymbol type, Microsoft.CodeAnalysis.INamedTypeSymbol target) {
                              foreach (var iface in type.AllInterfaces) {
                                  if (Microsoft.CodeAnalysis.SymbolEqualityComparer.Default.Equals(iface, target)) {
                                      return false;
                                  }
                              }
                              return false;
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenFollowedByVoidReturn() =>
        VerifyAsync($$"""
                      {{RoslynPolyfill}}

                      public class C {
                          void M(Microsoft.CodeAnalysis.ITypeSymbol type, Microsoft.CodeAnalysis.INamedTypeSymbol target) {
                              foreach (var iface in type.AllInterfaces) {
                                  if (Microsoft.CodeAnalysis.SymbolEqualityComparer.Default.Equals(iface, target)) {
                                      return;
                                  }
                              }
                              return;
                          }
                      }
                      """);
}
