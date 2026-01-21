using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0029: Use HasAttribute extension instead of GetAttributes() patterns.
/// </summary>
public sealed partial class Al0029UseHasAttributeTests : AnalyzerTest<Al0029UseHasAttributeAnalyzer> {
    private const string RoslynPolyfill = """
                                          namespace Microsoft.CodeAnalysis {
                                              public interface ISymbol {
                                                  System.Collections.Immutable.ImmutableArray<AttributeData> GetAttributes();
                                              }
                                              public interface INamedTypeSymbol : ISymbol { }
                                              public class AttributeData {
                                                  public INamedTypeSymbol? AttributeClass { get; }
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
    public Task ShouldReportGetAttributesAny() =>
        VerifyAsync($$"""
                      using System.Linq;
                      {{RoslynPolyfill}}

                      public class C {
                          bool M(Microsoft.CodeAnalysis.ISymbol symbol) {
                              return [|symbol.GetAttributes().Any(a => a.AttributeClass?.ToString() == "Test")|];
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportGetAttributesFirstOrDefault() =>
        VerifyAsync($$"""
                      using System.Linq;
                      {{RoslynPolyfill}}

                      public class C {
                          object? M(Microsoft.CodeAnalysis.ISymbol symbol) {
                              return [|symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToString() == "Test")|];
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportForeachOverGetAttributes() =>
        VerifyAsync($$"""
                      {{RoslynPolyfill}}

                      public class C {
                          bool M(Microsoft.CodeAnalysis.ISymbol symbol) {
                              [|foreach (var attr in symbol.GetAttributes()) {
                                  if (attr.AttributeClass?.ToString() == "Test") {
                                      return true;
                                  }
                              }|]
                              return false;
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportForeachWithoutAttributeClassCheck() =>
        VerifyAsync($$"""
                      {{RoslynPolyfill}}

                      public class C {
                          void M(Microsoft.CodeAnalysis.ISymbol symbol) {
                              foreach (var attr in symbol.GetAttributes()) {
                                  System.Console.WriteLine(attr);
                              }
                          }
                      }
                      """);
}
