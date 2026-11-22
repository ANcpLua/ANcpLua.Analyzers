using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1400: Methods decorated with [AotTest] or [TrimTest] must return int.
/// </summary>
public sealed partial class Al1400AotTestMustReturnIntTests : AnalyzerTest<Al1400AotTestMustReturnIntAnalyzer> {
    private const string AttributeStubs = """
                                          namespace ANcpLua.AotTesting {
                                              [System.AttributeUsage(System.AttributeTargets.Method)]
                                              public class AotTestAttribute : System.Attribute { }
                                              [System.AttributeUsage(System.AttributeTargets.Method)]
                                              public class TrimTestAttribute : System.Attribute { }
                                          }
                                          """;

    [Fact]
    public Task ShouldReportWhenAotTestReturnsVoid() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      public class C {
                          [ANcpLua.AotTesting.AotTest]
                          public void {|AL1400:TestMethod|}() { }
                      }
                      """);

    [Fact]
    public Task ShouldReportWhenTrimTestReturnsString() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      public class C {
                          [ANcpLua.AotTesting.TrimTest]
                          public string {|AL1400:TestMethod|}() => "100";
                      }
                      """);

    [Fact]
    public Task ShouldReportWhenAotTestReturnsTaskOfInt() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      public class C {
                          [ANcpLua.AotTesting.AotTest]
                          public System.Threading.Tasks.Task<int> {|AL1400:TestMethod|}() =>
                              System.Threading.Tasks.Task.FromResult(100);
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenAotTestReturnsInt() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      public class C {
                          [ANcpLua.AotTesting.AotTest]
                          public int AotMethod() => 100;

                          [ANcpLua.AotTesting.TrimTest]
                          public int TrimMethod() => 100;
                      }
                      """);

    [Fact]
    public Task ShouldNotReportVoidMethodWithoutTestAttribute() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      public class C {
                          public void Helper() { }
                      }
                      """);
}
