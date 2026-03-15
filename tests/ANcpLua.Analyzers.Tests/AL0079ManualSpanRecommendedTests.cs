using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0079: Manual span recommended for complex async flows.
/// </summary>
public sealed partial class Al0079ManualSpanRecommendedTests : AnalyzerTest<Al0079ManualSpanRecommendedAnalyzer> {
    private const string TracedAttributeDefinition = """
        namespace Qyl.Instrumentation.Instrumentation {
            [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method)]
            public class TracedAttribute : System.Attribute {
                public TracedAttribute(string activitySourceName = "") { }
                public string ActivitySourceName { get; set; } = "";
            }
        }
        """;

    [Fact]
    public Task ShouldReportTaskWhenAll() => VerifyAsync(
        $$"""
        using System.Threading.Tasks;
        using Qyl.Instrumentation.Instrumentation;

        {{TracedAttributeDefinition}}

        [Traced("MyApp")]
        public class MyService {
            public async Task {|AL0079:ProcessAsync|}() {
                var task1 = Task.Delay(100);
                var task2 = Task.Delay(200);
                await Task.WhenAll(task1, task2);
            }
        }
        """);

    [Fact]
    public Task ShouldReportTaskWhenAny() => VerifyAsync(
        $$"""
        using System.Threading.Tasks;
        using Qyl.Instrumentation.Instrumentation;

        {{TracedAttributeDefinition}}

        public class MyService {
            [Traced("MyApp")]
            public async Task {|AL0079:ProcessAsync|}() {
                var task1 = Task.Delay(100);
                var task2 = Task.Delay(200);
                await Task.WhenAny(task1, task2);
            }
        }
        """);

    [Fact]
    public Task ShouldReportParallelForEach() => VerifyAsync(
        $$"""
        using System.Collections.Generic;
        using System.Threading.Tasks;
        using Qyl.Instrumentation.Instrumentation;

        {{TracedAttributeDefinition}}

        [Traced("MyApp")]
        public class MyService {
            public async Task {|AL0079:ProcessAsync|}(IEnumerable<int> items) {
                await Parallel.ForEachAsync(items, async (item, ct) => {
                    await Task.Delay(100, ct);
                });
            }
        }
        """);

    [Fact]
    public Task ShouldReportConfigureAwaitFalse() => VerifyAsync(
        $$"""
        using System.Threading.Tasks;
        using Qyl.Instrumentation.Instrumentation;

        {{TracedAttributeDefinition}}

        [Traced("MyApp")]
        public class MyService {
            public async Task {|AL0079:ProcessAsync|}() {
                await Task.Delay(100).ConfigureAwait(false);
            }
        }
        """);

    [Fact]
    public Task ShouldReportMultipleAwaits() => VerifyAsync(
        $$"""
        using System.Threading.Tasks;
        using Qyl.Instrumentation.Instrumentation;

        {{TracedAttributeDefinition}}

        [Traced("MyApp")]
        public class MyService {
            public async Task {|AL0079:ProcessAsync|}() {
                await Task.Delay(100);
                await Task.Delay(200);
                await Task.Delay(300);
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportSimpleAsyncMethod() => VerifyAsync(
        $$"""
        using System.Threading.Tasks;
        using Qyl.Instrumentation.Instrumentation;

        {{TracedAttributeDefinition}}

        [Traced("MyApp")]
        public class MyService {
            public async Task ProcessAsync() {
                await Task.Delay(100);
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportTwoAwaits() => VerifyAsync(
        $$"""
        using System.Threading.Tasks;
        using Qyl.Instrumentation.Instrumentation;

        {{TracedAttributeDefinition}}

        [Traced("MyApp")]
        public class MyService {
            public async Task ProcessAsync() {
                await Task.Delay(100);
                await Task.Delay(200);
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportWithoutTracedAttribute() => VerifyAsync(
        $$"""
        using System.Threading.Tasks;
        using Qyl.Instrumentation.Instrumentation;

        {{TracedAttributeDefinition}}

        public class MyService {
            public async Task ProcessAsync() {
                var task1 = Task.Delay(100);
                var task2 = Task.Delay(200);
                await Task.WhenAll(task1, task2);
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportSyncMethod() => VerifyAsync(
        $$"""
        using System.Threading.Tasks;
        using Qyl.Instrumentation.Instrumentation;

        {{TracedAttributeDefinition}}

        [Traced("MyApp")]
        public class MyService {
            public void Process() {
                // Non-async method - no await patterns to analyze
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportConfigureAwaitTrue() => VerifyAsync(
        $$"""
        using System.Threading.Tasks;
        using Qyl.Instrumentation.Instrumentation;

        {{TracedAttributeDefinition}}

        [Traced("MyApp")]
        public class MyService {
            public async Task ProcessAsync() {
                await Task.Delay(100).ConfigureAwait(true);
            }
        }
        """);

    [Fact]
    public Task ShouldNotCountAwaitsInNestedLambdas() => VerifyAsync(
        $$"""
        using System;
        using System.Threading.Tasks;
        using Qyl.Instrumentation.Instrumentation;

        {{TracedAttributeDefinition}}

        [Traced("MyApp")]
        public class MyService {
            public async Task ProcessAsync() {
                // Only 1 await in outer method
                await Task.Delay(100);

                // Awaits in lambdas should not count
                Func<Task> lambda = async () => {
                    await Task.Delay(100);
                    await Task.Delay(200);
                    await Task.Delay(300);
                };
            }
        }
        """);

    [Fact]
    public Task ShouldReportMethodWithTracedAttribute() => VerifyAsync(
        $$"""
        using System.Threading.Tasks;
        using Qyl.Instrumentation.Instrumentation;

        {{TracedAttributeDefinition}}

        public class MyService {
            [Traced("MyApp.Orders")]
            public async Task {|AL0079:ProcessOrdersAsync|}() {
                await Task.Delay(100);
                await Task.Delay(200);
                await Task.Delay(300);
            }
        }
        """);
}
