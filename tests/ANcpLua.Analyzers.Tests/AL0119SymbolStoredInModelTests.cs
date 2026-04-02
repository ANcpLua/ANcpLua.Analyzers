using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0119: Avoid storing ISymbol in source generator models.
/// </summary>
public sealed partial class Al0119SymbolStoredInModelTests : AnalyzerTest<Al0119SymbolStoredInModelAnalyzer> {
    private const string RoslynStubs = """
                                       namespace Microsoft.CodeAnalysis {
                                           public interface ISymbol { }
                                           public interface ITypeSymbol : ISymbol { }
                                           public interface INamedTypeSymbol : ITypeSymbol { }
                                           public interface IMethodSymbol : ISymbol { }
                                           public interface IFieldSymbol : ISymbol { }
                                           public interface IPropertySymbol : ISymbol { }
                                       }
                                       namespace System.Collections.Immutable {
                                           public struct ImmutableArray<T> { }
                                       }
                                       """;

    [Fact]
    public Task ShouldReportFieldOfISymbol() =>
        VerifyAsync($$"""
                      {{RoslynStubs}}

                      public class Model {
                          public Microsoft.CodeAnalysis.ISymbol [|Symbol|];
                      }
                      """);

    [Fact]
    public Task ShouldReportFieldOfINamedTypeSymbol() =>
        VerifyAsync($$"""
                      {{RoslynStubs}}

                      public class Model {
                          public Microsoft.CodeAnalysis.INamedTypeSymbol [|Type|];
                      }
                      """);

    [Fact]
    public Task ShouldReportPropertyOfIMethodSymbol() =>
        VerifyAsync($$"""
                      {{RoslynStubs}}

                      public class Model {
                          public Microsoft.CodeAnalysis.IMethodSymbol [|Method|] { get; set; }
                      }
                      """);

    [Fact]
    public Task ShouldReportRecordParameterStoringSymbol() =>
        VerifyAsync($$"""
                      {{RoslynStubs}}

                      public record Model(Microsoft.CodeAnalysis.INamedTypeSymbol [|Type|]);
                      """);

    [Fact]
    public Task ShouldReportGenericContainingSymbol() =>
        VerifyAsync($$"""
                      {{RoslynStubs}}

                      public class Model {
                          public System.Collections.Immutable.ImmutableArray<Microsoft.CodeAnalysis.ISymbol> [|Symbols|];
                      }
                      """);

    [Fact]
    public Task ShouldNotReportStringField() =>
        VerifyAsync($$"""
                      {{RoslynStubs}}

                      public class Model {
                          public string Name;
                      }
                      """);

    [Fact]
    public Task ShouldNotReportPrimitiveProperty() =>
        VerifyAsync($$"""
                      {{RoslynStubs}}

                      public class Model {
                          public int Count { get; set; }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenRoslynNotReferenced() =>
        VerifyAsync("""
                    public class Model {
                        public string Name;
                    }
                    """);
}
