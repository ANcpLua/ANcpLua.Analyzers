using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1409: Avoid Type.GetType with dynamic name in AOT context.
/// </summary>
public sealed partial class Al1409AvoidTypeGetTypeTests : AnalyzerTest<Al1409AvoidTypeGetTypeAnalyzer> {
    [Fact]
    public Task ShouldReportDynamicTypeName() =>
        VerifyAsync("""
                    using System;

                    public class C {
                        public Type M(string name) => {|AL1409:Type.GetType(name)|};
                    }
                    """);

    [Fact]
    public Task ShouldReportLiteralTypeNameWithIgnoreCaseTrue() =>
        VerifyAsync("""
                    using System;

                    public class C {
                        public Type M() => {|AL1409:Type.GetType("System.String", false, true)|};
                    }
                    """);

    [Fact]
    public Task ShouldReportLiteralTypeNameWithNonConstantIgnoreCase() =>
        VerifyAsync("""
                    using System;

                    public class C {
                        public Type M(bool ignoreCase) => {|AL1409:Type.GetType("System.String", false, ignoreCase)|};
                    }
                    """);

    [Fact]
    public Task ShouldNotReportLiteralTypeName() =>
        VerifyAsync("""
                    using System;

                    public class C {
                        public Type M() => Type.GetType("System.String");
                    }
                    """);

    [Fact]
    public Task ShouldNotReportLiteralTypeNameWithIgnoreCaseFalse() =>
        VerifyAsync("""
                    using System;

                    public class C {
                        public Type M() => Type.GetType("System.String", false, false);
                    }
                    """);

    [Fact]
    public Task ShouldNotReportObjectGetType() =>
        VerifyAsync("""
                    using System;

                    public class C {
                        public Type M(object value) => value.GetType();
                    }
                    """);
}
