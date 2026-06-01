using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1205: Use ToImmutableArrayOrEmpty() instead of null-conditional with fallback.
/// </summary>
public sealed partial class Al1205UseToImmutableArrayOrEmptyTests
    : AnalyzerTest<Al1205UseToImmutableArrayOrEmptyAnalyzer> {
    // AL1205 only fires when ANcpLua.Roslyn.Utilities.EnumerableExtensions (which owns
    // ToImmutableArrayOrEmpty) is present and accessible. Each case appends this stub so the gate
    // is open; the dedicated ShouldNotReportWhenEnumerableExtensionsNotReferenced case omits it.
    private const string Stub = """
                                namespace ANcpLua.Roslyn.Utilities { internal static class EnumerableExtensions { } }
                                """;

    private static Task Verify(string body) => VerifyAsync($$"""
                                                            {{body}}
                                                            {{Stub}}
                                                            """);

    [Fact]
    public Task ShouldReportNullConditionalToImmutableArrayWithEmptyFallback() =>
        Verify("""
               using System.Collections.Generic;
               using System.Collections.Immutable;

               public class C {
                   ImmutableArray<int> M(IEnumerable<int>? items) {
                       return [|items?.ToImmutableArray() ?? ImmutableArray<int>.Empty|];
                   }
               }
               """);

    [Fact]
    public Task ShouldReportWithDefaultFallback() =>
        Verify("""
               using System.Collections.Generic;
               using System.Collections.Immutable;

               public class C {
                   ImmutableArray<string> M(IEnumerable<string>? items) {
                       return [|items?.ToImmutableArray() ?? default|];
                   }
               }
               """);

    [Fact]
    public Task ShouldNotReportPlainToImmutableArray() =>
        Verify("""
               using System.Collections.Generic;
               using System.Collections.Immutable;

               public class C {
                   ImmutableArray<int> M(IEnumerable<int> items) {
                       return items.ToImmutableArray();
                   }
               }
               """);

    [Fact]
    public Task ShouldNotReportNonImmutableArrayFallback() =>
        Verify("""
               using System.Collections.Generic;
               using System.Collections.Immutable;

               public class C {
                   ImmutableArray<int> M(IEnumerable<int>? items) {
                       return items?.ToImmutableArray() ?? new ImmutableArray<int>();
                   }
               }
               """);

    // The consumer scenario: a project that does not reference ANcpLua.Roslyn.Utilities has no
    // EnumerableExtensions type, so AL1205 must stay silent.
    [Fact]
    public Task ShouldNotReportWhenEnumerableExtensionsNotReferenced() =>
        VerifyAsync("""
                    using System.Collections.Generic;
                    using System.Collections.Immutable;

                    public class C {
                        ImmutableArray<int> M(IEnumerable<int>? items) =>
                            items?.ToImmutableArray() ?? ImmutableArray<int>.Empty;
                    }
                    """);
}
