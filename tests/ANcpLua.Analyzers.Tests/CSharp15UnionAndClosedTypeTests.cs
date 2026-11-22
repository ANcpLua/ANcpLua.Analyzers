using System.Collections.Immutable;
using System.Globalization;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Runs every analyzer in the assembly over C# 15 code (native <c>union</c> types and <c>closed</c>
///     hierarchies) and checks that no analyzer crashes (AD0001) or reports on code the compiler accepts.
/// </summary>
/// <remarks>
///     The test project compiles against net10.0 reference assemblies, which lack the runtime support
///     types for unions and closed types, so each sample declares them itself. The sample must compile
///     without errors; otherwise the analyzers would only have seen broken syntax.
/// </remarks>
public sealed partial class CSharp15UnionAndClosedTypeTests {
    private const string AnalyzerCrashId = "AD0001";
    private const string ClosedHierarchyRuleId = "AL1500";

    private const string RuntimeSupport = """
        namespace System.Runtime.CompilerServices
        {
            [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
            internal sealed class UnionAttribute : System.Attribute
            {
            }

            internal interface IUnion
            {
                object? Value { get; }
            }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
            internal sealed class IsClosedTypeAttribute : System.Attribute
            {
                public System.Type[] DerivedTypes { get; set; } = System.Array.Empty<System.Type>();
            }
        }

        """;

    private const string Unions = """
        namespace Sample
        {
            using System;

            public sealed record Accepted(string Message);
            public sealed record Rejected(string Reason);
            public readonly union Outcome(Accepted, Rejected);

            public sealed record Meters(double Value);
            public sealed record Feet(double Value);
            public readonly union Length(Meters, Feet)
            {
                public static Length Create(double value, bool isMetric) =>
                    isMetric ? new Meters(value) : new Feet(value);

                public double TotalMeters => this switch
                {
                    Meters meters => meters.Value,
                    Feet feet => feet.Value * 0.3048,
                    null => throw new InvalidOperationException("Length was not initialized."),
                };
            }

            public sealed record Success<T>(T Value);
            public sealed record Failure(string Error);
            public readonly union Result<T>(Success<T>, Failure);

            public static class OutcomeText
            {
                public static string Describe(Outcome value) => value switch
                {
                    Accepted accepted => accepted.Message,
                    Rejected rejected => rejected.Reason,
                    null => throw new InvalidOperationException("Outcome was not initialized."),
                };

                public static int Rank(Outcome value)
                {
                    switch (value)
                    {
                        case Accepted:
                            return 1;
                        case Rejected:
                            return 2;
                        default:
                            return 0;
                    }
                }

                public static bool IsAccepted(Outcome value) => value is Accepted;

                public static Outcome Accept(string message) => new Accepted(message);

                public static string Unwrap(Result<string> result) => result switch
                {
                    Success<string> success => success.Value,
                    Failure failure => failure.Error,
                    null => throw new InvalidOperationException("Result was not initialized."),
                };

                public static TResult Match<TResult>(
                    this Outcome value,
                    Func<Accepted, TResult> accepted,
                    Func<Rejected, TResult> rejected) => value switch
                {
                    Accepted item => accepted(item),
                    Rejected item => rejected(item),
                    null => throw new InvalidOperationException("Outcome was not initialized."),
                };

                public static string Text(Outcome value) =>
                    value.Match(static accepted => accepted.Message, static rejected => rejected.Reason);
            }
        }
        """;

    private const string ClosedGateHierarchy = """
        namespace Sample
        {
            using System;

            public closed record GateState;
            public sealed record GateClosed : GateState;
            public sealed record GateOpen : GateState;
            public closed record GateMoving(float Percent) : GateState;
            public sealed record GateOpening(float Percent) : GateMoving(Percent);
            public sealed record GateClosing(float Percent) : GateMoving(Percent);

            public closed class Shape
            {
            }

            public sealed class Circle : Shape
            {
            }

            public sealed class Square : Shape
            {
            }

        """;

    private const string AbstractGateHierarchy = """
        namespace Sample
        {
            using System;

            public abstract record GateState;
            public sealed record GateClosed : GateState;
            public sealed record GateOpen : GateState;
            public abstract record GateMoving(float Percent) : GateState;
            public sealed record GateOpening(float Percent) : GateMoving(Percent);
            public sealed record GateClosing(float Percent) : GateMoving(Percent);

        """;

    private const string SwitchOverEveryLeaf = """
            public static class Gates
            {
                public static string Describe(GateState state) => state switch
                {
                    GateClosed => "closed",
                    GateOpen => "open",
                    GateOpening => "opening",
                    GateClosing => "closing",
                    null => throw new ArgumentNullException(nameof(state)),
                };
            }
        }
        """;

    private const string SwitchOverIntermediateType = """
            public static class Gates
            {
                public static string Describe(GateState state) => state switch
                {
                    GateClosed => "closed",
                    GateOpen => "open",
                    GateMoving => "moving",
                    null => throw new ArgumentNullException(nameof(state)),
                };
            }
        }
        """;

    private const string SwitchMissingOneLeaf = """
            public static class Gates
            {
                public static string Describe(GateState state) => state switch
                {
                    GateClosed => "closed",
                    GateOpen => "open",
                    GateOpening => "opening",
                    null => "none",
                };
            }
        }
        """;

    private const string SwitchStatementOverShapes = """
            public static class Shapes
            {
                public static int Sides(Shape shape)
                {
                    switch (shape)
                    {
                        case Circle:
                            return 0;
                        case Square:
                            return 4;
                        default:
                            return -1;
                    }
                }
            }
        """;

    [Fact]
    public Task UnionDeclarationsAndExhaustiveMatches_ShouldNotCrashOrReport() =>
        AssertNoDiagnosticsAsync(RuntimeSupport + Unions);

    [Fact]
    public Task ClosedHierarchyWithEveryLeafHandled_ShouldNotCrashOrReport() =>
        AssertNoDiagnosticsAsync(RuntimeSupport + ClosedGateHierarchy + SwitchStatementOverShapes + SwitchOverEveryLeaf);

    [Fact]
    public Task ClosedHierarchyWithIntermediateTypeArm_ShouldNotReportItsLeavesAsMissing() =>
        AssertNoDiagnosticsAsync(RuntimeSupport + ClosedGateHierarchy + SwitchOverIntermediateType);

    [Fact]
    public Task AbstractHierarchyWithIntermediateTypeArm_ShouldNotReportItsLeavesAsMissing() =>
        AssertNoDiagnosticsAsync(AbstractGateHierarchy + SwitchOverIntermediateType);

    [Fact]
    public async Task AbstractHierarchyMissingOneLeaf_ShouldNameTheLeafAndTheFix() {
        var diagnostics = await AnalyzeAsync(AbstractGateHierarchy + SwitchMissingOneLeaf).ConfigureAwait(true);

        diagnostics.Select(static d => d.Id).Should().Equal(ClosedHierarchyRuleId);
        var message = diagnostics[0].GetMessage(CultureInfo.InvariantCulture);
        message.Should().Contain("GateClosing").And.Contain("add one switch arm");
        message.Should().NotContain("GateOpening");
    }

    [Fact]
    public async Task ClosedHierarchyMissingOneLeaf_ShouldReportOnlyThatLeaf() {
        var diagnostics = await AnalyzeAsync(RuntimeSupport + ClosedGateHierarchy + SwitchMissingOneLeaf).ConfigureAwait(true);

        // A closed type is implicitly abstract, so AL1500 sees the hierarchy like an abstract one and
        // reports next to the compiler's own CS8509. No other rule may report.
        diagnostics.Select(Describe).Should().ContainSingle()
            .Which.Should().StartWith(ClosedHierarchyRuleId).And.Contain("GateClosing").And.NotContain("GateOpening");
    }

    private static async Task AssertNoDiagnosticsAsync(string source) {
        var diagnostics = await AnalyzeAsync(source).ConfigureAwait(true);

        diagnostics.Select(Describe).Should().BeEmpty();
    }

    /// <summary>
    ///     Compiles <paramref name="source"/> as C# preview (the language version that carries C# 15 in
    ///     the pinned Roslyn package), then returns analyzer crashes plus every diagnostic located in source.
    /// </summary>
    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source) {
        var cancellationToken = TestContext.Current.CancellationToken;
        var tree = CSharpSyntaxTree.ParseText(
            source,
            new CSharpParseOptions(LanguageVersion.Preview),
            "CSharp15Sample.cs",
            cancellationToken: cancellationToken);
        var compilation = CSharpCompilation.Create(
            "CSharp15Sample",
            [tree],
            Net100.References.All,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        compilation.GetDiagnostics(cancellationToken)
            .Where(static d => d.Severity == DiagnosticSeverity.Error)
            .Select(Describe)
            .Should().BeEmpty("the sample must compile, otherwise the analyzers never saw valid C# 15 code");

        var withAnalyzers = new CompilationWithAnalyzers(
            compilation,
            CreateAllAnalyzers(),
            new CompilationWithAnalyzersOptions(
                new AnalyzerOptions([]),
                onAnalyzerException: null,
                concurrentAnalysis: true,
                logAnalyzerExecutionTime: false));
        var diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync(cancellationToken).ConfigureAwait(true);

        return [.. diagnostics.Where(static d => d.Id == AnalyzerCrashId || d.Location.IsInSource)];
    }

    private static ImmutableArray<DiagnosticAnalyzer> CreateAllAnalyzers() => [
        .. typeof(AlAnalyzer).Assembly.GetTypes()
            .Where(static t => typeof(DiagnosticAnalyzer).IsAssignableFrom(t) && !t.IsAbstract)
            .Select(static t => (DiagnosticAnalyzer)(Activator.CreateInstance(t)
                ?? throw new InvalidOperationException($"Cannot instantiate {t}")))
    ];

    private static string Describe(Diagnostic diagnostic) =>
        $"{diagnostic.Id} at {diagnostic.Location.GetLineSpan().StartLinePosition}: {diagnostic.GetMessage(CultureInfo.InvariantCulture)}";
}
