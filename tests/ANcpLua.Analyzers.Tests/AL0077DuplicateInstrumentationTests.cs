using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0077: Duplicate instrumentation - method has both auto and manual tracing.
/// </summary>
public sealed partial class Al0077DuplicateInstrumentationTests : AnalyzerTest<Al0077DuplicateInstrumentationAnalyzer> {
    private const string Stubs = """
                                 namespace Qyl.Instrumentation.Instrumentation {
                                     [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method)]
                                     public class TracedAttribute : System.Attribute {
                                         public TracedAttribute() { }
                                         public TracedAttribute(string activitySourceName) { }
                                         public string ActivitySourceName { get; set; }
                                     }
                                 }

                                 namespace System.Diagnostics {
                                     public class Activity : System.IDisposable {
                                         public void Dispose() { }
                                     }

                                     public class ActivitySource {
                                         public ActivitySource(string name) { }
                                         public Activity? StartActivity(string name) => null;
                                         public Activity? StartActivity(string name, ActivityKind kind) => null;
                                     }

                                     public enum ActivityKind {
                                         Internal,
                                         Server,
                                         Client,
                                         Producer,
                                         Consumer
                                     }
                                 }
                                 """;

    [Fact]
    public Task ShouldReportWhenMethodHasTracedAttributeAndStartActivityCall() =>
        VerifyAsync($$"""
                      {{Stubs}}

                      public class OrderService {
                          private static readonly System.Diagnostics.ActivitySource Source = new("MyApp");

                          {|AL0077:[Qyl.Instrumentation.Instrumentation.Traced("MyApp")]
                          public void ProcessOrder() {
                              using var activity = Source.StartActivity("ProcessOrder");
                          }|}
                      }
                      """);

    [Fact]
    public Task ShouldReportWhenContainingTypeHasTracedAttributeAndMethodHasStartActivity() =>
        VerifyAsync($$"""
                      {{Stubs}}

                      [Qyl.Instrumentation.Instrumentation.Traced("MyApp")]
                      public class OrderService {
                          private static readonly System.Diagnostics.ActivitySource Source = new("MyApp");

                          {|AL0077:public void ProcessOrder() {
                              using var activity = Source.StartActivity("ProcessOrder");
                          }|}
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenOnlyTracedAttribute() =>
        VerifyAsync($$"""
                      {{Stubs}}

                      public class OrderService {
                          [Qyl.Instrumentation.Instrumentation.Traced("MyApp")]
                          public void ProcessOrder() {
                              // No manual Activity.StartActivity call
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenOnlyManualStartActivity() =>
        VerifyAsync($$"""
                      {{Stubs}}

                      public class OrderService {
                          private static readonly System.Diagnostics.ActivitySource Source = new("MyApp");

                          public void ProcessOrder() {
                              using var activity = Source.StartActivity("ProcessOrder");
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenNoInstrumentation() =>
        VerifyAsync($$"""
                      {{Stubs}}

                      public class OrderService {
                          public void ProcessOrder() {
                              // Plain method, no instrumentation
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportWhenStartActivityWithKindParameter() =>
        VerifyAsync($$"""
                      {{Stubs}}

                      public class OrderService {
                          private static readonly System.Diagnostics.ActivitySource Source = new("MyApp");

                          {|AL0077:[Qyl.Instrumentation.Instrumentation.Traced("MyApp")]
                          public void ProcessOrder() {
                              using var activity = Source.StartActivity("ProcessOrder", System.Diagnostics.ActivityKind.Server);
                          }|}
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenDifferentMethodHasStartActivity() =>
        VerifyAsync($$"""
                      {{Stubs}}

                      public class OrderService {
                          private static readonly System.Diagnostics.ActivitySource Source = new("MyApp");

                          [Qyl.Instrumentation.Instrumentation.Traced("MyApp")]
                          public void ProcessOrder() {
                              // No StartActivity here
                          }

                          public void OtherMethod() {
                              using var activity = Source.StartActivity("Other");
                          }
                      }
                      """);
}
