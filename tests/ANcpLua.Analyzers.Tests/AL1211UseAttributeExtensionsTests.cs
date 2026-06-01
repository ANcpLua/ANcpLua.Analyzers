using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
/// Tests for AL1211: Use attribute argument extraction extensions.
/// </summary>
public sealed partial class Al1211UseAttributeExtensionsTests : AnalyzerTest<Al1211UseAttributeExtensionsAnalyzer> {
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

    // AL1211 only fires when ANcpLua.Roslyn.Utilities.AttributeExtensions (which owns
    // GetConstructorArgument<T> etc.) is present and accessible. Each case appends this stub so
    // the gate is open; the dedicated ShouldNotReportWhenAttributeExtensionsNotReferenced case omits
    // it to assert the gate itself.
    private const string Stub = """
                                namespace ANcpLua.Roslyn.Utilities { internal static class AttributeExtensions { } }
                                """;

    private static Task Verify(string body) => VerifyAsync($$"""
                                                            {{body}}
                                                            {{Stub}}
                                                            """);

    [Fact]
    public Task ShouldReportConstructorArgumentsValue() =>
        Verify($$"""
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
        Verify($$"""
                 using System.Linq;
                 {{RoslynPolyfill}}

                 public class C {
                     object? M(Microsoft.CodeAnalysis.AttributeData attr) {
                         return attr.ConstructorArguments[0];
                     }
                 }
                 """);

    // The consumer scenario: a project that does not reference ANcpLua.Roslyn.Utilities has no
    // AttributeExtensions type, so AL1211 must stay silent on attribute argument access patterns.
    [Fact]
    public Task ShouldNotReportWhenAttributeExtensionsNotReferenced() =>
        VerifyAsync($$"""
                      using System.Linq;
                      {{RoslynPolyfill}}

                      public class C {
                          object? M(Microsoft.CodeAnalysis.AttributeData attr) {
                              return attr.ConstructorArguments[0].Value;
                          }
                      }
                      """);
}

/// <summary>
///     Code fix tests for AL1211: Use typed attribute argument extraction extensions.
/// </summary>
public sealed partial class Al1211UseAttributeExtensionsCodeFixTests
    : CodeFixTest<Al1211UseAttributeExtensionsAnalyzer, Al1211UseAttributeExtensionsCodeFixProvider> {
    // Polyfill stub - matches ANcpLua.Roslyn.Utilities signature. Declared in the real
    // ANcpLua.Roslyn.Utilities namespace so the analyzer's accessibility gate opens, and imported
    // so the rewritten GetConstructorArgument<T>() call resolves (the code fix adds this using when missing).
    private const string RoslynPolyfill = """
        using ANcpLua.Roslyn.Utilities;
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
        namespace ANcpLua.Roslyn.Utilities {
            public static class AttributeExtensions {
                public static T GetConstructorArgument<T>(this Microsoft.CodeAnalysis.AttributeData attr, int index) => default!;
            }
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
