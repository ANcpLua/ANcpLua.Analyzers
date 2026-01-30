using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Code fix tests for AL0034: WhereNotNull().
/// </summary>
public sealed partial class Al0034UseWhereNotNullCodeFixTests
    : CodeFixTest<Al0034UseWhereNotNullAnalyzer, Al0034UseWhereNotNullCodeFixProvider> {
    // Polyfill stub - matches ANcpLua.Roslyn.Utilities signature
    private const string ExtensionsPolyfill = """
        #nullable enable
        using System.Collections.Generic;
        using System.Linq;
        public static class EnumerableExtensions {
            public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source)
                => throw null!;
        }
        """;

    [Fact]
    public Task ShouldFixNotEqualsNull() =>
        VerifyAsync(
            $$"""
            #nullable enable
            {{ExtensionsPolyfill}}
            public class C {
                IEnumerable<string> M(IEnumerable<string?> items) {
                    return [|items.Where(x => x != null)|]!;
                }
            }
            """,
            $$"""
            #nullable enable
            {{ExtensionsPolyfill}}
            public class C {
                IEnumerable<string> M(IEnumerable<string?> items) {
                    return items.WhereNotNull()!;
                }
            }
            """);

    [Fact]
    public Task ShouldFixIsNotNull() =>
        VerifyAsync(
            $$"""
            #nullable enable
            {{ExtensionsPolyfill}}
            public class C {
                IEnumerable<object> M(IEnumerable<object?> items) {
                    return [|items.Where(x => x is not null)|]!;
                }
            }
            """,
            $$"""
            #nullable enable
            {{ExtensionsPolyfill}}
            public class C {
                IEnumerable<object> M(IEnumerable<object?> items) {
                    return items.WhereNotNull()!;
                }
            }
            """);
}
