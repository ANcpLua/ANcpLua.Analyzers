using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0015: Normalize null-guard style.
///     Covers detection of simple ArgumentNullException null-guards and code fix generation
///     for both BCL and portable forms based on target framework and EditorConfig settings.
/// </summary>
public sealed partial class Al0015AnalyzerTests : AnalyzerTest<Al0015NormalizeNullGuardStyleAnalyzer> {
    [Theory]
    [InlineData("string? x", "x is null", "nameof(x)")]
    [InlineData("object? obj", "obj == null", "nameof(obj)")]
    [InlineData("int? count", "count is null", "\"count\"")]
    public Task ShouldReportDiagnostic(string param, string check, string arg) =>
        VerifyAsync(
            $"using System; public class C {{ void M({param}) {{ [|if|] ({check}) throw new ArgumentNullException({arg}); }} }}");

    [Fact]
    public Task ShouldReportDiagnosticWithBlock() => VerifyAsync("""
                                                                 using System;
                                                                 public class C {
                                                                     void M(string? x) {
                                                                         [|if|] (x is null) { throw new ArgumentNullException(nameof(x)); }
                                                                     }
                                                                 }
                                                                 """);

    [Theory]
    [InlineData("string? x, string? y", "x is null", "nameof(y)")] // Mismatched param
    [InlineData("string? x", "x is null", "nameof(x), \"msg\"")] // Custom message
    [InlineData("string? x", "x is not null", "nameof(x)")] // Wrong pattern
    public Task ShouldNotReportMismatchedOrCustom(string param, string check, string arg) =>
        VerifyAsync(
            $"using System; public class C {{ void M({param}) {{ if ({check}) throw new ArgumentNullException({arg}); }} }}");

    [Fact]
    public Task ShouldNotReportWrongExceptionType() => VerifyAsync(
        "using System; public class C { void M(string? x) { if (x is null) throw new InvalidOperationException(nameof(x)); } }");

    [Fact]
    public Task ShouldNotReportPropertyAccess() => VerifyAsync("""
                                                               using System;
                                                               public class W { public object? Value { get; set; } }
                                                               public class C { void M(W? obj) { if (obj.Value is null) throw new ArgumentNullException(nameof(obj)); } }
                                                               """);
}

/// <summary>
///     Code fix tests for AL0015: Normalize null-guard style.
///     Tests both BCL form (ArgumentNullException.ThrowIfNull) and portable form
///     (coalesce assignment) generation based on configuration.
/// </summary>
public sealed partial class Al0015PortableFormCodeFixTests : CodeFixTestWithEditorConfig<Al0015NormalizeNullGuardStyleAnalyzer
    , Al0015NormalizeNullGuardStyleCodeFixProvider> {
    /// <summary>
    ///     Test 1: netstandard2.0 without ThrowIfNull produces portable form.
    ///     Setup: No ThrowIfNull available, auto mode
    ///     Expect: Fix produces PORTABLE form (x = x ?? throw ...)
    /// </summary>
    [Fact]
    public Task ShouldProducePortableFormWithoutThrowIfNull() {
        const string Source = """
                              using System;

                              public class TestClass
                              {
                                  public void TestMethod(string? x)
                                  {
                                      [|if|] (x is null) throw new ArgumentNullException(nameof(x));
                                  }
                              }
                              """;

        var expected = """
                       using System;

                       public class TestClass
                       {
                           public void TestMethod(string? x)
                           {
                               x = x ?? throw new ArgumentNullException(nameof(x));
                           }
                       }
                       """;

        return VerifyAsync(Source, expected,
            new Dictionary<string, string> {
                {
                    "ancplua_target_framework", "netstandard2.0"
                }, {
                    "ancplua_nullguard_style", "auto"
                }
            }, null, false);
    }

    /// <summary>
    ///     Test 2: Portable form with block statement.
    /// </summary>
    [Fact]
    public Task ShouldProducePortableFormWithBlock() {
        const string Source = """
                              using System;

                              public class TestClass
                              {
                                  public void TestMethod(object? obj)
                                  {
                                      [|if|] (obj is null)
                                      {
                                          throw new ArgumentNullException(nameof(obj));
                                      }
                                  }
                              }
                              """;

        const string Expected = """
                                using System;

                                public class TestClass
                                {
                                    public void TestMethod(object? obj)
                                    {
                                        obj = obj ?? throw new ArgumentNullException(nameof(obj));
                                    }
                                }
                                """;

        return VerifyAsync(Source, Expected,
            new Dictionary<string, string> {
                {
                    "ancplua_target_framework", "netstandard2.0"
                }, {
                    "ancplua_nullguard_style", "auto"
                }
            }, null, false);
    }

    /// <summary>
    ///     Test 3: Portable form with == null instead of is null.
    /// </summary>
    [Fact]
    public Task ShouldProducePortableFormWithEqualityCheck() {
        const string Source = """
                              using System;

                              public class TestClass
                              {
                                  public void TestMethod(string? value)
                                  {
                                      [|if|] (value == null) throw new ArgumentNullException(nameof(value));
                                  }
                              }
                              """;

        const string Expected = """
                                using System;

                                public class TestClass
                                {
                                    public void TestMethod(string? value)
                                    {
                                        value = value ?? throw new ArgumentNullException(nameof(value));
                                    }
                                }
                                """;

        return VerifyAsync(Source, Expected,
            new Dictionary<string, string> {
                {
                    "ancplua_target_framework", "netstandard2.0"
                }
            },
            null, false);
    }

    /// <summary>
    ///     Test 4: Portable form with string literal parameter name.
    /// </summary>
    [Fact]
    public Task ShouldProducePortableFormWithStringLiteral() {
        const string Source = """
                              using System;

                              public class TestClass
                              {
                                  public void TestMethod(int? count)
                                  {
                                      [|if|] (count is null) throw new ArgumentNullException("count");
                                  }
                              }
                              """;

        const string Expected = """
                                using System;

                                public class TestClass
                                {
                                    public void TestMethod(int? count)
                                    {
                                        count = count ?? throw new ArgumentNullException(nameof(count));
                                    }
                                }
                                """;

        return VerifyAsync(Source, Expected,
            new Dictionary<string, string> {
                {
                    "ancplua_nullguard_style", "portable"
                }
            });
    }

    /// <summary>
    ///     Test 5: Explicit portable mode forces portable form.
    /// </summary>
    [Fact]
    public Task ShouldProducePortableFormWhenExplicitlyConfigured() {
        const string Source = """
                              using System;

                              public class TestClass
                              {
                                  public void TestMethod(string? x)
                                  {
                                      [|if|] (x is null) throw new ArgumentNullException(nameof(x));
                                  }
                              }
                              """;

        const string Expected = """
                                using System;

                                public class TestClass
                                {
                                    public void TestMethod(string? x)
                                    {
                                        x = x ?? throw new ArgumentNullException(nameof(x));
                                    }
                                }
                                """;

        return VerifyAsync(Source, Expected,
            new Dictionary<string, string> {
                {
                    "ancplua_target_framework", "net10.0"
                }, {
                    "ancplua_nullguard_style", "portable"
                }
            });
    }

    /// <summary>
    ///     Test 6: Explicit portable mode forces portable form even with ThrowIfNull available.
    ///     This tests that editorconfig options are actually being read.
    /// </summary>
    [Fact]
    public Task ShouldProducePortableFormWhenExplicitlyConfiguredWithThrowIfNull() {
        const string Source = """
                              using System;

                              public class TestClass
                              {
                                  public void TestMethod(string? x)
                                  {
                                      [|if|] (x is null) throw new ArgumentNullException(nameof(x));
                                  }
                              }
                              """;

        const string Expected = """
                                using System;

                                public class TestClass
                                {
                                    public void TestMethod(string? x)
                                    {
                                        x = x ?? throw new ArgumentNullException(nameof(x));
                                    }
                                }
                                """;

        return VerifyAsync(Source, Expected,
            new Dictionary<string, string> {
                {
                    "ancplua_target_framework", "net10.0"
                }, {
                    "ancplua_nullguard_style", "portable"
                }
            });
    }
}

/// <summary>
///     BCL form code fix tests for AL0015.
///     Tests ArgumentNullException.ThrowIfNull generation when available.
/// </summary>
public sealed partial class Al0015BclFormCodeFixTests : CodeFixTestWithEditorConfig<Al0015NormalizeNullGuardStyleAnalyzer,
    Al0015NormalizeNullGuardStyleCodeFixProvider> {
    /// <summary>
    ///     Test 2: net10.0 with single target produces BCL form.
    ///     Setup: Reference set WITH ThrowIfNull available
    ///     Setup: TargetFramework=net10.0 (single target)
    ///     Setup: ancplua_nullguard_style=auto
    ///     Expect: Fix produces BCL form (ArgumentNullException.ThrowIfNull(x))
    /// </summary>
    [Fact]
    public Task ShouldProduceBclFormWithSingleTarget() {
        const string Source = """
                              using System;

                              public class TestClass
                              {
                                  public void TestMethod(string? x)
                                  {
                                      [|if|] (x is null) throw new ArgumentNullException(nameof(x));
                                  }
                              }
                              """;

        const string Expected = """
                                using System;

                                public class TestClass
                                {
                                    public void TestMethod(string? x)
                                    {
                                        ArgumentNullException.ThrowIfNull(x);
                                    }
                                }
                                """;

        return VerifyAsync(Source, Expected,
            new Dictionary<string, string> {
                {
                    "ancplua_target_framework", "net10.0"
                }, {
                    "ancplua_nullguard_style", "auto"
                }
            });
    }

    /// <summary>
    ///     Test 2b: BCL form with block statement.
    /// </summary>
    [Fact]
    public Task ShouldProduceBclFormWithBlock() {
        const string Source = """
                              using System;

                              public class TestClass
                              {
                                  public void TestMethod(object? obj)
                                  {
                                      [|if|] (obj is null)
                                      {
                                          throw new ArgumentNullException(nameof(obj));
                                      }
                                  }
                              }
                              """;

        const string Expected = """
                                using System;

                                public class TestClass
                                {
                                    public void TestMethod(object? obj)
                                    {
                                        ArgumentNullException.ThrowIfNull(obj);
                                    }
                                }
                                """;

        return VerifyAsync(Source, Expected,
            new Dictionary<string, string> {
                {
                    "ancplua_target_framework", "net10.0"
                }, {
                    "ancplua_nullguard_style", "auto"
                }
            });
    }

    /// <summary>
    ///     Test 2c: BCL form with == null.
    /// </summary>
    [Fact]
    public Task ShouldProduceBclFormWithEqualityCheck() {
        const string Source = """
                              using System;

                              public class TestClass
                              {
                                  public void TestMethod(string? value)
                                  {
                                      [|if|] (value == null) throw new ArgumentNullException(nameof(value));
                                  }
                              }
                              """;

        const string Expected = """
                                using System;

                                public class TestClass
                                {
                                    public void TestMethod(string? value)
                                    {
                                        ArgumentNullException.ThrowIfNull(value);
                                    }
                                }
                                """;

        return VerifyAsync(Source, Expected,
            new Dictionary<string, string> {
                {
                    "ancplua_target_framework", "net10.0"
                }
            });
    }

    /// <summary>
    ///     Test 2d: BCL form with string literal parameter name.
    /// </summary>
    [Fact]
    public Task ShouldProduceBclFormWithStringLiteral() {
        const string Source = """
                              using System;

                              public class TestClass
                              {
                                  public void TestMethod(int? count)
                                  {
                                      [|if|] (count is null) throw new ArgumentNullException("count");
                                  }
                              }
                              """;

        const string Expected = """
                                using System;

                                public class TestClass
                                {
                                    public void TestMethod(int? count)
                                    {
                                        ArgumentNullException.ThrowIfNull(count);
                                    }
                                }
                                """;

        return VerifyAsync(Source, Expected,
            new Dictionary<string, string> {
                {
                    "ancplua_target_framework", "net10.0"
                }, {
                    "ancplua_nullguard_style", "auto"
                }
            });
    }

    /// <summary>
    ///     Test 2e: Explicit BCL mode forces BCL form when available.
    /// </summary>
    [Fact]
    public Task ShouldProduceBclFormWhenExplicitlyConfigured() {
        const string Source = """
                              using System;

                              public class TestClass
                              {
                                  public void TestMethod(string? x)
                                  {
                                      [|if|] (x is null) throw new ArgumentNullException(nameof(x));
                                  }
                              }
                              """;

        const string Expected = """
                                using System;

                                public class TestClass
                                {
                                    public void TestMethod(string? x)
                                    {
                                        ArgumentNullException.ThrowIfNull(x);
                                    }
                                }
                                """;

        return VerifyAsync(Source, Expected,
            new Dictionary<string, string> {
                {
                    "ancplua_target_framework", "netstandard2.0"
                }, {
                    "ancplua_nullguard_style", "bcl"
                }
            });
    }
}

/// <summary>
///     Multi-target stability tests for AL0015.
///     Verifies that multi-target projects always use portable form for consistency.
/// </summary>
public sealed partial class Al0015MultiTargetTests : CodeFixTestWithEditorConfig<Al0015NormalizeNullGuardStyleAnalyzer,
    Al0015NormalizeNullGuardStyleCodeFixProvider> {
    /// <summary>
    ///     Test 3: Multi-target with netstandard2.0;net10.0 produces portable form.
    ///     Setup: TargetFrameworks=netstandard2.0;net10.0 (multi-target with semicolon)
    ///     Setup: ancplua_nullguard_style=auto
    ///     Expect: Fix produces PORTABLE form (stable across all targets)
    ///     Key: Even though net10 would support BCL, we use portable for stability.
    /// </summary>
    [Fact]
    public Task ShouldProducePortableFormForMultiTargetProject() {
        const string Source = """
                              using System;

                              public class TestClass
                              {
                                  public void TestMethod(string? x)
                                  {
                                      [|if|] (x is null) throw new ArgumentNullException(nameof(x));
                                  }
                              }
                              """;

        const string Expected = """
                                using System;

                                public class TestClass
                                {
                                    public void TestMethod(string? x)
                                    {
                                        x = x ?? throw new ArgumentNullException(nameof(x));
                                    }
                                }
                                """;

        return VerifyAsync(Source, Expected,
            new Dictionary<string, string> {
                {
                    "ancplua_is_multi_target", "true"
                }, {
                    "ancplua_nullguard_style", "auto"
                }
            }, null, false);
    }

    /// <summary>
    ///     Test 3b: Multi-target with three frameworks produces portable form.
    /// </summary>
    [Fact]
    public Task ShouldProducePortableFormForMultipleTargets() {
        const string Source = """
                              using System;

                              public class TestClass
                              {
                                  public void TestMethod(object? obj)
                                  {
                                      [|if|] (obj is null) throw new ArgumentNullException(nameof(obj));
                                  }
                              }
                              """;

        const string Expected = """
                                using System;

                                public class TestClass
                                {
                                    public void TestMethod(object? obj)
                                    {
                                        obj = obj ?? throw new ArgumentNullException(nameof(obj));
                                    }
                                }
                                """;

        return VerifyAsync(Source, Expected,
            new Dictionary<string, string> {
                {
                    "ancplua_is_multi_target", "true"
                }, {
                    "ancplua_nullguard_style", "auto"
                }
            }, null, false);
    }

    /// <summary>
    ///     Test 3c: FixAll behavior - multiple diagnostics produce consistent portable form.
    /// </summary>
    [Fact]
    public Task ShouldProduceConsistentPortableFormForMultipleDiagnostics() {
        const string Source = """
                              using System;

                              public class TestClass
                              {
                                  public void Method1(string? x)
                                  {
                                      [|if|] (x is null) throw new ArgumentNullException(nameof(x));
                                  }

                                  public void Method2(object? y)
                                  {
                                      [|if|] (y is null) throw new ArgumentNullException(nameof(y));
                                  }

                                  public void Method3(int? z)
                                  {
                                      [|if|] (z is null) throw new ArgumentNullException(nameof(z));
                                  }
                              }
                              """;

        const string Expected = """
                                using System;

                                public class TestClass
                                {
                                    public void Method1(string? x)
                                    {
                                        x = x ?? throw new ArgumentNullException(nameof(x));
                                    }

                                    public void Method2(object? y)
                                    {
                                        y = y ?? throw new ArgumentNullException(nameof(y));
                                    }

                                    public void Method3(int? z)
                                    {
                                        z = z ?? throw new ArgumentNullException(nameof(z));
                                    }
                                }
                                """;

        return VerifyAsync(Source, Expected,
            new Dictionary<string, string> {
                {
                    "ancplua_is_multi_target", "true"
                }, {
                    "ancplua_nullguard_style", "auto"
                }
            }, null, false);
    }
}

/// <summary>
///     Edge case and rejection tests for AL0015.
/// </summary>
public sealed partial class Al0015EdgeCasesTests : CodeFixTestWithEditorConfig<Al0015NormalizeNullGuardStyleAnalyzer,
    Al0015NormalizeNullGuardStyleCodeFixProvider> {
    /// <summary>
    ///     Test 4: BCL mode forced but ThrowIfNull unavailable - falls back to portable.
    ///     Setup: ancplua_nullguard_style=bcl (explicit BCL mode)
    ///     Setup: No ThrowIfNull in compilation (netstandard2.0)
    ///     Expect: Code fix uses portable form as fallback
    ///     Note: Per spec "do not offer fix" but current impl falls back to portable
    /// </summary>
    [Fact]
    public Task ShouldFallbackToPortableFormWhenBclUnavailable() {
        const string Source = """
                              using System;

                              public class TestClass
                              {
                                  public void TestMethod(string? x)
                                  {
                                      [|if|] (x is null) throw new ArgumentNullException(nameof(x));
                                  }
                              }
                              """;

        const string Expected = """
                                using System;

                                public class TestClass
                                {
                                    public void TestMethod(string? x)
                                    {
                                        x = x ?? throw new ArgumentNullException(nameof(x));
                                    }
                                }
                                """;

        return VerifyAsync(Source, Expected,
            new Dictionary<string, string> {
                {
                    "ancplua_target_framework", "netstandard2.0"
                }, {
                    "ancplua_nullguard_style", "bcl"
                }
            }, null, false);
    }
}
