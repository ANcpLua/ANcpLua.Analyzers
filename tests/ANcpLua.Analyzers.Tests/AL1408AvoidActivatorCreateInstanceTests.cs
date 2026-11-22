using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1408: Avoid Activator.CreateInstance in AOT context.
/// </summary>
public sealed partial class Al1408AvoidActivatorCreateInstanceTests
    : AnalyzerTest<Al1408AvoidActivatorCreateInstanceAnalyzer> {
    [Fact]
    public Task ShouldReportGenericCreateInstance() =>
        VerifyAsync("""
                    using System;

                    public class Foo { }

                    public class C {
                        public Foo M() => {|AL1408:Activator.CreateInstance<Foo>()|};
                    }
                    """);

    [Fact]
    public Task ShouldReportCreateInstanceWithTypeOf() =>
        VerifyAsync("""
                    using System;

                    public class Foo { }

                    public class C {
                        public object M() => {|AL1408:Activator.CreateInstance(typeof(Foo))|};
                    }
                    """);

    [Fact]
    public Task ShouldReportCreateInstanceWithTypeVariable() =>
        VerifyAsync("""
                    using System;

                    public class C {
                        public object M(Type type) => {|AL1408:Activator.CreateInstance(type)|};
                    }
                    """);

    [Fact]
    public Task ShouldNotReportExplicitConstruction() =>
        VerifyAsync("""
                    public class Foo { }

                    public class C {
                        public Foo M() => new Foo();
                    }
                    """);

    [Fact]
    public Task ShouldNotReportCreateInstanceOnOtherType() =>
        VerifyAsync("""
                    public static class MyActivator {
                        public static object CreateInstance() => new object();
                    }

                    public class C {
                        public object M() => MyActivator.CreateInstance();
                    }
                    """);
}
