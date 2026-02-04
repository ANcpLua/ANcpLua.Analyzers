using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0042: [AotTest]/[TrimTest] methods should return 100 to indicate success.
/// </summary>
public sealed partial class Al0042AotTestExitCode100Tests : AnalyzerTest<Al0042AotTestExitCode100Analyzer> {
    private const string AttributeStubs = """
                                          namespace ANcpLua.AotTesting {
                                              [System.AttributeUsage(System.AttributeTargets.Method)]
                                              public class AotTestAttribute : System.Attribute { }
                                              [System.AttributeUsage(System.AttributeTargets.Method)]
                                              public class TrimTestAttribute : System.Attribute { }
                                          }
                                          """;

    [Fact]
    public Task ShouldNotReportWhenReturning100() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      public class C {
                          [ANcpLua.AotTesting.AotTest]
                          public int TestMethod() {
                              return 100;
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenHasSuccessAndFailureReturns() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      public class C {
                          [ANcpLua.AotTesting.AotTest]
                          public int TestMethod() {
                              if (SomethingFailed())
                                  return 1;
                              return 100;
                          }

                          private bool SomethingFailed() => false;
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenHasMultipleFailureCodesAndSuccess() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      public class C {
                          [ANcpLua.AotTesting.AotTest]
                          public int TestMethod() {
                              if (FirstCheck())
                                  return 1;
                              if (SecondCheck())
                                  return 2;
                              return 100;
                          }

                          private bool FirstCheck() => false;
                          private bool SecondCheck() => false;
                      }
                      """);

    [Fact]
    public Task ShouldReportWhenNoReturn100Exists() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      public class C {
                          [ANcpLua.AotTesting.AotTest]
                          public int {|AL0042:TestMethod|}() {
                              return 1;
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportWhenOnlyReturning0() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      public class C {
                          [ANcpLua.AotTesting.AotTest]
                          public int {|AL0042:TestMethod|}() {
                              return 0;
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportExpressionBodyNotReturning100() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      public class C {
                          [ANcpLua.AotTesting.AotTest]
                          public int TestMethod() => {|AL0042:0|};
                      }
                      """);

    [Fact]
    public Task ShouldNotReportExpressionBodyReturning100() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      public class C {
                          [ANcpLua.AotTesting.AotTest]
                          public int TestMethod() => 100;
                      }
                      """);

    [Fact]
    public Task ShouldNotReportForNonIntReturningMethod() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      public class C {
                          [ANcpLua.AotTesting.AotTest]
                          public void TestMethod() {
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportForMethodWithoutAttribute() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      public class C {
                          public int TestMethod() {
                              return 0;
                          }
                      }
                      """);

    [Fact]
    public Task ShouldWorkWithTrimTestAttribute() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      public class C {
                          [ANcpLua.AotTesting.TrimTest]
                          public int TestMethod() {
                              return 100;
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportTrimTestWhenNoReturn100() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      public class C {
                          [ANcpLua.AotTesting.TrimTest]
                          public int {|AL0042:TestMethod|}() {
                              return 42;
                          }
                      }
                      """);
}
