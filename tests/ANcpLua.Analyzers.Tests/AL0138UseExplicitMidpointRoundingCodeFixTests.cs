using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Code fix tests for AL0138: Math.Round / MathF.Round → overload with MidpointRounding.ToEven.
/// </summary>
public sealed partial class Al0138UseExplicitMidpointRoundingCodeFixTests
    : CodeFixTest<Al0138UseExplicitMidpointRoundingAnalyzer, Al0138UseExplicitMidpointRoundingCodeFixProvider> {
    [Fact]
    public Task ShouldFixMathRoundDouble() =>
        VerifyAsync(
            """
            using System;
            public class C {
                double M(double x) => [|Math.Round(x)|];
            }
            """,
            """
            using System;
            public class C {
                double M(double x) => Math.Round(x, MidpointRounding.ToEven);
            }
            """);

    [Fact]
    public Task ShouldFixMathRoundDoubleWithDigits() =>
        VerifyAsync(
            """
            using System;
            public class C {
                double M(double x) => [|Math.Round(x, 2)|];
            }
            """,
            """
            using System;
            public class C {
                double M(double x) => Math.Round(x, 2, MidpointRounding.ToEven);
            }
            """);

    [Fact]
    public Task ShouldFixMathRoundDecimal() =>
        VerifyAsync(
            """
            using System;
            public class C {
                decimal M(decimal x) => [|Math.Round(x)|];
            }
            """,
            """
            using System;
            public class C {
                decimal M(decimal x) => Math.Round(x, MidpointRounding.ToEven);
            }
            """);

    [Fact]
    public Task ShouldFixMathFRoundFloat() =>
        VerifyAsync(
            """
            using System;
            public class C {
                float M(float x) => [|MathF.Round(x)|];
            }
            """,
            """
            using System;
            public class C {
                float M(float x) => MathF.Round(x, MidpointRounding.ToEven);
            }
            """);

    [Fact]
    public Task ShouldAddUsingSystemWhenMissing() =>
        VerifyAsync(
            """
            public class C {
                double M(double x) => [|System.Math.Round(x)|];
            }
            """,
            """
            using System;
            public class C {
                double M(double x) => System.Math.Round(x, MidpointRounding.ToEven);
            }
            """);
}
