using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1206: Use WhereNotNull() instead of Where with null check.
/// </summary>
public sealed partial class Al1206UseWhereNotNullTests : AnalyzerTest<Al1206UseWhereNotNullAnalyzer> {
    // AL1206 only fires when ANcpLua.Roslyn.Utilities.EnumerableExtensions (which owns WhereNotNull)
    // is present and accessible. Each case appends this stub so the gate is open; the dedicated
    // ShouldNotReportWhenEnumerableExtensionsNotReferenced case omits it to assert the gate itself.
    private const string Stub = """
                                namespace ANcpLua.Roslyn.Utilities { internal static class EnumerableExtensions { } }
                                """;

    private static Task Verify(string body) => VerifyAsync($$"""
                                                            {{body}}
                                                            {{Stub}}
                                                            """);

    [Fact]
    public Task ShouldReportWhereWithNotEqualsNull() =>
        Verify("""
               using System.Collections.Generic;
               using System.Linq;

               public class C {
                   IEnumerable<string> M(IEnumerable<string?> items) {
                       return [|items.Where(x => x != null)|]!;
                   }
               }
               """);

    [Fact]
    public Task ShouldReportWhereWithIsNotNullPattern() =>
        Verify("""
               using System.Collections.Generic;
               using System.Linq;

               public class C {
                   IEnumerable<object> M(IEnumerable<object?> items) {
                       return [|items.Where(x => x is not null)|]!;
                   }
               }
               """);

    [Fact]
    public Task ShouldNotReportWhereWithOtherCondition() =>
        Verify("""
               using System.Collections.Generic;
               using System.Linq;

               public class C {
                   IEnumerable<string> M(IEnumerable<string> items) {
                       return items.Where(x => x.Length > 0);
                   }
               }
               """);

    [Fact]
    public Task ShouldNotReportWhereWithNullEquality() =>
        Verify("""
               using System.Collections.Generic;
               using System.Linq;

               public class C {
                   IEnumerable<string?> M(IEnumerable<string?> items) {
                       // Keep only nulls - not a "where not null" pattern
                       return items.Where(x => x == null);
                   }
               }
               """);

    [Fact]
    public Task ShouldNotReportWhereWithMultipleConditions() =>
        Verify("""
               using System.Collections.Generic;
               using System.Linq;

               public class C {
                   IEnumerable<string> M(IEnumerable<string?> items) {
                       // More complex than just null check
                       return items.Where(x => x != null && x.Length > 0)!;
                   }
               }
               """);

    // The consumer scenario: a project that does not reference ANcpLua.Roslyn.Utilities has no
    // EnumerableExtensions type, so AL1206 must stay silent on correct BCL LINQ code.
    [Fact]
    public Task ShouldNotReportWhenEnumerableExtensionsNotReferenced() =>
        VerifyAsync("""
                    using System.Collections.Generic;
                    using System.Linq;

                    public class C {
                        IEnumerable<string> M(IEnumerable<string?> items) => items.Where(x => x != null)!;
                    }
                    """);
}
