using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0044: [AotSafe] code must not call methods with [RequiresDynamicCode].
/// </summary>
public sealed partial class Al0044AnalyzerTests : AnalyzerTest<Al0044AotSafeViolationAnalyzer> {
    private const string AttributeStubs = """
                                          namespace ANcpLua.AotTesting {
                                              [System.AttributeUsage(System.AttributeTargets.Method)]
                                              public class AotSafeAttribute : System.Attribute { }
                                          }

                                          namespace System.Diagnostics.CodeAnalysis {
                                              [System.AttributeUsage(System.AttributeTargets.Method)]
                                              public class RequiresDynamicCodeAttribute : System.Attribute {
                                                  public RequiresDynamicCodeAttribute(string message) { }
                                              }
                                          }
                                          """;

    [Fact]
    public Task ShouldReportWhenAotSafeCallsRequiresDynamicCode() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      public class C {
                          [ANcpLua.AotTesting.AotSafe]
                          public void SafeMethod() {
                              {|AL0044:DynamicMethod()|};
                          }

                          [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Uses runtime code generation")]
                          public void DynamicMethod() { }
                      }
                      """);

    [Fact]
    public Task ShouldReportWhenAotSafeCallsRequiresDynamicCodeOnDifferentClass() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      public class C {
                          [ANcpLua.AotTesting.AotSafe]
                          public void SafeMethod() {
                              var helper = new Helper();
                              {|AL0044:helper.DynamicMethod()|};
                          }
                      }

                      public class Helper {
                          [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Uses runtime code generation")]
                          public void DynamicMethod() { }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenAotSafeCallsSafeMethod() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      public class C {
                          [ANcpLua.AotTesting.AotSafe]
                          public void SafeMethod() {
                              AnotherSafeMethod();
                          }

                          public void AnotherSafeMethod() { }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenNonAotSafeCallsRequiresDynamicCode() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      public class C {
                          public void RegularMethod() {
                              DynamicMethod();
                          }

                          [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Uses runtime code generation")]
                          public void DynamicMethod() { }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenAotSafeMethodHasNoMethodCalls() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      public class C {
                          [ANcpLua.AotTesting.AotSafe]
                          public int SafeMethod() {
                              return 100;
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenCallingAotSafeMethod() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      public class C {
                          [ANcpLua.AotTesting.AotSafe]
                          public void SafeMethod1() {
                              SafeMethod2();
                          }

                          [ANcpLua.AotTesting.AotSafe]
                          public void SafeMethod2() { }
                      }
                      """);

    [Fact]
    public Task ShouldReportMultipleViolationsInSameMethod() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      public class C {
                          [ANcpLua.AotTesting.AotSafe]
                          public void SafeMethod() {
                              {|AL0044:DynamicMethod1()|};
                              {|AL0044:DynamicMethod2()|};
                          }

                          [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Uses runtime code generation")]
                          public void DynamicMethod1() { }

                          [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Uses runtime code generation")]
                          public void DynamicMethod2() { }
                      }
                      """);
}
