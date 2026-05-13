using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
/// Tests for AL0040: Use attribute argument extraction extensions.
/// </summary>
public sealed partial class Al0040UseAttributeExtensionsTests : AnalyzerTest<Al0040UseAttributeExtensionsAnalyzer> {
    private const string RoslynPolyfill = """
                                          namespace Microsoft.CodeAnalysis {
                                              public interface AttributeData {
                                                  public System.Collections.Immutable.ImmutableArray<TypedConstant> ConstructorArguments { get; }
                                              }
                                              public struct TypedConstant {
                                                  public object? Value { get; }
                                              }
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
    public Task ShouldReportConstructorArgumentsValue() =>
        VerifyAsync($$"""
                      using System.Linq;
                      {{RoslynPolyfill}}

                      public class C {
                          object? M(Microsoft.CodeAnalysis.AttributeData attr) {
                              return [|attr.ConstructorArguments[0].Value|];
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportConstructorArgumentsIndexing() =>
        VerifyAsync($$"""
                      using System.Linq;
                      {{RoslynPolyfill}}

                      public class C {
                          object? M(Microsoft.CodeAnalysis.AttributeData attr) {
                              return attr.ConstructorArguments[0];
                          }
                      }
                      """);
}

/// <summary>
///     Code fix tests for AL0040: Use typed attribute argument extraction extensions.
/// </summary>
public sealed partial class Al0040UseAttributeExtensionsCodeFixTests
    : CodeFixTest<Al0040UseAttributeExtensionsAnalyzer, Al0040UseAttributeExtensionsCodeFixProvider> {
    private const string RoslynPolyfill = """
                                          namespace Microsoft.CodeAnalysis {
                                              public interface AttributeData {
                                                  public System.Collections.Immutable.ImmutableArray<TypedConstant> ConstructorArguments { get; }
                                              }
                                              public struct TypedConstant {
                                                  public object? Value { get; }
                                              }
                                          }
                                          namespace System.Collections.Immutable {
                                              public struct ImmutableArray<T> : System.Collections.Generic.IEnumerable<T> {
                                                  public int Length => 0;
                                                  public T this[int index] => default!;
                                                  public System.Collections.Generic.IEnumerator<T> GetEnumerator() => null!;
                                                  System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => null!;
                                              }
                                          }
                                          public static class AttributeExtensions {
                                              public static T GetConstructorArgument<T>(this Microsoft.CodeAnalysis.AttributeData attr, int index) => default!;
                                          }
                                          """;

    [Fact]
    public Task ShouldFixConstructorArgumentValueWhenCastProvidesType() =>
        VerifyAsync(
            $$"""
              {{RoslynPolyfill}}

              public class C {
                  string M(Microsoft.CodeAnalysis.AttributeData attr) {
                      return (string)[|attr.ConstructorArguments[0].Value|];
                  }
              }
              """,
            $$"""
              {{RoslynPolyfill}}

              public class C {
                  string M(Microsoft.CodeAnalysis.AttributeData attr) {
                      return (string)attr.GetConstructorArgument<string>(0);
                  }
              }
              """);
}
