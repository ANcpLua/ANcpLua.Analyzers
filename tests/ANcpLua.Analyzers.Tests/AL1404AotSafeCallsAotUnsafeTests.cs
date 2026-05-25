using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1404: [AotSafe] code must not call [AotUnsafe] code.
/// </summary>
public sealed partial class Al1404AotSafeCallsAotUnsafeTests : AnalyzerTest<Al1404AotSafeCallsAotUnsafeAnalyzer> {
    private const string AttributeStubs = """
                                          namespace ANcpLua.AotTesting {
                                              [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method)]
                                              public class AotSafeAttribute : System.Attribute { }

                                              [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method)]
                                              public class AotUnsafeAttribute : System.Attribute {
                                                  public AotUnsafeAttribute() { }
                                                  public AotUnsafeAttribute(string reason) { }
                                              }
                                          }
                                          """;

    [Fact]
    public Task ShouldReportWhenAotSafeCallsAotUnsafe() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      public class C {
                          [ANcpLua.AotTesting.AotSafe]
                          public void SafeMethod() {
                              {|AL1404:UnsafeMethod()|};
                          }

                          [ANcpLua.AotTesting.AotUnsafe("Uses reflection")]
                          public void UnsafeMethod() { }
                      }
                      """);

    [Fact]
    public Task ShouldReportWhenAotSafeCallsAotUnsafeOnDifferentClass() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      public class C {
                          [ANcpLua.AotTesting.AotSafe]
                          public void SafeMethod() {
                              {|AL1404:UnsafeHelper.UnsafeMethod()|};
                          }
                      }

                      [ANcpLua.AotTesting.AotUnsafe("Uses reflection")]
                      public static class UnsafeHelper {
                          public static void UnsafeMethod() { }
                      }
                      """);

    [Fact]
    public Task ShouldReportWhenAotSafeClassCallsAotUnsafe() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      [ANcpLua.AotTesting.AotSafe]
                      public class SafeClass {
                          public void Method() {
                              {|AL1404:UnsafeHelper.UnsafeMethod()|};
                          }
                      }

                      [ANcpLua.AotTesting.AotUnsafe]
                      public static class UnsafeHelper {
                          public static void UnsafeMethod() { }
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
    public Task ShouldNotReportWhenNonAotSafeCallsAotUnsafe() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      public class C {
                          public void RegularMethod() {
                              UnsafeMethod();
                          }

                          [ANcpLua.AotTesting.AotUnsafe]
                          public void UnsafeMethod() { }
                      }
                      """);

    [Fact]
    public Task ShouldReportMultipleViolationsInSameMethod() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      public class C {
                          [ANcpLua.AotTesting.AotSafe]
                          public void SafeMethod() {
                              {|AL1404:UnsafeMethod1()|};
                              {|AL1404:UnsafeMethod2()|};
                          }

                          [ANcpLua.AotTesting.AotUnsafe]
                          public void UnsafeMethod1() { }

                          [ANcpLua.AotTesting.AotUnsafe]
                          public void UnsafeMethod2() { }
                      }
                      """);
}
