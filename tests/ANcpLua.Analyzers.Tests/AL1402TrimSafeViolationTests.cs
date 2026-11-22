using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1402: Code marked [TrimSafe] must not call methods with [RequiresUnreferencedCode].
/// </summary>
public sealed partial class Al1402TrimSafeViolationTests : AnalyzerTest<Al1402TrimSafeViolationAnalyzer> {
    private const string AttributeStubs = """
                                          namespace ANcpLua.AotTesting {
                                              [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method)]
                                              public class TrimSafeAttribute : System.Attribute { }
                                          }

                                          namespace System.Diagnostics.CodeAnalysis {
                                              [System.AttributeUsage(System.AttributeTargets.Method)]
                                              public class RequiresUnreferencedCodeAttribute : System.Attribute {
                                                  public RequiresUnreferencedCodeAttribute(string message) { }
                                              }
                                          }
                                          """;

    [Fact]
    public Task ShouldReportWhenTrimSafeMethodCallsRequiresUnreferencedCode() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      public class C {
                          [ANcpLua.AotTesting.TrimSafe]
                          public void SafeMethod() {
                              {|AL1402:ReflectionMethod()|};
                          }

                          [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Uses reflection")]
                          public void ReflectionMethod() { }
                      }
                      """);

    [Fact]
    public Task ShouldReportWhenTrimSafeClassCallsRequiresUnreferencedCodeOnOtherType() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      [ANcpLua.AotTesting.TrimSafe]
                      public class C {
                          public void SafeMethod() {
                              var helper = new Helper();
                              {|AL1402:helper.ReflectionMethod()|};
                          }
                      }

                      public class Helper {
                          [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Uses reflection")]
                          public void ReflectionMethod() { }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenTrimSafeCallsUnannotatedMethod() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      public class C {
                          [ANcpLua.AotTesting.TrimSafe]
                          public void SafeMethod() {
                              OtherMethod();
                          }

                          public void OtherMethod() { }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenUnmarkedCodeCallsRequiresUnreferencedCode() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      public class C {
                          public void Caller() {
                              ReflectionMethod();
                          }

                          [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Uses reflection")]
                          public void ReflectionMethod() { }
                      }
                      """);

    /// <summary>
    ///     Pins current behaviour: only the calling method and its immediate containing type are checked,
    ///     so a type nested inside a [TrimSafe] type is not covered (AL1404 walks all containing types).
    /// </summary>
    [Fact]
    public Task ShouldNotReportInTypeNestedInsideTrimSafeType() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      [ANcpLua.AotTesting.TrimSafe]
                      public class Outer {
                          public class Inner {
                              public void Caller() {
                                  ReflectionMethod();
                              }

                              [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Uses reflection")]
                              public void ReflectionMethod() { }
                          }
                      }
                      """);
}
