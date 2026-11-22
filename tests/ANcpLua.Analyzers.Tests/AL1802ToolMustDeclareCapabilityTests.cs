using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1802: a [LoomTool] must declare at least one [RequiresCapability] on the method or
///     its containing type. Pins the current behaviour; asserts ID and span only.
/// </summary>
public sealed partial class Al1802ToolMustDeclareCapabilityTests
    : AnalyzerTest<Al1802ToolMustDeclareCapabilityAnalyzer> {
    private const string LoomStubs = """
                                     namespace Loom {
                                         [System.AttributeUsage(System.AttributeTargets.Method | System.AttributeTargets.Class)]
                                         public sealed class LoomToolAttribute : System.Attribute { }

                                         [System.AttributeUsage(System.AttributeTargets.Method | System.AttributeTargets.Class, AllowMultiple = true)]
                                         public sealed class RequiresCapabilityAttribute : System.Attribute {
                                             public RequiresCapabilityAttribute(string capability) { }
                                         }
                                     }
                                     """;

    [Fact]
    public Task ShouldReportToolWithoutCapability() =>
        VerifyAsync($$"""
                      using Loom;

                      {{LoomStubs}}

                      public class Tools {
                          [LoomTool]
                          public string [|ReadFile|](string path) => path;
                      }
                      """);

    [Fact]
    public Task ShouldReportEachToolWithoutCapability() =>
        VerifyAsync($$"""
                      using Loom;

                      {{LoomStubs}}

                      public class Tools {
                          [LoomTool]
                          public void [|First|]() { }

                          [LoomTool]
                          [RequiresCapability("file:read")]
                          public void Second() { }

                          [LoomToolAttribute]
                          public void [|Third|]() { }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenMethodDeclaresCapability() =>
        VerifyAsync($$"""
                      using Loom;

                      {{LoomStubs}}

                      public class Tools {
                          [LoomTool]
                          [RequiresCapability("file:read")]
                          [RequiresCapability("file:write")]
                          public string ReadFile(string path) => path;
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenContainingTypeDeclaresCapability() =>
        VerifyAsync($$"""
                      using Loom;

                      {{LoomStubs}}

                      [RequiresCapability("git:write")]
                      public class Tools {
                          [LoomTool]
                          public void Commit() { }
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
}
