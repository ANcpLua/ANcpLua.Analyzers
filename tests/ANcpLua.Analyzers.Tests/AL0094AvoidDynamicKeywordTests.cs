using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using AnalyzerTestBase = ANcpLua.Roslyn.Utilities.Testing.AnalyzerTest<ANcpLua.Analyzers.Analyzers.Al0094AvoidDynamicKeywordAnalyzer>;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0094: Avoid 'dynamic' keyword in AOT-published code.
/// </summary>
/// <remarks>
///     Dynamic method calls generate both a DynamicMemberReference (d.Foo) and a
///     DynamicInvocation (d.Foo()) operation, so tests must expect both diagnostics.
/// </remarks>
public sealed partial class Al0094AvoidDynamicKeywordTests : AnalyzerTestBase {
    [Fact]
    public Task ShouldReportDynamicInvocation() {
        const string Source = """
                              public class C {
                                  public void M() {
                                      dynamic d = 42;
                                      d.ToString();
                                  }
                              }
                              """;

        // d.ToString() produces both a DynamicMemberReference and a DynamicInvocation
        var expected = new[] {
            new DiagnosticResult("AL0094", DiagnosticSeverity.Warning)
                .WithSpan(4, 9, 4, 21)
                .WithArguments("dynamic invocation"),
            new DiagnosticResult("AL0094", DiagnosticSeverity.Warning)
                .WithSpan(4, 9, 4, 19)
                .WithArguments("dynamic member reference")
        };

        return VerifyAsync(Source, [], expected);
    }

    [Fact]
    public Task ShouldReportDynamicMemberReference() =>
        VerifyAsync("""
                    public class C {
                        public void M() {
                            dynamic d = new object();
                            var x = {|AL0094:d.Name|};
                        }
                    }
                    """);

    [Fact]
    public Task ShouldReportDynamicIndexerAccess() =>
        VerifyAsync("""
                    public class C {
                        public void M() {
                            dynamic d = new object();
                            var x = {|AL0094:d[0]|};
                        }
                    }
                    """);

    [Fact]
    public Task ShouldReportMultipleDynamicOperations() {
        const string Source = """
                              public class C {
                                  public void M() {
                                      dynamic d = new object();
                                      d.Foo();
                                      var x = d.Bar;
                                  }
                              }
                              """;

        // d.Foo() produces DynamicInvocation + DynamicMemberReference; d.Bar produces DynamicMemberReference
        var expected = new[] {
            new DiagnosticResult("AL0094", DiagnosticSeverity.Warning)
                .WithSpan(4, 9, 4, 16)
                .WithArguments("dynamic invocation"),
            new DiagnosticResult("AL0094", DiagnosticSeverity.Warning)
                .WithSpan(5, 17, 5, 22)
                .WithArguments("dynamic member reference"),
            new DiagnosticResult("AL0094", DiagnosticSeverity.Warning)
                .WithSpan(4, 9, 4, 14)
                .WithArguments("dynamic member reference")
        };

        return VerifyAsync(Source, [], expected);
    }

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
