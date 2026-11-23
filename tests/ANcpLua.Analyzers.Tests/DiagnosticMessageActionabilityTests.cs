using ANcpLua.Analyzers.Analyzers;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Globalization;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Agents read diagnostics from build output and never run a code fix, so the message itself has to
///     say what to write. Each row pins the replacement text of one message that used to leave it out.
/// </summary>
public sealed partial class DiagnosticMessageActionabilityTests {
    [Theory]
    [InlineData("AL1000", "'var {0}Value = {0};'")]
    [InlineData("AL1001", "delete every pair of 'not'")]
    [InlineData("AL1002", "divide by a non-zero value")]
    [InlineData("AL1003", "'span is [1, 2, 3]'")]
    [InlineData("AL1004", "'left.SequenceEqual(right)'")]
    [InlineData("AL1005", "rename the field (for example '_{0}')")]
    [InlineData("AL1006", "'XmlSchema? IXmlSerializable.GetSchema() => null;'")]
    [InlineData("AL1007", "'XmlSchema? IXmlSerializable.GetSchema() => null;'")]
    [InlineData("AL1008", "delete this call")]
    [InlineData("AL1011", "with '{1}'")]
    [InlineData("AL1012", "'if (<initializer> is not {{ }} {0}) <early exit>;'")]
    [InlineData("AL1109", "replace 'await Task.Run(() => Work())' with 'Work()'")]
    [InlineData("AL1208", "'Guard.NotNull({0})'")]
    [InlineData("AL1301", "'lock (_gate)'")]
    [InlineData("AL1302", "'lock (s_gate)'")]
    [InlineData("AL1303", "'lock (_gate)'")]
    [InlineData("AL1313", "pass the CancellationToken that is in scope")]
    [InlineData("AL1400", "change the return type to 'int'")]
    [InlineData("AL1402", "remove [TrimSafe] from '{0}'")]
    [InlineData("AL1403", "remove [AotSafe] from '{0}'")]
    [InlineData("AL1404", "remove [AotSafe] from '{0}'")]
    [InlineData("AL1405", "remove the [AotUnsafe] attribute")]
    [InlineData("AL1406", "Replace 'dynamic' with a static type")]
    [InlineData("AL1500", "add one switch arm, case label or Match handler for each listed type")]
    [InlineData("AL1501", "'symbol.ToDisplayString()'")]
    [InlineData("AL1502", "'public void Initialize(IncrementalGeneratorInitializationContext context)'")]
    [InlineData("AL1504", "add the 'partial' modifier")]
    [InlineData("AL1505", "a different Ordinal")]
    [InlineData("AL1601", "'<Import Project=\"$(MSBuildThisFileDirectory)Version.props\" />'")]
    [InlineData("AL1606", "change its version to '{2}' or newer")]
    [InlineData("AL1700", "add the 'static' modifier")]
    [InlineData("AL1703", "with 'var'")]
    public void MessageFormat_ShouldSayWhatToWrite(string diagnosticId, string expectedFragment) =>
        GetMessageFormat(diagnosticId).Should().Contain(expectedFragment);

    [Fact]
    public async Task AL1011_ShouldPrintTheReplacementStatement() {
        var messages = await GetMessagesAsync(
            new Al1011NormalizeNullGuardStyleAnalyzer(),
            "using System; public class C { void M(string? x) { if (x is null) throw new ArgumentNullException(nameof(x)); } }")
            .ConfigureAwait(true);

        messages.Should().Equal(
            "Replace this if/throw null-guard with 'ArgumentNullException.ThrowIfNull(x);' (null-guard style 'bcl')");
    }

    [Fact]
    public async Task AL1208_ShouldPrintTheGuardCallNotTheOperand() {
        var messages = await GetMessagesAsync(
            new Al1208UseGuardNotNullAnalyzer(),
            """
            using System;
            public class C {
                string M(string? x) => x ?? throw new ArgumentNullException(nameof(x));
            }
            namespace ANcpLua.Roslyn.Utilities { internal static class Guard { } }
            """)
            .ConfigureAwait(true);

        messages.Should().Equal(
            "Use 'Guard.NotNull(x)' instead of 'x ?? throw new ArgumentNullException(nameof(x))'");
    }

    private static async Task<ImmutableArray<string>> GetMessagesAsync(DiagnosticAnalyzer analyzer, string source) {
        var cancellationToken = TestContext.Current.CancellationToken;
        var tree = CSharpSyntaxTree.ParseText(
            source,
            new CSharpParseOptions(LanguageVersion.CSharp15),
            "MessageSample.cs",
            cancellationToken: cancellationToken);
        var compilation = CSharpCompilation.Create(
            "MessageSample",
            [tree],
            Net110.References.All,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
        var withAnalyzers = new CompilationWithAnalyzers(
            compilation,
            [analyzer],
            new CompilationWithAnalyzersOptions(
                new AnalyzerOptions([]),
                onAnalyzerException: null,
                concurrentAnalysis: true,
                logAnalyzerExecutionTime: false));
        var diagnostics = await withAnalyzers.GetAnalyzerDiagnosticsAsync(cancellationToken).ConfigureAwait(true);

        return [.. diagnostics.Select(static d => d.GetMessage(CultureInfo.InvariantCulture))];
    }

    private static string GetMessageFormat(string diagnosticId) =>
        typeof(AlAnalyzer).Assembly.GetTypes()
            .Where(static t => typeof(DiagnosticAnalyzer).IsAssignableFrom(t) && !t.IsAbstract)
            .Select(static t => (DiagnosticAnalyzer)(Activator.CreateInstance(t)
                ?? throw new InvalidOperationException($"Cannot instantiate {t}")))
            .SelectMany(static analyzer => analyzer.SupportedDiagnostics)
            .First(descriptor => descriptor.Id == diagnosticId)
            .MessageFormat
            .ToString(CultureInfo.InvariantCulture);
}
