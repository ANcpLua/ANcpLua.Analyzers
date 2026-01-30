using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Code fix tests for AL0029: HasAttribute().
/// </summary>
public sealed partial class Al0029UseHasAttributeCodeFixTests
    : CodeFixTest<Al0029UseHasAttributeAnalyzer, Al0029UseHasAttributeCodeFixProvider> {
    // Polyfill stubs matching ANcpLua.Roslyn.Utilities signatures
    private const string Polyfills = """
        namespace Microsoft.CodeAnalysis {
            public interface ISymbol {
                System.Collections.Immutable.ImmutableArray<AttributeData> GetAttributes();
            }
            public class AttributeData {
                public INamedTypeSymbol? AttributeClass { get; }
            }
            public interface INamedTypeSymbol {
                string ToDisplayString();
                string Name { get; }
            }
        }
        public static class SymbolExtensions {
            public static bool HasAttribute(this Microsoft.CodeAnalysis.ISymbol symbol, string name)
                => throw null!;
        }
        """;

    [Fact]
    public Task ShouldFixAnyWithToDisplayString() =>
        VerifyAsync(
            $$"""
            using System.Linq;
            {{Polyfills}}
            public class C {
                bool M(Microsoft.CodeAnalysis.ISymbol symbol) {
                    return [|symbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "MyAttribute")|];
                }
            }
            """,
            $$"""
            using System.Linq;
            {{Polyfills}}
            public class C {
                bool M(Microsoft.CodeAnalysis.ISymbol symbol) {
                    return symbol.HasAttribute("MyAttribute");
                }
            }
            """);

    [Fact]
    public Task ShouldFixAnyWithName() =>
        VerifyAsync(
            $$"""
            using System.Linq;
            {{Polyfills}}
            public class C {
                bool M(Microsoft.CodeAnalysis.ISymbol symbol) {
                    return [|symbol.GetAttributes().Any(a => a.AttributeClass?.Name == "Test")|];
                }
            }
            """,
            $$"""
            using System.Linq;
            {{Polyfills}}
            public class C {
                bool M(Microsoft.CodeAnalysis.ISymbol symbol) {
                    return symbol.HasAttribute("Test");
                }
            }
            """);

    [Fact]
    public Task ShouldFixReversedComparison() =>
        VerifyAsync(
            $$"""
            using System.Linq;
            {{Polyfills}}
            public class C {
                bool M(Microsoft.CodeAnalysis.ISymbol symbol) {
                    return [|symbol.GetAttributes().Any(a => "MyAttr" == a.AttributeClass?.ToDisplayString())|];
                }
            }
            """,
            $$"""
            using System.Linq;
            {{Polyfills}}
            public class C {
                bool M(Microsoft.CodeAnalysis.ISymbol symbol) {
                    return symbol.HasAttribute("MyAttr");
                }
            }
            """);

}
