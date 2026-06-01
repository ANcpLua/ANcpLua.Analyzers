using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1204: Use OrEmpty() instead of null-coalescing with empty collections.
/// </summary>
public sealed partial class Al1204UseOrEmptyTests : AnalyzerTest<Al1204UseOrEmptyAnalyzer> {
    // AL1204 only fires when ANcpLua.Roslyn.Utilities.EnumerableExtensions (which owns OrEmpty) is
    // present and accessible. Each case appends this stub so the gate is open; the dedicated
    // ShouldNotReportWhenEnumerableExtensionsNotReferenced case omits it to assert the gate itself.
    private const string Stub = """
                                namespace ANcpLua.Roslyn.Utilities { internal static class EnumerableExtensions { } }
                                """;

    private static Task Verify(string body) => VerifyAsync($$"""
                                                            {{body}}
                                                            {{Stub}}
                                                            """);

    [Fact]
    public Task ShouldReportArrayEmpty() =>
        Verify("""
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
        Verify("""
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
        Verify("""
               using System.Collections.Generic;

               public class C {
                   IEnumerable<object> M(IEnumerable<object>? objects) {
                       return [|objects ?? []|];
                   }
               }
               """);

    [Fact]
    public Task ShouldNotReportStringCoalesce() =>
        Verify("""
               public class C {
                   string M(string? s) {
                       return s ?? "";
                   }
               }
               """);

    [Fact]
    public Task ShouldNotReportNonEmptyArray() =>
        Verify("""
               using System.Collections.Generic;

               public class C {
                   IEnumerable<int> M(IEnumerable<int>? items) {
                       return items ?? new[] { 1, 2, 3 };
                   }
               }
               """);

    [Fact]
    public Task ShouldNotReportArrayTypeEvenWhenReturnIsIEnumerable() =>
        Verify("""
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
        Verify("""
               using System.Collections.Generic;

               public class C {
                   Dictionary<string, object?> M(Dictionary<string, object?>? dict) {
                       return dict ?? [];
                   }
               }
               """);

    [Fact]
    public Task ShouldNotReportListCoalesce() =>
        Verify("""
               using System.Collections.Generic;

               public class C {
                   List<int> M(List<int>? list) {
                       return list ?? [];
                   }
               }
               """);

    [Fact]
    public Task ShouldNotReportArrayCoalesce() =>
        Verify("""
               using System;

               public class C {
                   string[] M(string[]? arr) {
                       return arr ?? [];
                   }
               }
               """);

    [Fact]
    public Task ShouldNotReportIListCoalesce() =>
        Verify("""
               using System.Collections.Generic;

               public class C {
                   IList<string> M(IList<string>? items) {
                       return items ?? [];
                   }
               }
               """);

    // The consumer scenario: a project that does not reference ANcpLua.Roslyn.Utilities has no
    // EnumerableExtensions type, so AL1204 must stay silent on correct BCL LINQ code.
    [Fact]
    public Task ShouldNotReportWhenEnumerableExtensionsNotReferenced() =>
        VerifyAsync("""
                    using System;
                    using System.Collections.Generic;

                    public class C {
                        IEnumerable<string> M(IEnumerable<string>? items) => items ?? Array.Empty<string>();
                    }
                    """);
}
