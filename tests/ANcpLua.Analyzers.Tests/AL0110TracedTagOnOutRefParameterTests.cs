using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0110: [TracedTag] on out or ref parameters.
/// </summary>
public sealed partial class Al0110TracedTagOnOutRefParameterTests : AnalyzerTest<Al0110TracedTagOnOutRefParameterAnalyzer> {
    private const string Stubs = """
                                 namespace Qyl.Instrumentation.Instrumentation {
                                     [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method)]
                                     public class TracedAttribute : System.Attribute {
                                         public TracedAttribute() { }
                                         public TracedAttribute(string activitySourceName) { }
                                         public string ActivitySourceName { get; set; }
                                     }

                                     [System.AttributeUsage(System.AttributeTargets.Parameter)]
                                     public class TracedTagAttribute : System.Attribute {
                                         public TracedTagAttribute() { }
                                         public TracedTagAttribute(string name) { }
                                     }
                                 }
                                 """;

    [Fact]
    public Task ShouldReportOnOutParameter() =>
        VerifyAsync($$"""
                      {{Stubs}}

                      public class OrderService {
                          [Qyl.Instrumentation.Instrumentation.Traced("MyApp")]
                          public bool TryParse(string input, [Qyl.Instrumentation.Instrumentation.TracedTag] out int {|AL0110:result|}) {
                              result = 0;
                              return true;
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportOnRefParameter() =>
        VerifyAsync($$"""
                      {{Stubs}}

                      public class OrderService {
                          [Qyl.Instrumentation.Instrumentation.Traced("MyApp")]
                          public void Update([Qyl.Instrumentation.Instrumentation.TracedTag] ref int {|AL0110:value|}) {
                              value = 42;
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportOnNormalParameter() =>
        VerifyAsync($$"""
                      {{Stubs}}

                      public class OrderService {
                          [Qyl.Instrumentation.Instrumentation.Traced("MyApp")]
                          public void Process([Qyl.Instrumentation.Instrumentation.TracedTag] string orderId) { }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportOnParameterWithoutTracedTag() =>
        VerifyAsync($$"""
                      {{Stubs}}

                      public class OrderService {
                          [Qyl.Instrumentation.Instrumentation.Traced("MyApp")]
                          public bool TryParse(string input, out int result) {
                              result = 0;
                              return true;
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportOnOutParameterEvenWithoutTracedOnMethod() =>
        VerifyAsync($$"""
                      {{Stubs}}

                      public class OrderService {
                          public bool TryParse(string input, [Qyl.Instrumentation.Instrumentation.TracedTag] out int {|AL0110:result|}) {
                              result = 0;
                              return true;
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportOnInParameter() =>
        VerifyAsync($$"""
                      {{Stubs}}

                      public class OrderService {
                          [Qyl.Instrumentation.Instrumentation.Traced("MyApp")]
                          public void Process([Qyl.Instrumentation.Instrumentation.TracedTag] in int value) { }
                      }
                      """);
}
