using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using System;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0015: Normalize null-guard style.
///     Covers detection of simple ArgumentNullException null-guards and code fix generation
///     for both BCL and portable forms based on target framework and EditorConfig settings.
/// </summary>
public sealed class AL0015AnalyzerTests : ALAnalyzerTest<AL0015NormalizeNullGuardStyleAnalyzer>
{
    [Theory]
    [InlineData("""
                using System;

                public class TestClass
                {
                    public void TestMethod(string? x)
                    {
                        [|if|] (x is null) throw new ArgumentNullException(nameof(x));
                    }
                }
                """)]
    [InlineData("""
                using System;

                public class TestClass
                {
                    public void TestMethod(object? obj)
                    {
                        [|if|] (obj == null) throw new ArgumentNullException(nameof(obj));
                    }
                }
                """)]
    [InlineData("""
                using System;

                public class TestClass
                {
                    public void TestMethod(string? x)
                    {
                        [|if|] (x is null)
                        {
                            throw new ArgumentNullException(nameof(x));
                        }
                    }
                }
                """)]
    [InlineData("""
                using System;

                public class TestClass
                {
                    public void TestMethod(int? count)
                    {
                        [|if|] (count is null) throw new ArgumentNullException("count");
                    }
                }
                """)]
    public Task ShouldReportDiagnostic(string source)
    {
        return VerifyAsync(source);
    }

    [Theory]
    [InlineData("""
                using System;

                public class TestClass
                {
                    public void TestMethod(string? x, string? y)
                    {
                        if (x is null) throw new ArgumentNullException(nameof(y));
                    }
                }
                """)]
    [InlineData("""
                using System;

                public class TestClass
                {
                    public void TestMethod(string? x)
                    {
                        if (x is null) throw new ArgumentNullException(nameof(x), "custom message");
                    }
                }
                """)]
    [InlineData("""
                using System;

                public class TestClass
                {
                    public void TestMethod(string? x)
                    {
                        if (x is null) throw new InvalidOperationException(nameof(x));
                    }
                }
                """)]
    [InlineData("""
                using System;

                public class TestClass
                {
                    public void TestMethod(object? obj)
                    {
                        if (obj.Value is null) throw new ArgumentNullException(nameof(obj));
                    }
                }
                """)]
    [InlineData("""
                using System;

                public class TestClass
                {
                    public void TestMethod(string? x)
                    {
                        if (x is not null) throw new ArgumentNullException(nameof(x));
                    }
                }
                """)]
    public Task ShouldNotReportDiagnostic(string source)
    {
        return VerifyAsync(source);
    }
}

/// <summary>
///     Code fix tests for AL0015: Normalize null-guard style.
///     Tests both BCL form (ArgumentNullException.ThrowIfNull) and portable form
///     (coalesce assignment) generation based on configuration.
/// </summary>
public sealed class AL0015PortableFormCodeFixTests : ALCodeFixTestWithEditorConfig<AL0015NormalizeNullGuardStyleAnalyzer, AL0015NormalizeNullGuardStyleCodeFixProvider>
{
    /// <summary>
    ///     Test 1: netstandard2.0 without ThrowIfNull produces portable form.
    ///     Setup: No ThrowIfNull available, auto mode
    ///     Expect: Fix produces PORTABLE form (x = x ?? throw ...)
    /// </summary>
    [Fact]
    public Task ShouldProducePortableFormWithoutThrowIfNull()
    {
        var source = """
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

        return VerifyAsync(source, expected, editorConfig: new Dictionary<string, string>
        {
            { "build_property.TargetFramework", "netstandard2.0" },
            { "ancplua_nullguard_style", "auto" }
        });
    }

    /// <summary>
    ///     Test 2: Portable form with block statement.
    /// </summary>
    [Fact]
    public Task ShouldProducePortableFormWithBlock()
    {
        var source = """
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

        var expected = """
                       public class TestClass
                       {
                           public void TestMethod(object? obj)
                           {
                               obj = obj ?? throw new ArgumentNullException(nameof(obj));
                           }
                       }
                       """;

        return VerifyAsync(source, expected, editorConfig: new Dictionary<string, string>
        {
            { "build_property.TargetFramework", "netstandard2.0" },
            { "ancplua_nullguard_style", "auto" }
        });
    }

    /// <summary>
    ///     Test 3: Portable form with == null instead of is null.
    /// </summary>
    [Fact]
    public Task ShouldProducePortableFormWithEqualityCheck()
    {
        var source = """
                     public class TestClass
                     {
                         public void TestMethod(string? value)
                         {
                             [|if|] (value == null) throw new ArgumentNullException(nameof(value));
                         }
                     }
                     """;

        var expected = """
                       public class TestClass
                       {
                           public void TestMethod(string? value)
                           {
                               value = value ?? throw new ArgumentNullException(nameof(value));
                           }
                       }
                       """;

        return VerifyAsync(source, expected, editorConfig: new Dictionary<string, string>
        {
            { "build_property.TargetFramework", "netstandard2.0" }
        });
    }

    /// <summary>
    ///     Test 4: Portable form with string literal parameter name.
    /// </summary>
    [Fact]
    public Task ShouldProducePortableFormWithStringLiteral()
    {
        var source = """
                     public class TestClass
                     {
                         public void TestMethod(int? count)
                         {
                             [|if|] (count is null) throw new ArgumentNullException("count");
                         }
                     }
                     """;

        var expected = """
                       public class TestClass
                       {
                           public void TestMethod(int? count)
                           {
                               count = count ?? throw new ArgumentNullException("count");
                           }
                       }
                       """;

        return VerifyAsync(source, expected, editorConfig: new Dictionary<string, string>
        {
            { "ancplua_nullguard_style", "portable" }
        });
    }

    /// <summary>
    ///     Test 5: Explicit portable mode forces portable form.
    /// </summary>
    [Fact]
    public Task ShouldProducePortableFormWhenExplicitlyConfigured()
    {
        var source = """
                     public class TestClass
                     {
                         public void TestMethod(string? x)
                         {
                             [|if|] (x is null) throw new ArgumentNullException(nameof(x));
                         }
                     }
                     """;

        var expected = """
                       public class TestClass
                       {
                           public void TestMethod(string? x)
                           {
                               x = x ?? throw new ArgumentNullException(nameof(x));
                           }
                       }
                       """;

        return VerifyAsync(source, expected, editorConfig: new Dictionary<string, string>
        {
            { "build_property.TargetFramework", "net10.0" },
            { "ancplua_nullguard_style", "portable" }
        });
    }
}

/// <summary>
///     BCL form code fix tests for AL0015.
///     Tests ArgumentNullException.ThrowIfNull generation when available.
/// </summary>
public sealed class AL0015BclFormCodeFixTests : ALCodeFixTestWithEditorConfig<AL0015NormalizeNullGuardStyleAnalyzer, AL0015NormalizeNullGuardStyleCodeFixProvider>
{
    /// <summary>
    ///     Test 2: net10.0 with single target produces BCL form.
    ///     Setup: Reference set WITH ThrowIfNull available
    ///     Setup: TargetFramework=net10.0 (single target)
    ///     Setup: ancplua_nullguard_style=auto
    ///     Expect: Fix produces BCL form (ArgumentNullException.ThrowIfNull(x))
    /// </summary>
    [Fact]
    public Task ShouldProduceBclFormWithSingleTarget()
    {
        var source = """
                     public class TestClass
                     {
                         public void TestMethod(string? x)
                         {
                             [|if|] (x is null) throw new ArgumentNullException(nameof(x));
                         }
                     }
                     """;

        var expected = """
                       public class TestClass
                       {
                           public void TestMethod(string? x)
                           {
                               ArgumentNullException.ThrowIfNull(x);
                           }
                       }
                       """;

        return VerifyAsync(source, expected, editorConfig: new Dictionary<string, string>
        {
            { "build_property.TargetFramework", "net10.0" },
            { "ancplua_nullguard_style", "auto" }
        }, includeThrowIfNullReference: true);
    }

    /// <summary>
    ///     Test 2b: BCL form with block statement.
    /// </summary>
    [Fact]
    public Task ShouldProduceBclFormWithBlock()
    {
        var source = """
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

        var expected = """
                       public class TestClass
                       {
                           public void TestMethod(object? obj)
                           {
                               ArgumentNullException.ThrowIfNull(obj);
                           }
                       }
                       """;

        return VerifyAsync(source, expected, editorConfig: new Dictionary<string, string>
        {
            { "build_property.TargetFramework", "net10.0" },
            { "ancplua_nullguard_style", "auto" }
        }, includeThrowIfNullReference: true);
    }

    /// <summary>
    ///     Test 2c: BCL form with == null.
    /// </summary>
    [Fact]
    public Task ShouldProduceBclFormWithEqualityCheck()
    {
        var source = """
                     public class TestClass
                     {
                         public void TestMethod(string? value)
                         {
                             [|if|] (value == null) throw new ArgumentNullException(nameof(value));
                         }
                     }
                     """;

        var expected = """
                       public class TestClass
                       {
                           public void TestMethod(string? value)
                           {
                               ArgumentNullException.ThrowIfNull(value);
                           }
                       }
                       """;

        return VerifyAsync(source, expected, editorConfig: new Dictionary<string, string>
        {
            { "build_property.TargetFramework", "net10.0" }
        }, includeThrowIfNullReference: true);
    }

    /// <summary>
    ///     Test 2d: BCL form with string literal parameter name.
    /// </summary>
    [Fact]
    public Task ShouldProduceBclFormWithStringLiteral()
    {
        var source = """
                     public class TestClass
                     {
                         public void TestMethod(int? count)
                         {
                             [|if|] (count is null) throw new ArgumentNullException("count");
                         }
                     }
                     """;

        var expected = """
                       public class TestClass
                       {
                           public void TestMethod(int? count)
                           {
                               ArgumentNullException.ThrowIfNull(count);
                           }
                       }
                       """;

        return VerifyAsync(source, expected, editorConfig: new Dictionary<string, string>
        {
            { "build_property.TargetFramework", "net10.0" },
            { "ancplua_nullguard_style", "auto" }
        }, includeThrowIfNullReference: true);
    }

    /// <summary>
    ///     Test 2e: Explicit BCL mode forces BCL form when available.
    /// </summary>
    [Fact]
    public Task ShouldProduceBclFormWhenExplicitlyConfigured()
    {
        var source = """
                     public class TestClass
                     {
                         public void TestMethod(string? x)
                         {
                             [|if|] (x is null) throw new ArgumentNullException(nameof(x));
                         }
                     }
                     """;

        var expected = """
                       public class TestClass
                       {
                           public void TestMethod(string? x)
                           {
                               ArgumentNullException.ThrowIfNull(x);
                           }
                       }
                       """;

        return VerifyAsync(source, expected, editorConfig: new Dictionary<string, string>
        {
            { "build_property.TargetFramework", "netstandard2.0" },
            { "ancplua_nullguard_style", "bcl" }
        }, includeThrowIfNullReference: true);
    }
}

/// <summary>
///     Multi-target stability tests for AL0015.
///     Verifies that multi-target projects always use portable form for consistency.
/// </summary>
public sealed class AL0015MultiTargetTests : ALCodeFixTestWithEditorConfig<AL0015NormalizeNullGuardStyleAnalyzer, AL0015NormalizeNullGuardStyleCodeFixProvider>
{
    /// <summary>
    ///     Test 3: Multi-target with netstandard2.0;net10.0 produces portable form.
    ///     Setup: TargetFrameworks=netstandard2.0;net10.0 (multi-target with semicolon)
    ///     Setup: ancplua_nullguard_style=auto
    ///     Expect: Fix produces PORTABLE form (stable across all targets)
    ///     Key: Even though net10 would support BCL, we use portable for stability.
    /// </summary>
    [Fact]
    public Task ShouldProducePortableFormForMultiTargetProject()
    {
        var source = """
                     public class TestClass
                     {
                         public void TestMethod(string? x)
                         {
                             [|if|] (x is null) throw new ArgumentNullException(nameof(x));
                         }
                     }
                     """;

        var expected = """
                       public class TestClass
                       {
                           public void TestMethod(string? x)
                           {
                               x = x ?? throw new ArgumentNullException(nameof(x));
                           }
                       }
                       """;

        return VerifyAsync(source, expected, editorConfig: new Dictionary<string, string>
        {
            { "build_property.TargetFrameworks", "netstandard2.0;net10.0" },
            { "ancplua_nullguard_style", "auto" }
        }, includeThrowIfNullReference: true);
    }

    /// <summary>
    ///     Test 3b: Multi-target with three frameworks produces portable form.
    /// </summary>
    [Fact]
    public Task ShouldProducePortableFormForMultipleTargets()
    {
        var source = """
                     public class TestClass
                     {
                         public void TestMethod(object? obj)
                         {
                             [|if|] (obj is null) throw new ArgumentNullException(nameof(obj));
                         }
                     }
                     """;

        var expected = """
                       public class TestClass
                       {
                           public void TestMethod(object? obj)
                           {
                               obj = obj ?? throw new ArgumentNullException(nameof(obj));
                           }
                       }
                       """;

        return VerifyAsync(source, expected, editorConfig: new Dictionary<string, string>
        {
            { "build_property.TargetFrameworks", "netstandard2.0;net8.0;net10.0" },
            { "ancplua_nullguard_style", "auto" }
        }, includeThrowIfNullReference: true);
    }

    /// <summary>
    ///     Test 3c: FixAll behavior - multiple diagnostics produce consistent portable form.
    /// </summary>
    [Fact]
    public Task ShouldProduceConsistentPortableFormForMultipleDiagnostics()
    {
        var source = """
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

        var expected = """
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

        return VerifyAsync(source, expected, editorConfig: new Dictionary<string, string>
        {
            { "build_property.TargetFrameworks", "netstandard2.0;net10.0" },
            { "ancplua_nullguard_style", "auto" }
        }, includeThrowIfNullReference: true);
    }
}

/// <summary>
///     Edge case and rejection tests for AL0015.
/// </summary>
public sealed class AL0015EdgeCasesTests : ALCodeFixTestWithEditorConfig<AL0015NormalizeNullGuardStyleAnalyzer, AL0015NormalizeNullGuardStyleCodeFixProvider>
{
    /// <summary>
    ///     Test 4: BCL mode forced but ThrowIfNull unavailable produces no code fix.
    ///     Setup: ancplua_nullguard_style=bcl (explicit BCL mode)
    ///     Setup: No ThrowIfNull in compilation (netstandard2.0)
    ///     Expect: NO code fix offered
    /// </summary>
    [Fact]
    public Task ShouldNotProvideBclFormWhenThrowIfNullUnavailable()
    {
        var source = """
                     public class TestClass
                     {
                         public void TestMethod(string? x)
                         {
                             if (x is null) throw new ArgumentNullException(nameof(x));
                         }
                     }
                     """;

        return VerifyAsync(source, source, editorConfig: new Dictionary<string, string>
        {
            { "build_property.TargetFramework", "netstandard2.0" },
            { "ancplua_nullguard_style", "bcl" }
        }, includeThrowIfNullReference: false);
    }
}
