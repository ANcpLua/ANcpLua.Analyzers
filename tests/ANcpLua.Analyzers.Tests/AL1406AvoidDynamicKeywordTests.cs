using AnalyzerTestBase = ANcpLua.Roslyn.Utilities.Testing.AnalyzerTest<ANcpLua.Analyzers.Analyzers.Al1406AvoidDynamicKeywordAnalyzer>;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1406: Avoid 'dynamic' keyword in AOT-published code.
/// </summary>
/// <remarks>
///     The analyzer is gated on MSBuild <c>PublishAot=true</c> or <c>IsAotCompatible=true</c>.
///     In unit tests without a .globalconfig the properties aren't set, so the analyzer correctly
///     produces no diagnostics. The positive case (AOT project using dynamic) is verified by
///     build-time integration tests — same pattern as AL1407.
/// </remarks>
public sealed partial class Al1406AvoidDynamicKeywordTests : AnalyzerTestBase {
    [Fact]
    public Task ShouldNotReportDynamicInvocationOutsideAotContext() =>
        VerifyAsync("""
                    public class C {
                        public void M() {
                            dynamic d = 42;
                            d.ToString();
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportDynamicMemberReferenceOutsideAotContext() =>
        VerifyAsync("""
                    public class C {
                        public void M() {
                            dynamic d = new object();
                            var x = d.Name;
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportDynamicIndexerAccessOutsideAotContext() =>
        VerifyAsync("""
                    public class C {
                        public void M() {
                            dynamic d = new object();
                            var x = d[0];
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportStaticTypedCode() =>
        VerifyAsync("""
                    public class C {
                        public void M() {
                            object o = 42;
                            o.ToString();
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportGenericCode() =>
        VerifyAsync("""
                    public class C {
                        public T Identity<T>(T value) => value;
                        public void M() {
                            var result = Identity(42);
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportVarUsage() =>
        VerifyAsync("""
                    public class C {
                        public void M() {
                            var x = 42;
                            var s = x.ToString();
                        }
                    }
                    """);
}
