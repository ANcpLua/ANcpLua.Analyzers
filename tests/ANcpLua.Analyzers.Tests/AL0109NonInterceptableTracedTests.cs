using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0109: Non-interceptable [Traced] on abstract, extern, or partial definition methods.
/// </summary>
public sealed partial class Al0109NonInterceptableTracedTests : AnalyzerTest<Al0109NonInterceptableTracedAnalyzer> {
    private const string Stubs = """
                                 namespace Qyl.Instrumentation.Instrumentation {
                                     [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method)]
                                     public class TracedAttribute : System.Attribute {
                                         public TracedAttribute() { }
                                         public TracedAttribute(string activitySourceName) { }
                                         public string ActivitySourceName { get; set; }
                                     }
                                 }
                                 """;

    [Fact]
    public Task ShouldReportOnAbstractMethod() =>
        VerifyAsync($$"""
                      {{Stubs}}

                      public abstract class BaseService {
                          [Qyl.Instrumentation.Instrumentation.Traced("MyApp")]
                          public abstract void {|AL0109:ProcessAsync|}();
                      }
                      """);

    [Fact]
    public Task ShouldReportOnExternMethod() =>
        VerifyAsync($$"""
                      using System.Runtime.InteropServices;
                      {{Stubs}}

                      public class NativeService {
                          [Qyl.Instrumentation.Instrumentation.Traced("MyApp")]
                          [DllImport("native.dll")]
                          public static extern void {|AL0109:NativeCall|}();
                      }
                      """);

    [Fact]
    public Task ShouldReportOnPartialDefinitionMethod() =>
        VerifyAsync($$"""
                      {{Stubs}}

                      public partial class OrderService {
                          [Qyl.Instrumentation.Instrumentation.Traced("MyApp")]
                          public partial void {|AL0109:Process|}();
                      }

                      public partial class OrderService {
                          public partial void Process() { }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportOnConcreteMethod() =>
        VerifyAsync($$"""
                      {{Stubs}}

                      public class OrderService {
                          [Qyl.Instrumentation.Instrumentation.Traced("MyApp")]
                          public void Process() { }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportOnMethodWithoutTracedAttribute() =>
        VerifyAsync($$"""
                      {{Stubs}}

                      public abstract class BaseService {
                          public abstract void ProcessAsync();
                      }
                      """);

    [Fact]
    public Task ShouldNotReportOnClassLevelTracedWithAbstractMethod() =>
        VerifyAsync($$"""
                      {{Stubs}}

                      [Qyl.Instrumentation.Instrumentation.Traced("MyApp")]
                      public abstract class BaseService {
                          public abstract void ProcessAsync();
                      }
                      """);
}
