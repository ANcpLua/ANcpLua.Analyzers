using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0124: Non-interceptable [AgentTraced] on abstract, extern, or partial definition methods.
/// </summary>
public sealed partial class Al0124NonInterceptableAgentTracedTests : AnalyzerTest<Al0124NonInterceptableAgentTracedAnalyzer> {
    private const string AgentStubs = """
                                      namespace Qyl.Instrumentation.Instrumentation {
                                          [System.AttributeUsage(System.AttributeTargets.Method)]
                                          public class AgentTracedAttribute : System.Attribute {
                                              public AgentTracedAttribute() { }
                                              public string AgentName { get; set; }
                                          }
                                      }
                                      """;

    [Fact]
    public Task ShouldReportOnAbstractMethod() =>
        VerifyAsync($$"""
                      {{AgentStubs}}

                      public abstract class BaseAgent {
                          [Qyl.Instrumentation.Instrumentation.AgentTraced(AgentName = "test")]
                          public abstract void {|AL0124:InvokeAsync|}();
                      }
                      """);

    [Fact]
    public Task ShouldReportOnExternMethod() =>
        VerifyAsync($$"""
                      using System.Runtime.InteropServices;
                      {{AgentStubs}}

                      public class NativeAgent {
                          [Qyl.Instrumentation.Instrumentation.AgentTraced]
                          [DllImport("native.dll")]
                          public static extern void {|AL0124:NativeCall|}();
                      }
                      """);

    [Fact]
    public Task ShouldReportOnPartialDefinitionMethod() =>
        VerifyAsync($$"""
                      {{AgentStubs}}

                      public partial class AgentService {
                          [Qyl.Instrumentation.Instrumentation.AgentTraced]
                          public partial void {|AL0124:Process|}();
                      }

                      public partial class AgentService {
                          public partial void Process() { }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportOnConcreteMethod() =>
        VerifyAsync($$"""
                      {{AgentStubs}}

                      public class AgentService {
                          [Qyl.Instrumentation.Instrumentation.AgentTraced(AgentName = "test")]
                          public void Process() { }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportOnMethodWithoutAttribute() =>
        VerifyAsync($$"""
                      {{AgentStubs}}

                      public abstract class BaseAgent {
                          public abstract void InvokeAsync();
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenAttributeNotReferenced() =>
        VerifyAsync("""
                    public abstract class BaseAgent {
                        public abstract void InvokeAsync();
                    }
                    """);
}

/// <summary>
///     Code fix tests for AL0124: Removes [AgentTraced] attribute from non-interceptable methods.
/// </summary>
public sealed partial class Al0124CodeFixTests : CodeFixTest<Al0124NonInterceptableAgentTracedAnalyzer, Al0124AgentTracedCodeFixProvider> {
    private const string AgentStubs = """
                                      namespace Qyl.Instrumentation.Instrumentation {
                                          [System.AttributeUsage(System.AttributeTargets.Method)]
                                          public class AgentTracedAttribute : System.Attribute {
                                              public AgentTracedAttribute() { }
                                              public string AgentName { get; set; }
                                          }
                                      }
                                      """;

    [Fact]
    public Task ShouldRemoveAgentTracedFromAbstractMethod() =>
        VerifyAsync($$"""
                      {{AgentStubs}}

                      public abstract class BaseAgent {
                          [Qyl.Instrumentation.Instrumentation.AgentTraced]
                          public abstract void {|AL0124:InvokeAsync|}();
                      }
                      """,
            $$"""
              {{AgentStubs}}

              public abstract class BaseAgent {
                  public abstract void InvokeAsync();
              }
              """);
}
