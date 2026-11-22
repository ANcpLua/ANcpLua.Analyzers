using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1800: a [LoomTool] whose [ToolSideEffect] is destructive must declare [RequiresApproval].
///     Pins the current behaviour; asserts ID and span only.
/// </summary>
public sealed partial class Al1800DestructiveToolMustRequireApprovalTests
    : AnalyzerTest<Al1800DestructiveToolMustRequireApprovalAnalyzer> {
    private const string LoomStubs = """
                                     namespace Loom {
                                         public enum ToolSideEffects {
                                             None = 0,
                                             ReadsOnly = 1,
                                             WritesExternalState = 2,
                                             MutatesCode = 3,
                                             Deploys = 4,
                                             ClosesIssue = 5
                                         }

                                         [System.AttributeUsage(System.AttributeTargets.Method)]
                                         public sealed class LoomToolAttribute : System.Attribute { }

                                         [System.AttributeUsage(System.AttributeTargets.Method | System.AttributeTargets.Class)]
                                         public sealed class ToolSideEffectAttribute : System.Attribute {
                                             public ToolSideEffectAttribute(ToolSideEffects effect) { }
                                         }

                                         [System.AttributeUsage(System.AttributeTargets.Method | System.AttributeTargets.Class)]
                                         public sealed class RequiresApprovalAttribute : System.Attribute { }
                                     }
                                     """;

    [Theory]
    [InlineData("WritesExternalState")]
    [InlineData("MutatesCode")]
    [InlineData("Deploys")]
    [InlineData("ClosesIssue")]
    public Task ShouldReportDestructiveToolWithoutApproval(string sideEffect) =>
        VerifyAsync($$"""
                      using Loom;

                      {{LoomStubs}}

                      public class Tools {
                          [LoomTool]
                          [ToolSideEffect(ToolSideEffects.{{sideEffect}})]
                          public void [|Run|]() { }
                      }
                      """);

    [Fact]
    public Task ShouldReportWhenDestructiveSideEffectIsOnContainingType() =>
        VerifyAsync($$"""
                      using Loom;

                      {{LoomStubs}}

                      [ToolSideEffect(ToolSideEffects.Deploys)]
                      public class Tools {
                          [LoomTool]
                          public void [|Ship|]() { }
                      }
                      """);

    [Fact]
    public Task ShouldReportWhenAttributeSuffixIsSpelledOut() =>
        VerifyAsync($$"""
                      using Loom;

                      {{LoomStubs}}

                      public class Tools {
                          [LoomToolAttribute]
                          [ToolSideEffectAttribute(ToolSideEffects.MutatesCode)]
                          public void [|Edit|]() { }
                      }
                      """);

    // Current behaviour: an enum value above ClosesIssue (5) still counts as destructive
    // (the message falls back to the numeric value).
    [Fact]
    public Task ShouldReportUnknownSideEffectAboveThreshold() =>
        VerifyAsync($$"""
                      using Loom;

                      {{LoomStubs}}

                      public class Tools {
                          [LoomTool]
                          [ToolSideEffect((ToolSideEffects)9)]
                          public void [|Run|]() { }
                      }
                      """);

    // Current behaviour: a non-destructive method-level [ToolSideEffect] does not override a
    // destructive type-level one; the type-level value is still found and reported.
    [Fact]
    public Task ShouldReportWhenMethodIsReadOnlyButTypeIsDestructive() =>
        VerifyAsync($$"""
                      using Loom;

                      {{LoomStubs}}

                      [ToolSideEffect(ToolSideEffects.Deploys)]
                      public class Tools {
                          [LoomTool]
                          [ToolSideEffect(ToolSideEffects.ReadsOnly)]
                          public void [|Read|]() { }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenMethodRequiresApproval() =>
        VerifyAsync($$"""
                      using Loom;

                      {{LoomStubs}}

                      public class Tools {
                          [LoomTool]
                          [ToolSideEffect(ToolSideEffects.Deploys)]
                          [RequiresApproval]
                          public void Ship() { }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenContainingTypeRequiresApproval() =>
        VerifyAsync($$"""
                      using Loom;

                      {{LoomStubs}}

                      [RequiresApproval]
                      public class Tools {
                          [LoomTool]
                          [ToolSideEffect(ToolSideEffects.ClosesIssue)]
                          public void Close() { }
                      }
                      """);

    [Theory]
    [InlineData("None")]
    [InlineData("ReadsOnly")]
    public Task ShouldNotReportNonDestructiveSideEffect(string sideEffect) =>
        VerifyAsync($$"""
                      using Loom;

                      {{LoomStubs}}

                      public class Tools {
                          [LoomTool]
                          [ToolSideEffect(ToolSideEffects.{{sideEffect}})]
                          public void Read() { }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportToolWithoutSideEffect() =>
        VerifyAsync($$"""
                      using Loom;

                      {{LoomStubs}}

                      public class Tools {
                          [LoomTool]
                          public void Run() { }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportDestructiveMethodWithoutLoomTool() =>
        VerifyAsync($$"""
                      using Loom;

                      {{LoomStubs}}

                      public class Tools {
                          [ToolSideEffect(ToolSideEffects.Deploys)]
                          public void Ship() { }
                      }
                      """);
}
