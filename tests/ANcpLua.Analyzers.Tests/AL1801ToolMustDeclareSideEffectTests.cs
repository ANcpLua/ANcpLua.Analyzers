using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1801: a [LoomTool] must declare [ToolSideEffect] on the method or its containing type.
///     Pins the current behaviour; asserts ID and span only.
/// </summary>
public sealed partial class Al1801ToolMustDeclareSideEffectTests
    : AnalyzerTest<Al1801ToolMustDeclareSideEffectAnalyzer> {
    private const string LoomStubs = """
                                     namespace Loom {
                                         public enum ToolSideEffects { None = 0, ReadsOnly = 1, WritesExternalState = 2 }

                                         [System.AttributeUsage(System.AttributeTargets.Method | System.AttributeTargets.Class)]
                                         public sealed class LoomToolAttribute : System.Attribute { }

                                         [System.AttributeUsage(System.AttributeTargets.Method | System.AttributeTargets.Class)]
                                         public sealed class ToolSideEffectAttribute : System.Attribute {
                                             public ToolSideEffectAttribute(ToolSideEffects effect) { }
                                         }
                                     }
                                     """;

    [Fact]
    public Task ShouldReportToolWithoutSideEffect() =>
        VerifyAsync($$"""
                      using Loom;

                      {{LoomStubs}}

                      public class Tools {
                          [LoomTool]
                          public string [|ReadFile|](string path) => path;
                      }
                      """);

    [Fact]
    public Task ShouldReportStaticToolWithoutSideEffect() =>
        VerifyAsync($$"""
                      using Loom;

                      {{LoomStubs}}

                      public static class Tools {
                          [LoomToolAttribute]
                          public static void [|Run|]() { }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenMethodDeclaresSideEffect() =>
        VerifyAsync($$"""
                      using Loom;

                      {{LoomStubs}}

                      public class Tools {
                          [LoomTool]
                          [ToolSideEffect(ToolSideEffects.ReadsOnly)]
                          public string ReadFile(string path) => path;
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenContainingTypeDeclaresSideEffect() =>
        VerifyAsync($$"""
                      using Loom;

                      {{LoomStubs}}

                      [ToolSideEffect(ToolSideEffects.WritesExternalState)]
                      public class Tools {
                          [LoomTool]
                          public void Write() { }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportMethodWithoutLoomTool() =>
        VerifyAsync($$"""
                      using Loom;

                      {{LoomStubs}}

                      public class Tools {
                          public void Run() { }
                      }
                      """);

    // Current behaviour: [LoomTool] on the containing type alone does not make its methods tools.
    [Fact]
    public Task ShouldNotReportWhenOnlyContainingTypeIsLoomTool() =>
        VerifyAsync($$"""
                      using Loom;

                      {{LoomStubs}}

                      [LoomTool]
                      public class Tools {
                          public void Run() { }
                      }
                      """);
}
