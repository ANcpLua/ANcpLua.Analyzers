using ANcpLua.Roslyn.Utilities.Testing;
using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0010: Type should be partial for source generator support.
///     Note: This analyzer is disabled by default (DiagnosticSeverity.Info, enabledByDefault: false).
/// </summary>
public sealed class AL0010AnalyzerTests : AnalyzerTest<AL0010PartialTypeAnalyzer> {
    [Theory]
    [InlineData("public class [|C|] { }")]
    [InlineData("public struct [|S|] { }")]
    [InlineData("public record [|R|];")]
    [InlineData("public record struct [|RS|];")]
    [InlineData("internal class [|Internal|] { }")]
    [InlineData("public sealed class [|Sealed|] { }")]
    [InlineData("public abstract class [|Abstract|] { }")]
    public Task ShouldReportNonPartialTypes(string source) => VerifyAsync(source);

    [Theory]
    [InlineData("public partial class C { }")]
    [InlineData("public partial struct S { }")]
    [InlineData("public partial record R;")]
    [InlineData("public partial record struct RS;")]
    [InlineData("internal partial class Internal { }")]
    public Task ShouldNotReportPartialTypes(string source) => VerifyAsync(source);
}

/// <summary>
///     Code fix tests for AL0010: Adds partial modifier to types.
/// </summary>
public sealed class AL0010CodeFixTests : CodeFixTest<AL0010PartialTypeAnalyzer, AL0010PartialTypeCodeFixProvider> {
    [Theory]
    [InlineData("public class [|C|] { }", "public partial class C { }")]
    [InlineData("public struct [|S|] { }", "public partial struct S { }")]
    [InlineData("public record [|R|];", "public partial record R;")]
    [InlineData("public record struct [|RS|];", "public partial record struct RS;")]
    [InlineData("public sealed class [|Sealed|] { }", "public sealed partial class Sealed { }")]
    [InlineData("public abstract class [|Abstract|] { }", "public abstract partial class Abstract { }")]
    public Task ShouldAddPartial(string source, string expected) => VerifyAsync(source, expected);
}
