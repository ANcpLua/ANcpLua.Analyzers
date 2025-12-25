using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;
using System;
using System.Collections.Generic;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0016: Combine declaration with subsequent null-check.
///     Covers detection of a pattern where a local variable is declared and immediately
///     checked for null, flagging them for combination into a single pattern match.
/// </summary>
public sealed class AL0016AnalyzerTests : ALAnalyzerTest<AL0016CombineDeclarationWithNullCheckAnalyzer>
{
    [Theory]
    [InlineData("""
                public class TestClass
                {
                    public void TestMethod()
                    {
                        [|var x = M();|]
                        if (x is null) return;
                    }

                    private object? M() => null;
                }
                """)]
    [InlineData("""
                public class TestClass
                {
                    public void TestMethod()
                    {
                        [|var result = GetValue();|]
                        if (result == null) return;
                    }

                    private string? GetValue() => null;
                }
                """)]
    [InlineData("""
                public class TestClass
                {
                    public void TestMethod()
                    {
                        [|var data = FetchData();|]
                        if (data is null) throw new System.InvalidOperationException();
                    }

                    private object? FetchData() => null;
                }
                """)]
    [InlineData("""
                public class TestClass
                {
                    public void TestMethod(System.Collections.Generic.IEnumerable<object?> items)
                    {
                        foreach (var item in items)
                        {
                            [|var processed = Process(item);|]
                            if (processed is null) continue;
                        }
                    }

                    private object? Process(object? item) => item;
                }
                """)]
    public Task ShouldReportDiagnostic(string source)
    {
        return VerifyAsync(source);
    }

    [Theory]
    [InlineData("""
                public class TestClass
                {
                    public void TestMethod()
                    {
                        var x = M(), y = N();
                        if (x is null) return;
                    }

                    private object? M() => null;
                    private object? N() => null;
                }
                """)]
    [InlineData("""
                public class TestClass
                {
                    public void TestMethod()
                    {
                        var x = M();
                        y = ProcessX(x);
                        if (x is null) return;
                    }

                    private object? M() => null;
                    private object? ProcessX(object? x) => x;
                }
                """)]
    [InlineData("""
                public class TestClass
                {
                    public void TestMethod()
                    {
                        var x = M();
                        if (x is null)
                            return;
                        else
                            System.Console.WriteLine(x);
                    }

                    private object? M() => null;
                }
                """)]
    [InlineData("""
                public class TestClass
                {
                    public void TestMethod()
                    {
                        var x = M();
                        var y = N();
                        if (y is null) return;
                    }

                    private object? M() => null;
                    private object? N() => null;
                }
                """)]
    [InlineData("""
                public class TestClass
                {
                    public void TestMethod()
                    {
                        var x = M();
                        if (x?.Value is null) return;
                    }

                    private object? M() => null;
                }
                """)]
    [InlineData("""
                public class TestClass
                {
                    public void TestMethod(bool condition)
                    {
                        var x = M();
                        if (condition && x is null) return;
                    }

                    private object? M() => null;
                }
                """)]
    [InlineData("""
                public class TestClass
                {
                    public void TestMethod()
                    {
                        var x = M();
                        if (x is not null) return;
                    }

                    private object? M() => null;
                }
                """)]
    public Task ShouldNotReportDiagnostic(string source)
    {
        return VerifyAsync(source);
    }
}

/// <summary>
///     Code fix tests for AL0016: Combine declaration with subsequent null-check.
///     Tests the transformation of patterns into combined pattern matches.
/// </summary>
public sealed class AL0016CodeFixTests : ALCodeFixTest<AL0016CombineDeclarationWithNullCheckAnalyzer, AL0016CombineDeclarationWithNullCheckCodeFixProvider>
{
    /// <summary>
    ///     Test 5: Basic return statement - the fundamental pattern.
    /// </summary>
    [Fact]
    public Task ShouldCombineDeclarationWithReturnStatement()
    {
        var source = """
                     public interface IData
                     {
                         object? GetValue();
                     }

                     public class TestClass
                     {
                         public string Format(IData? data)
                         {
                             [|var value = data?.GetValue();|]
                             if (value is null) return "";
                             return $"Value: {value}";
                         }
                     }
                     """;

        var expected = """
                       public interface IData
                       {
                           object? GetValue();
                       }

                       public class TestClass
                       {
                           public string Format(IData? data)
                           {
                               if (data?.GetValue() is not { } value) return "";
                               return $"Value: {value}";
                           }
                       }
                       """;

        return VerifyAsync(source, expected);
    }

    /// <summary>
    ///     Test 5b: Simple method call return.
    /// </summary>
    [Fact]
    public Task ShouldCombineSimpleMethodCallWithReturn()
    {
        var source = """
                     public class TestClass
                     {
                         public void TestMethod()
                         {
                             [|var|] x = M();
                             if (x is null) return;
                         }

                         private object? M() => null;
                     }
                     """;

        var expected = """
                       public class TestClass
                       {
                           public void TestMethod()
                           {
                               if (M() is not { } x) return;
                           }

                           private object? M() => null;
                       }
                       """;

        return VerifyAsync(source, expected);
    }

    /// <summary>
    ///     Test 5c: Equality check (== null) should also work.
    /// </summary>
    [Fact]
    public Task ShouldCombineDeclarationWithEqualityCheck()
    {
        var source = """
                     public class TestClass
                     {
                         public void TestMethod()
                         {
                             [|var|] result = GetValue();
                             if (result == null) return;
                         }

                         private string? GetValue() => null;
                     }
                     """;

        var expected = """
                       public class TestClass
                       {
                           public void TestMethod()
                           {
                               if (GetValue() is not { } result) return;
                           }

                           private string? GetValue() => null;
                       }
                       """;

        return VerifyAsync(source, expected);
    }
}

/// <summary>
///     Comprehensive tests for different early-exit forms (throw, continue, break).
/// </summary>
public sealed class AL0016EarlyExitTests : ALCodeFixTest<AL0016CombineDeclarationWithNullCheckAnalyzer, AL0016CombineDeclarationWithNullCheckCodeFixProvider>
{
    /// <summary>
    ///     Test 6: Throw statement as early-exit.
    /// </summary>
    [Fact]
    public Task ShouldCombineDeclarationWithThrowStatement()
    {
        var source = """
                     public class Item
                     {
                         public object? Context { get; }
                     }

                     public class TestClass
                     {
                         public void ProcessItem(Item? item)
                         {
                             [|var|] context = item?.Context;
                             if (context is null) throw new System.InvalidOperationException("No context");
                             Process(context);
                         }

                         private void Process(object context) { }
                     }
                     """;

        var expected = """
                       public class Item
                       {
                           public object? Context { get; }
                       }

                       public class TestClass
                       {
                           public void ProcessItem(Item? item)
                           {
                               if (item?.Context is not { } context) throw new System.InvalidOperationException("No context");
                               Process(context);
                           }

                           private void Process(object context) { }
                       }
                       """;

        return VerifyAsync(source, expected);
    }

    /// <summary>
    ///     Test 6b: Continue statement in loop.
    /// </summary>
    [Fact]
    public Task ShouldCombineDeclarationWithContinueStatement()
    {
        var source = """
                     public class Item
                     {
                     }

                     public class TestClass
                     {
                         public void ProcessItems(System.Collections.Generic.IEnumerable<Item?> items)
                         {
                             foreach (var item in items)
                             {
                                 [|var|] processed = Process(item);
                                 if (processed is null) continue;
                                 Add(processed);
                             }
                         }

                         private object? Process(Item? item) => item;
                         private void Add(object item) { }
                     }
                     """;

        var expected = """
                       public class Item
                       {
                       }

                       public class TestClass
                       {
                           public void ProcessItems(System.Collections.Generic.IEnumerable<Item?> items)
                           {
                               foreach (var item in items)
                               {
                                   if (Process(item) is not { } processed) continue;
                                   Add(processed);
                               }
                           }

                           private object? Process(Item? item) => item;
                           private void Add(object item) { }
                       }
                       """;

        return VerifyAsync(source, expected);
    }

    /// <summary>
    ///     Test 6c: Break statement in loop.
    /// </summary>
    [Fact]
    public Task ShouldCombineDeclarationWithBreakStatement()
    {
        var source = """
                     public class TestClass
                     {
                         public void ProcessArray(int[] array)
                         {
                             for (int i = 0; i < array.Length; i++)
                             {
                                 [|var|] value = GetValue(array[i]);
                                 if (value is null) break;
                                 ProcessValue(value);
                             }
                         }

                         private object? GetValue(int index) => null;
                         private void ProcessValue(object value) { }
                     }
                     """;

        var expected = """
                       public class TestClass
                       {
                           public void ProcessArray(int[] array)
                           {
                               for (int i = 0; i < array.Length; i++)
                               {
                                   if (GetValue(array[i]) is not { } value) break;
                                   ProcessValue(value);
                               }
                           }

                           private object? GetValue(int index) => null;
                           private void ProcessValue(object value) { }
                       }
                       """;

        return VerifyAsync(source, expected);
    }

    /// <summary>
    ///     Test 6d: Block containing single throw statement.
    /// </summary>
    [Fact]
    public Task ShouldCombineDeclarationWithBlockContainingThrow()
    {
        var source = """
                     public class TestClass
                     {
                         public void ProcessData(object? data)
                         {
                             [|var|] config = LoadConfig(data);
                             if (config is null)
                             {
                                 throw new System.IO.FileNotFoundException("config.json not found");
                             }
                             Apply(config);
                         }

                         private object? LoadConfig(object? data) => null;
                         private void Apply(object config) { }
                     }
                     """;

        var expected = """
                       public class TestClass
                       {
                           public void ProcessData(object? data)
                           {
                               if (LoadConfig(data) is not { } config)
                               {
                                   throw new System.IO.FileNotFoundException("config.json not found");
                               }
                               Apply(config);
                           }

                           private object? LoadConfig(object? data) => null;
                           private void Apply(object config) { }
                       }
                       """;

        return VerifyAsync(source, expected);
    }

    /// <summary>
    ///     Test 6e: Block containing single return statement.
    /// </summary>
    [Fact]
    public Task ShouldCombineDeclarationWithBlockContainingReturn()
    {
        var source = """
                     public class TestClass
                     {
                         public string FormatData(object? data)
                         {
                             [|var|] value = Extract(data);
                             if (value is null)
                             {
                                 return "N/A";
                             }
                             return value.ToString();
                         }

                         private object? Extract(object? data) => data;
                     }
                     """;

        var expected = """
                       public class TestClass
                       {
                           public string FormatData(object? data)
                           {
                               if (Extract(data) is not { } value)
                               {
                                   return "N/A";
                               }
                               return value.ToString();
                           }

                           private object? Extract(object? data) => data;
                       }
                       """;

        return VerifyAsync(source, expected);
    }
}

/// <summary>
///     Complex expression and multi-statement tests for AL0016.
/// </summary>
public sealed class AL0016ComplexExpressionTests : ALCodeFixTest<AL0016CombineDeclarationWithNullCheckAnalyzer, AL0016CombineDeclarationWithNullCheckCodeFixProvider>
{
    /// <summary>
    ///     Test: Complex initializer expression (null-conditional call).
    /// </summary>
    [Fact]
    public Task ShouldCombineComplexNullConditionalExpression()
    {
        var source = """
                     public class TestClass
                     {
                         public void ProcessData(IData? data)
                         {
                             [|var|] value = data?.GetValue?.Invoke();
                             if (value is null) return;
                             Use(value);
                         }

                         private void Use(object value) { }
                     }
                     """;

        var expected = """
                       public class TestClass
                       {
                           public void ProcessData(IData? data)
                           {
                               if (data?.GetValue?.Invoke() is not { } value) return;
                               Use(value);
                           }

                           private void Use(object value) { }
                       }
                       """;

        return VerifyAsync(source, expected);
    }

    /// <summary>
    ///     Test: Initializer with cast expression.
    /// </summary>
    [Fact]
    public Task ShouldCombineDeclarationWithCastExpression()
    {
        var source = """
                     public class TestClass
                     {
                         public void ProcessData(object? input)
                         {
                             [|var|] data = input as string;
                             if (data is null) return;
                             Use(data);
                         }

                         private void Use(string data) { }
                     }
                     """;

        var expected = """
                       public class TestClass
                       {
                           public void ProcessData(object? input)
                           {
                               if (input as string is not { } data) return;
                               Use(data);
                           }

                           private void Use(string data) { }
                       }
                       """;

        return VerifyAsync(source, expected);
    }
}

/// <summary>
///     FixAll (batch fixer) tests for AL0016.
/// </summary>
public sealed class AL0016FixAllTests : ALCodeFixTest<AL0016CombineDeclarationWithNullCheckAnalyzer, AL0016CombineDeclarationWithNullCheckCodeFixProvider>
{
    /// <summary>
    ///     Test: Multiple patterns in same method are all fixed.
    /// </summary>
    [Fact]
    public Task ShouldFixAllDeclarationsInSameMethod()
    {
        var source = """
                     public class TestClass
                     {
                         public void ProcessData(object? data1, object? data2, object? data3)
                         {
                             [|var|] value1 = Extract(data1);
                             if (value1 is null) return;

                             [|var|] value2 = Extract(data2);
                             if (value2 is null) return;

                             [|var|] value3 = Extract(data3);
                             if (value3 is null) return;

                             UseAll(value1, value2, value3);
                         }

                         private object? Extract(object? data) => data;
                         private void UseAll(object v1, object v2, object v3) { }
                     }
                     """;

        var expected = """
                       public class TestClass
                       {
                           public void ProcessData(object? data1, object? data2, object? data3)
                           {
                               if (Extract(data1) is not { } value1) return;

                               if (Extract(data2) is not { } value2) return;

                               if (Extract(data3) is not { } value3) return;

                               UseAll(value1, value2, value3);
                           }

                           private object? Extract(object? data) => data;
                           private void UseAll(object v1, object v2, object v3) { }
                       }
                       """;

        return VerifyAsync(source, expected);
    }

    /// <summary>
    ///     Test: Multiple patterns with different exit forms.
    /// </summary>
    [Fact]
    public Task ShouldFixAllWithDifferentExitForms()
    {
        var source = """
                     public class Item
                     {
                         public string? Name { get; }
                         public object? Value { get; }
                     }

                     public class TestClass
                     {
                         public string Process(System.Collections.Generic.IEnumerable<Item?> items)
                         {
                             foreach (var item in items)
                             {
                                 [|var|] name = item?.Name;
                                 if (name is null) continue;

                                 [|var|] value = item?.Value;
                                 if (value is null) break;

                                 Use(name, value);
                             }
                             return "Done";
                         }

                         private void Use(string name, object value) { }
                     }
                     """;

        var expected = """
                       public class Item
                       {
                           public string? Name { get; }
                           public object? Value { get; }
                       }

                       public class TestClass
                       {
                           public string Process(System.Collections.Generic.IEnumerable<Item?> items)
                           {
                               foreach (var item in items)
                               {
                                   if (item?.Name is not { } name) continue;

                                   if (item?.Value is not { } value) break;

                                   Use(name, value);
                               }
                               return "Done";
                           }

                           private void Use(string name, object value) { }
                       }
                       """;

        return VerifyAsync(source, expected);
    }
}
