using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0096: Enable EventSourceSupport for AOT with telemetry.
/// </summary>
/// <remarks>
///     This analyzer reads MSBuild properties via AnalyzerConfigOptionsProvider.GlobalOptions.
///     In unit tests without a .globalconfig, the properties are not available, so the analyzer
///     correctly produces no diagnostics (PublishAot is not set). The positive case (PublishAot=true
///     without EventSourceSupport=true) is verified via integration tests at build time when the
///     MSBuild properties are exposed through CompilerVisibleProperty in ANcpLua.Analyzers.props.
/// </remarks>
public sealed partial class Al0096EnableEventSourceSupportTests : AnalyzerTest<Al0096EnableEventSourceSupportAnalyzer> {
    [Fact]
    public Task ShouldNotReportWhenPublishAotIsNotSet() =>
        VerifyAsync("""
                    public class C {
                        public void M() { }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportForEmptyCode() =>
        VerifyAsync("public class C { }");
}
