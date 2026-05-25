using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1503: Avoid NormalizeWhitespace in source generators.
/// </summary>
public sealed partial class Al1503NormalizeWhitespaceTests : AnalyzerTest<Al1503NormalizeWhitespaceAnalyzer> {
    private const string SyntaxStubs = """
                                       namespace Microsoft.CodeAnalysis {
                                           public abstract class SyntaxNode {
                                               public SyntaxNode NormalizeWhitespace() => this;
                                               public SyntaxNode NormalizeWhitespace(string indentation) => this;
                                               public string ToFullString() => "";
                                           }
                                           public static class SyntaxFactory {
                                               public static SyntaxNode ParseExpression(string text) => null;
                                           }
                                       }
                                       """;

    [Fact]
    public Task ShouldReportNormalizeWhitespaceCall() =>
        VerifyAsync($$"""
                      {{SyntaxStubs}}

                      public class Generator {
                          public void Generate() {
                              var node = Microsoft.CodeAnalysis.SyntaxFactory.ParseExpression("x + y");
                              var normalized = node.[|NormalizeWhitespace()|];
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportNormalizeWhitespaceWithArgs() =>
        VerifyAsync($$"""
                      {{SyntaxStubs}}

                      public class Generator {
                          public void Generate() {
                              var node = Microsoft.CodeAnalysis.SyntaxFactory.ParseExpression("x");
                              var normalized = node.[|NormalizeWhitespace("  ")|];
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportInCallChainToFullString() =>
        VerifyAsync($$"""
                      {{SyntaxStubs}}

                      public class Generator {
                          public void Generate() {
                              var text = Microsoft.CodeAnalysis.SyntaxFactory.ParseExpression("x").NormalizeWhitespace().ToFullString();
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportUnrelatedMethodNamedNormalizeWhitespace() =>
        VerifyAsync("""
                    public class MyClass {
                        public string NormalizeWhitespace() => "";
                    }

                    public class Consumer {
                        public void M() {
                            var c = new MyClass();
                            c.NormalizeWhitespace();
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportWhenRoslynNotReferenced() =>
        VerifyAsync("""
                    public class Generator {
                        public void Generate() { }
                    }
                    """);
}

/// <summary>
///     Code fix tests for AL1503: Removes NormalizeWhitespace() call.
/// </summary>
public sealed partial class Al1503CodeFixTests : CodeFixTest<Al1503NormalizeWhitespaceAnalyzer, Al1503NormalizeWhitespaceCodeFixProvider> {
    private const string SyntaxStubs = """
                                       namespace Microsoft.CodeAnalysis {
                                           public abstract class SyntaxNode {
                                               public SyntaxNode NormalizeWhitespace() => this;
                                               public string ToFullString() => "";
                                           }
                                           public static class SyntaxFactory {
                                               public static SyntaxNode ParseExpression(string text) => null;
                                           }
                                       }
                                       """;

    [Fact]
    public Task ShouldRemoveNormalizeWhitespace() =>
        VerifyAsync($$"""
                      {{SyntaxStubs}}

                      public class Generator {
                          public void Generate() {
                              var node = Microsoft.CodeAnalysis.SyntaxFactory.ParseExpression("x");
                              var normalized = node.[|NormalizeWhitespace()|];
                          }
                      }
                      """,
            $$"""
              {{SyntaxStubs}}

              public class Generator {
                  public void Generate() {
                      var node = Microsoft.CodeAnalysis.SyntaxFactory.ParseExpression("x");
                      var normalized = node;
                  }
              }
              """);
}
