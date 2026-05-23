using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0139: conservative implicit local type style.
/// </summary>
public sealed partial class Al0139UseImplicitTypeWhenApparentTests
    : AnalyzerTest<Al0139UseImplicitTypeWhenApparentAnalyzer> {
    [Theory]
    [InlineData("""
                class C {
                    void M() {
                        {|AL0139:string|} s = "";
                    }
                }
                """)]
    [InlineData("""
                class C {
                    void M() {
                        {|AL0139:int|} i = 0;
                    }
                }
                """)]
    [InlineData("""
                class C {
                    void M(object value) {
                        {|AL0139:string|} s = (string)value;
                    }
                }
                """)]
    [InlineData("""
                class C {
                    void M(object value) {
                        {|AL0139:string|} s = value as string;
                    }
                }
                """)]
    [InlineData("""
                class C {
                    void M() {
                        {|AL0139:C|} c = new C();
                    }
                }
                """)]
    [InlineData("""
                class C {
                    void M() {
                        {|AL0139:int[]|} values = new[] { 1, 2 };
                    }
                }
                """)]
    public Task ShouldReportExplicitTypeWhenTypeIsApparent(string source) => VerifyAsync(source);

    [Theory]
    [InlineData("""
                class C {
                    void M() {
                        string s = null;
                    }
                }
                """)]
    [InlineData("""
                using System;
                class C {
                    void M() {
                        Func<int, int> f = x => x * 2;
                    }
                }
                """)]
    [InlineData("""
                using System;
                class C {
                    void M() {
                        Func<string, string> f = string.Copy;
                    }
                }
                """)]
    [InlineData("""
                class C {
                    void M() {
                        dynamic value = 1;
                    }
                }
                """)]
    [InlineData("""
                class C {
                    void M() {
                        int x = 1, y = 2;
                    }
                }
                """)]
    [InlineData("""
                class C {
                    private int _field = 5;
                }
                """)]
    public Task ShouldNotReportWhenExplicitTypeIsRequiredOrAcceptable(string source) => VerifyAsync(source);

    [Theory]
    [InlineData("""
                class C {
                    object M() {
                        var value = GetValue();
                        return value;
                    }
                    object GetValue() => new object();
                }
                """)]
    [InlineData("""
                class C {
                    void M(string text) {
                        int.TryParse(text, out var value);
                    }
                }
                """)]
    [InlineData("""
                class C {
                    void M(int[] values) {
                        foreach (var value in values) {
                        }
                    }
                }
                """)]
    public Task ShouldNotReportVarWhenTypeIsNotApparent(string source) => VerifyAsync(source);

    [Fact]
    public Task ShouldNotReportWhenVarWouldBindToARealTypeNamedVar() => VerifyAsync("""
        class var {
        }
        class C {
            void M() {
                var value = new var();
            }
        }
        """);

    [Fact]
    public Task ShouldReportWhenExplicitTypeIsTheRealTypeNamedVar() => VerifyAsync("""
        class var<T> {
            void M() {
                {|AL0139:var<int>|} value = new var<int>();
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportWhenImplicitTypingChangesConversionSemantics() => VerifyAsync("""
        class C {
            void M() {
                uint value = 0;
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForDeconstructionVar() => VerifyAsync("""
        class C {
            void M() {
                var (number, text) = (1, "");
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForForEachVarOverAnonymousType() => VerifyAsync("""
        using System.Linq;

        class C {
            void M() {
                var items = new[] { new { Id = 1 }, new { Id = 2 } };
                foreach (var item in items) {
                }
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForForEachVarWithErrorType() {
        const string Source = """
                              using System.Collections.Generic;

                              class C {
                                  IEnumerable<MissingType> GetValues() => null;

                                  void M() {
                                      foreach (var value in GetValues()) {
                                      }
                                  }
                              }
                              """;
        var expected = Microsoft.CodeAnalysis.Testing.DiagnosticResult.CompilerError("CS0246")
            .WithSpan(4, 17, 4, 28)
            .WithArguments("MissingType");

        return VerifyAsync(Source, [], [expected]);
    }
}

/// <summary>
///     Code fix tests for AL0139.
/// </summary>
public sealed partial class Al0139UseImplicitTypeWhenApparentCodeFixTests
    : CodeFixTest<Al0139UseImplicitTypeWhenApparentAnalyzer,
        Al0139UseImplicitTypeWhenApparentCodeFixProvider> {
    [Fact]
    public Task ShouldUseImplicitTypeForApparentInitializer() =>
        VerifyAsync(
            """
            class C {
                void M() {
                    {|AL0139:string|} value = "";
                }
            }
            """,
            """
            class C {
                void M() {
                    var value = "";
                }
            }
            """);
}
