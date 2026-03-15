using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0108: Redundant [NoTrace] - method has [NoTrace] but declaring type has no [Traced].
/// </summary>
public sealed partial class Al0108RedundantNoTraceTests : AnalyzerTest<Al0108RedundantNoTraceAnalyzer> {
    private const string Stubs = """
                                 namespace Qyl.Instrumentation.Instrumentation {
                                     [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method)]
                                     public class TracedAttribute : System.Attribute {
                                         public TracedAttribute() { }
                                         public TracedAttribute(string activitySourceName) { }
                                         public string ActivitySourceName { get; set; }
                                     }

                                     [System.AttributeUsage(System.AttributeTargets.Method)]
                                     public class NoTraceAttribute : System.Attribute { }
                                 }
                                 """;

    [Fact]
    public Task ShouldReportWhenNoTracedOnClass() =>
        VerifyAsync($$"""
                      {{Stubs}}

                      public class OrderService {
                          [Qyl.Instrumentation.Instrumentation.NoTrace]
                          public void {|AL0108:HelperMethod|}() { }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenClassHasTraced() =>
        VerifyAsync($$"""
                      {{Stubs}}

                      [Qyl.Instrumentation.Instrumentation.Traced("MyApp")]
                      public class OrderService {
                          [Qyl.Instrumentation.Instrumentation.NoTrace]
                          public void HelperMethod() { }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenBaseClassHasTraced() =>
        VerifyAsync($$"""
                      {{Stubs}}

                      [Qyl.Instrumentation.Instrumentation.Traced("MyApp")]
                      public class BaseService { }

                      public class OrderService : BaseService {
                          [Qyl.Instrumentation.Instrumentation.NoTrace]
                          public void HelperMethod() { }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportMethodWithoutNoTrace() =>
        VerifyAsync($$"""
                      {{Stubs}}

                      public class OrderService {
                          public void HelperMethod() { }
                      }
                      """);

    [Fact]
    public Task ShouldReportWhenNoTracedOnClassOrBaseClasses() =>
        VerifyAsync($$"""
                      {{Stubs}}

                      public class BaseService { }

                      public class OrderService : BaseService {
                          [Qyl.Instrumentation.Instrumentation.NoTrace]
                          public void {|AL0108:HelperMethod|}() { }
                      }
                      """);
}
