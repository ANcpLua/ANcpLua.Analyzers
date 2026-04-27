using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0138: Use Math.Round / MathF.Round overloads with explicit MidpointRounding.
/// </summary>
public sealed partial class Al0138UseExplicitMidpointRoundingTests
    : AnalyzerTest<Al0138UseExplicitMidpointRoundingAnalyzer> {
    [Fact]
    public Task ShouldReportForMathRoundDoubleWithoutMidpointRounding() => VerifyAsync("""
        using System;
        public class C {
            double M(double x) => [|Math.Round(x)|];
        }
        """);

    [Fact]
    public Task ShouldReportForMathRoundDoubleWithDigitsWithoutMidpointRounding() => VerifyAsync("""
        using System;
        public class C {
            double M(double x) => [|Math.Round(x, 2)|];
        }
        """);

    [Fact]
    public Task ShouldReportForMathRoundDecimalWithoutMidpointRounding() => VerifyAsync("""
        using System;
        public class C {
            decimal M(decimal x) => [|Math.Round(x)|];
        }
        """);

    [Fact]
    public Task ShouldReportForMathRoundDecimalWithDigitsWithoutMidpointRounding() => VerifyAsync("""
        using System;
        public class C {
            decimal M(decimal x) => [|Math.Round(x, 2)|];
        }
        """);

    [Fact]
    public Task ShouldReportForMathFRoundFloatWithoutMidpointRounding() => VerifyAsync("""
        using System;
        public class C {
            float M(float x) => [|MathF.Round(x)|];
        }
        """);

    [Fact]
    public Task ShouldReportForMathFRoundFloatWithDigitsWithoutMidpointRounding() => VerifyAsync("""
        using System;
        public class C {
            float M(float x) => [|MathF.Round(x, 2)|];
        }
        """);

    [Fact]
    public Task ShouldNotReportWhenMidpointRoundingProvided() => VerifyAsync("""
        using System;
        public class C {
            double M(double x) => Math.Round(x, MidpointRounding.AwayFromZero);
        }
        """);

    [Fact]
    public Task ShouldNotReportWhenAllArgumentsProvided() => VerifyAsync("""
        using System;
        public class C {
            double M(double x) => Math.Round(x, 2, MidpointRounding.AwayFromZero);
        }
        """);

    [Fact]
    public Task ShouldNotReportForUnrelatedMathMethod() => VerifyAsync("""
        using System;
        public class C {
            double M(double x) => Math.Floor(x);
        }
        """);

    [Fact]
    public Task ShouldNotReportForCustomRoundInDifferentNamespace() => VerifyAsync("""
        namespace Other {
            public static class Math {
                public static double Round(double x) => x;
            }
        }
        public class C {
            double M(double x) => Other.Math.Round(x);
        }
        """);
}
