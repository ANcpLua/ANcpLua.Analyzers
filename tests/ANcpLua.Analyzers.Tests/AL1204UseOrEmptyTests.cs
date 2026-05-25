using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1204: Use OrEmpty() instead of null-coalescing with empty collections.
/// </summary>
public sealed partial class Al1204UseOrEmptyTests : AnalyzerTest<Al1204UseOrEmptyAnalyzer> {
    // Stub appended to ShouldReport tests so the analyzer detects OrEmpty() in the compilation
    private const string OrEmptyStub = """

                                       public static class EnumerableExtensions {
                                           public static System.Collections.Generic.IEnumerable<T> OrEmpty<T>(
                                               this System.Collections.Generic.IEnumerable<T>? source)
                                           {
                                               if (source is null) return System.Array.Empty<T>();
                                               return source;
                                           }
                                       }
                                       """;

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
                    """ + OrEmptyStub);

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
                    """ + OrEmptyStub);

    [Fact]
    public Task ShouldReportCollectionExpression() =>
        VerifyAsync("""
                    using System.Collections.Generic;

                    public class C {
                        IEnumerable<object> M(IEnumerable<object>? objects) {
                            return [|objects ?? []|];
                        }
                    }
                    """ + OrEmptyStub);

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
    public Task ShouldNotReportArrayTypeEvenWhenReturnIsIEnumerable() =>
        VerifyAsync("""
                    using System;
                    using System.Collections.Generic;

                    public class C {
                        IEnumerable<string> M(string[]? array) {
                            return array ?? Array.Empty<string>();
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportDictionaryCoalesce() =>
        VerifyAsync("""
                    using System.Collections.Generic;

                    public class C {
                        Dictionary<string, object?> M(Dictionary<string, object?>? dict) {
                            return dict ?? [];
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportListCoalesce() =>
        VerifyAsync("""
                    using System.Collections.Generic;

                    public class C {
                        List<int> M(List<int>? list) {
                            return list ?? [];
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportArrayCoalesce() =>
        VerifyAsync("""
                    using System;

                    public class C {
                        string[] M(string[]? arr) {
                            return arr ?? [];
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportIListCoalesce() =>
        VerifyAsync("""
                    using System.Collections.Generic;

                    public class C {
                        IList<string> M(IList<string>? items) {
                            return items ?? [];
                        }
                    }
                    """);
}
