using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1405: Unnecessary [AotUnsafe] attribute.
/// </summary>
public sealed partial class Al1405UnnecessaryAotUnsafeTests : AnalyzerTest<Al1405UnnecessaryAotUnsafeAnalyzer> {
    private const string AttributeStubs = """
                                          namespace ANcpLua.AotTesting {
                                              [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method)]
                                              public class AotUnsafeAttribute : System.Attribute {
                                                  public AotUnsafeAttribute() { }
                                                  public AotUnsafeAttribute(string reason) { }
                                              }
                                          }

                                          namespace System.Diagnostics.CodeAnalysis {
                                              [System.AttributeUsage(System.AttributeTargets.Method)]
                                              public class RequiresDynamicCodeAttribute : System.Attribute {
                                                  public RequiresDynamicCodeAttribute(string message) { }
                                              }
                                          }
                                          """;

    [Fact]
    public Task ShouldReportWhenAotUnsafeMethodHasNoUnsafePatterns() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      public class C {
                          [{|AL1405:ANcpLua.AotTesting.AotUnsafe("Not needed")|}]
                          public string FormatName(string first, string last) {
                              return first + " " + last;
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenAotUnsafeCallsRequiresDynamicCode() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      public class C {
                          [ANcpLua.AotTesting.AotUnsafe]
                          public void UnsafeMethod() {
                              DynamicMethod();
                          }

                          [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("Uses runtime code gen")]
                          public void DynamicMethod() { }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenAotUnsafeCallsOtherAotUnsafe() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      public class C {
                          [ANcpLua.AotTesting.AotUnsafe]
                          public void Method1() {
                              Method2();
                          }

                          // Method2 actually uses reflection, so both are correctly marked
                          [ANcpLua.AotTesting.AotUnsafe]
                          public object? Method2() {
                              return GetType().GetProperty("Name")?.GetValue(this);
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenAotUnsafeUsesReflection() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      public class C {
                          [ANcpLua.AotTesting.AotUnsafe("Uses reflection")]
                          public object? GetProperty(object obj, string name) {
                              var type = obj.GetType();
                              var prop = type.GetProperty(name);
                              return prop?.GetValue(obj);
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportForMethodOnAotUnsafeClass() =>
        VerifyAsync($$"""
                      {{AttributeStubs}}

                      [ANcpLua.AotTesting.AotUnsafe]
                      public class UnsafeClass {
                          // Class-level attribute, methods not individually marked
                          public string SafeLookingMethod() {
                              return "hello";
                          }
                      }
                      """);
}
