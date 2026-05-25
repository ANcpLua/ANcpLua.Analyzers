using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1201: Use HasAttribute extension instead of GetAttributes() patterns.
/// </summary>
public sealed partial class Al1201UseHasAttributeTests : AnalyzerTest<Al1201UseHasAttributeAnalyzer> {
    private const string RoslynPolyfill = """
                                          namespace Microsoft.CodeAnalysis {
                                              public interface ISymbol {
                                                  System.Collections.Immutable.ImmutableArray<AttributeData> GetAttributes();
                                              }
                                              public interface INamedTypeSymbol : ISymbol { }
                                              public class AttributeData {
                                                  public INamedTypeSymbol? AttributeClass { get; }
                                                  public System.Collections.Immutable.ImmutableArray<TypedConstant> ConstructorArguments { get; }
                                                  public System.Collections.Immutable.ImmutableArray<System.Collections.Generic.KeyValuePair<string, TypedConstant>> NamedArguments { get; }
                                                  public SyntaxReference? ApplicationSyntaxReference { get; }
                                              }
                                              public struct TypedConstant {
                                                  public object? Value { get; }
                                              }
                                              public class SyntaxReference { }
                                          }
                                          namespace System.Collections.Immutable {
                                              public struct ImmutableArray<T> : System.Collections.Generic.IEnumerable<T> {
                                                  public int Length => 0;
                                                  public T this[int index] => default!;
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
    public Task ShouldNotReportGetAttributesAnyWithoutPredicate() =>
        VerifyAsync($$"""
                      using System.Linq;
                      {{RoslynPolyfill}}

                      public class C {
                          bool M(Microsoft.CodeAnalysis.ISymbol symbol) {
                              return symbol.GetAttributes().Any();
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportGetAttributesFirstOrDefault() =>
        VerifyAsync($$"""
                      using System.Linq;
                      {{RoslynPolyfill}}

                      public class C {
                          object? M(Microsoft.CodeAnalysis.ISymbol symbol) {
                              return symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToString() == "Test");
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportGetAttributesWhere() =>
        VerifyAsync($$"""
                      using System.Linq;
                      {{RoslynPolyfill}}

                      public class C {
                          object[]? M(Microsoft.CodeAnalysis.ISymbol symbol) {
                              return symbol.GetAttributes().Where(a => a.AttributeClass?.ToString() == "Test").ToArray();
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportGetAttributesCount() =>
        VerifyAsync($$"""
                      using System.Linq;
                      {{RoslynPolyfill}}

                      public class C {
                          bool M(Microsoft.CodeAnalysis.ISymbol symbol) {
                              return symbol.GetAttributes().Count(a => a.AttributeClass?.ToString() == "Test") > 0;
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

    [Fact]
    public Task ShouldNotReportForeachWhenExtractingConstructorArguments() =>
        VerifyAsync($$"""
                      {{RoslynPolyfill}}

                      public class C {
                          string? M(Microsoft.CodeAnalysis.ISymbol symbol) {
                              foreach (var attr in symbol.GetAttributes()) {
                                  if (attr.AttributeClass?.ToString() == "GetAttribute") {
                                      // Extracting data - not just checking existence
                                      if (attr.ConstructorArguments.Length > 0) {
                                          return attr.ConstructorArguments[0].Value?.ToString();
                                      }
                                  }
                              }
                              return null;
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportForeachWhenExtractingNamedArguments() =>
        VerifyAsync($$"""
                      {{RoslynPolyfill}}

                      public class C {
                          void M(Microsoft.CodeAnalysis.ISymbol symbol) {
                              foreach (var attr in symbol.GetAttributes()) {
                                  if (attr.AttributeClass?.ToString() == "MyAttribute") {
                                      // Extracting named arguments - valid use case
                                      foreach (var namedArg in attr.NamedArguments) {
                                          System.Console.WriteLine(namedArg.Key);
                                      }
                                  }
                              }
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportForeachWhenExtractingSyntaxReference() =>
        VerifyAsync($$"""
                      {{RoslynPolyfill}}

                      public class C {
                          object? M(Microsoft.CodeAnalysis.ISymbol symbol) {
                              foreach (var attr in symbol.GetAttributes()) {
                                  if (attr.AttributeClass?.ToString() == "MyAttribute") {
                                      // Getting location - valid use case
                                      return attr.ApplicationSyntaxReference;
                                  }
                              }
                              return null;
                          }
                      }
                      """);
}
