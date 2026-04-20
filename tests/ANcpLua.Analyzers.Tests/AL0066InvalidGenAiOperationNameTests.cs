using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

public sealed partial class Al0066InvalidGenAiOperationNameTests : AnalyzerTest<Al0066InvalidGenAiOperationNameAnalyzer> {
    [Theory]
    [InlineData("chat")]
    [InlineData("generate_content")]
    [InlineData("text_completion")]
    [InlineData("embeddings")]
    [InlineData("retrieval")]
    [InlineData("create_agent")]
    [InlineData("invoke_agent")]
    [InlineData("execute_tool")]
    [InlineData("invoke_workflow")]
    public Task ShouldNotReportOfficialOperationNames(string operationName) =>
        VerifyAsync($$"""
            public class Activity {
                public void SetTag(string key, object? value) { }
            }

            public class C {
                void M(Activity activity) {
                    activity.SetTag("gen_ai.operation.name", "{{operationName}}");
                }
            }
            """);

    [Fact]
    public Task ShouldReportUnknownOperationName() =>
        VerifyAsync("""
            public class Activity {
                public void SetTag(string key, object? value) { }
            }

            public class C {
                void M(Activity activity) {
                    activity.SetTag("gen_ai.operation.name", [|"summarize"|]);
                }
            }
            """);
}
