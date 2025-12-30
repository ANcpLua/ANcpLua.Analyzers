using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0010: Type should be partial for source generator support.
///     Note: This analyzer is disabled by default (DiagnosticSeverity.Info, enabledByDefault: false).
/// </summary>
public sealed class AL0010AnalyzerTests : ALAnalyzerTest<AL0010PartialTypeAnalyzer> {
    [Theory]
    [InlineData("""
                public class [|C|] { }
                """)]
    [InlineData("""
                public struct [|S|] { }
                """)]
    [InlineData("""
                public record [|R|];
                """)]
    [InlineData("""
                public record struct [|RS|];
                """)]
    [InlineData("""
                internal class [|Internal|] { }
                """)]
    [InlineData("""
                public sealed class [|Sealed|] { }
                """)]
    [InlineData("""
                public abstract class [|Abstract|] { }
                """)]
    public Task ShouldReportNonPartialTypes(string source) => VerifyAsync(source);

    [Theory]
    [InlineData("""
                public partial class C { }
                """)]
    [InlineData("""
                public partial struct S { }
                """)]
    [InlineData("""
                public partial record R;
                """)]
    [InlineData("""
                public partial record struct RS;
                """)]
    [InlineData("""
                internal partial class Internal { }
                """)]
    public Task ShouldNotReportPartialTypes(string source) => VerifyAsync(source);
}

/// <summary>
///     Code fix tests for AL0010: Adds partial modifier to types.
/// </summary>
public sealed class AL0010CodeFixTests : ALCodeFixTest<AL0010PartialTypeAnalyzer, AL0010PartialTypeCodeFixProvider> {
    [Fact]
    public Task ShouldAddPartialToClass() => VerifyAsync(
        """
        public class [|C|] { }
        """,
        """
        public partial class C { }
        """);

    [Fact]
    public Task ShouldAddPartialToStruct() => VerifyAsync(
        """
        public struct [|S|] { }
        """,
        """
        public partial struct S { }
        """);

    [Fact]
    public Task ShouldAddPartialToRecord() => VerifyAsync(
        """
        public record [|R|];
        """,
        """
        public partial record R;
        """);

    [Fact]
    public Task ShouldAddPartialToRecordStruct() => VerifyAsync(
        """
        public record struct [|RS|];
        """,
        """
        public partial record struct RS;
        """);

    [Fact]
    public Task ShouldAddPartialToSealedClass() => VerifyAsync(
        """
        public sealed class [|Sealed|] { }
        """,
        """
        public sealed partial class Sealed { }
        """);

    [Fact]
    public Task ShouldAddPartialToAbstractClass() => VerifyAsync(
        """
        public abstract class [|Abstract|] { }
        """,
        """
        public abstract partial class Abstract { }
        """);
}
