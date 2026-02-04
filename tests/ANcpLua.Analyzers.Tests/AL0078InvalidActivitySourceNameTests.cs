using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0078: Detects ActivitySource names that don't follow reverse-DNS naming convention.
/// </summary>
public sealed partial class Al0078InvalidActivitySourceNameTests : AnalyzerTest<Al0078InvalidActivitySourceNameAnalyzer> {
    private const string ActivitySourceSetup = """
        namespace System.Diagnostics {
            public class ActivitySource {
                public ActivitySource(string name) { }
                public ActivitySource(string name, string? version) { }
            }
        }
        """;

    [Theory]
    [InlineData("MySource")]
    [InlineData("source")]
    [InlineData("TracingSource")]
    public Task ShouldReportSingleWordName(string name) =>
        VerifyAsync($$"""
            {{ActivitySourceSetup}}

            public class C {
                private static readonly System.Diagnostics.ActivitySource Source =
                    new System.Diagnostics.ActivitySource([|"{{name}}"|]);
            }
            """);

    [Theory]
    [InlineData("my source")]
    [InlineData("tracing source")]
    [InlineData("source name with spaces")]
    public Task ShouldReportNameWithSpaces(string name) =>
        VerifyAsync($$"""
            {{ActivitySourceSetup}}

            public class C {
                private static readonly System.Diagnostics.ActivitySource Source =
                    new System.Diagnostics.ActivitySource([|"{{name}}"|]);
            }
            """);

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public Task ShouldReportEmptyOrWhitespaceName(string name) =>
        VerifyAsync($$"""
            {{ActivitySourceSetup}}

            public class C {
                private static readonly System.Diagnostics.ActivitySource Source =
                    new System.Diagnostics.ActivitySource([|"{{name}}"|]);
            }
            """);

    [Theory]
    [InlineData("company..component")]
    [InlineData(".company.component")]
    [InlineData("company.component.")]
    public Task ShouldReportNamesWithEmptySegments(string name) =>
        VerifyAsync($$"""
            {{ActivitySourceSetup}}

            public class C {
                private static readonly System.Diagnostics.ActivitySource Source =
                    new System.Diagnostics.ActivitySource([|"{{name}}"|]);
            }
            """);

    [Theory]
    [InlineData("company.product.component")]
    [InlineData("com.example.myapp")]
    [InlineData("io.opentelemetry.contrib")]
    [InlineData("Company.Product.Component")]
    [InlineData("com.example.my-app")]
    [InlineData("mycompany.v2.tracing")]
    public Task ShouldNotReportValidReverseDnsName(string name) =>
        VerifyAsync($$"""
            {{ActivitySourceSetup}}

            public class C {
                private static readonly System.Diagnostics.ActivitySource Source =
                    new System.Diagnostics.ActivitySource("{{name}}");
            }
            """);

    [Fact]
    public Task ShouldNotReportWhenNameIsNotConstant() =>
        VerifyAsync($$"""
            {{ActivitySourceSetup}}

            public class C {
                private static readonly string SourceName = "company.product";
                private static readonly System.Diagnostics.ActivitySource Source =
                    new System.Diagnostics.ActivitySource(SourceName);
            }
            """);

    [Fact]
    public Task ShouldReportInConstructorWithVersion() =>
        VerifyAsync($$"""
            {{ActivitySourceSetup}}

            public class C {
                private static readonly System.Diagnostics.ActivitySource Source =
                    new System.Diagnostics.ActivitySource([|"InvalidName"|], "1.0.0");
            }
            """);

    [Fact]
    public Task ShouldNotReportForNonActivitySourceCreation() =>
        VerifyAsync("""
            public class MySource {
                public MySource(string name) { }
            }

            public class C {
                private static readonly MySource Source = new MySource("SingleWord");
            }
            """);
}
