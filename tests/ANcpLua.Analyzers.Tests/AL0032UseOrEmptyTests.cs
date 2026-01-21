using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0032: Use OrEmpty() instead of null-coalescing with empty collections.
/// </summary>
public sealed partial class Al0032UseOrEmptyTests : AnalyzerTest<Al0032UseOrEmptyAnalyzer> {
    [Fact]
    public Task ShouldReportArrayEmpty() =>
        VerifyAsync("""
                    using System;
                    using System.Collections.Generic;

                    public class C {
                        IEnumerable<string> M(IEnumerable<string>? items) {
                            return [|items ?? Array.Empty<string>()|];
                        }
                    }
                    """);

    [Fact]
    public Task ShouldReportEnumerableEmpty() =>
        VerifyAsync("""
                    using System.Collections.Generic;
                    using System.Linq;

                    public class C {
                        IEnumerable<int> M(IEnumerable<int>? numbers) {
                            return [|numbers ?? Enumerable.Empty<int>()|];
                        }
                    }
                    """);

    [Fact]
    public Task ShouldReportCollectionExpression() =>
        VerifyAsync("""
                    using System.Collections.Generic;

                    public class C {
                        IEnumerable<object> M(IEnumerable<object>? objects) {
                            return [|objects ?? []|];
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportStringCoalesce() =>
        VerifyAsync("""
                    public class C {
                        string M(string? s) {
                            return s ?? "";
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportNonEmptyArray() =>
        VerifyAsync("""
                    using System.Collections.Generic;

                    public class C {
                        IEnumerable<int> M(IEnumerable<int>? items) {
                            return items ?? new[] { 1, 2, 3 };
                        }
                    }
                    """);

    [Fact]
    public Task ShouldReportWithArrayType() =>
        VerifyAsync("""
                    using System;
                    using System.Collections.Generic;

                    public class C {
                        IEnumerable<string> M(string[]? array) {
                            return [|array ?? Array.Empty<string>()|];
                        }
                    }
                    """);
}
