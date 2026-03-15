using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0107: Orphaned [TracedTag] - tag on parameter without [Traced] in scope.
/// </summary>
public sealed partial class Al0107OrphanedTracedTagTests : AnalyzerTest<Al0107OrphanedTracedTagAnalyzer> {
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
    public Task ShouldReportWhenNoTracedOnMethodOrClass() =>
        VerifyAsync($$"""
                      {{Stubs}}

                      public class OrderService {
                          public void Process([Qyl.Instrumentation.Instrumentation.TracedTag] string {|AL0107:orderId|}) { }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenMethodHasTraced() =>
        VerifyAsync($$"""
                      {{Stubs}}

                      public class OrderService {
                          [Qyl.Instrumentation.Instrumentation.Traced("MyApp")]
                          public void Process([Qyl.Instrumentation.Instrumentation.TracedTag] string orderId) { }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenClassHasTraced() =>
        VerifyAsync($$"""
                      {{Stubs}}

                      [Qyl.Instrumentation.Instrumentation.Traced("MyApp")]
                      public class OrderService {
                          public void Process([Qyl.Instrumentation.Instrumentation.TracedTag] string orderId) { }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenBaseClassHasTraced() =>
        VerifyAsync($$"""
                      {{Stubs}}

                      [Qyl.Instrumentation.Instrumentation.Traced("MyApp")]
                      public class BaseService { }

                      public class OrderService : BaseService {
                          public void Process([Qyl.Instrumentation.Instrumentation.TracedTag] string orderId) { }
                      }
                      """);

    [Fact]
    public Task ShouldReportMultipleOrphanedTags() =>
        VerifyAsync($$"""
                      {{Stubs}}

                      public class OrderService {
                          public void Process(
                              [Qyl.Instrumentation.Instrumentation.TracedTag] string {|AL0107:orderId|},
                              [Qyl.Instrumentation.Instrumentation.TracedTag] int {|AL0107:count|}) { }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportParameterWithoutTracedTag() =>
        VerifyAsync($$"""
                      {{Stubs}}

                      public class OrderService {
                          public void Process(string orderId) { }
                      }
                      """);
}
