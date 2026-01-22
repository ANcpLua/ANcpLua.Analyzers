using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0031: Use IsMethodNamed/TryGetConstantValue instead of verbose patterns.
/// </summary>
public sealed class Al0031UseOperationExtensionsTests : AnalyzerTest<Al0031UseOperationExtensionsAnalyzer> {
    private const string RoslynPolyfill = """
                                          namespace Microsoft.CodeAnalysis {
                                              public interface IMethodSymbol {
                                                  string Name { get; }
                                              }
                                          }
                                          namespace Microsoft.CodeAnalysis.Operations {
                                              public interface IOperation {
                                                  Optional<object?> ConstantValue { get; }
                                              }
                                              public interface IInvocationOperation : IOperation {
                                                  Microsoft.CodeAnalysis.IMethodSymbol TargetMethod { get; }
                                              }
                                          }
                                          public struct Optional<T> {
                                              public bool HasValue { get; }
                                              public T Value { get; }
                                          }
                                          """;

    [Fact]
    public Task ShouldReportTargetMethodNameEquals() =>
        VerifyAsync($$"""
                      {{RoslynPolyfill}}

                      public class C {
                          bool M(Microsoft.CodeAnalysis.Operations.IInvocationOperation invocation) {
                              return [|invocation.TargetMethod.Name == "ToString"|];
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportTargetMethodNameNotEquals() =>
        VerifyAsync($$"""
                      {{RoslynPolyfill}}

                      public class C {
                          bool M(Microsoft.CodeAnalysis.Operations.IInvocationOperation invocation) {
                              return [|invocation.TargetMethod.Name != "Dispose"|];
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportOtherNameComparison() =>
        VerifyAsync($$"""
                      {{RoslynPolyfill}}

                      public class C {
                          bool M(Microsoft.CodeAnalysis.IMethodSymbol method) {
                              return method.Name == "Test";
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportConstantValueHasValueCheck() =>
        VerifyAsync($$"""
                      {{RoslynPolyfill}}

                      public class C {
                          void M(Microsoft.CodeAnalysis.Operations.IOperation operation) {
                              if ([|operation.ConstantValue.HasValue && operation.ConstantValue.Value is int value|]) {
                                  System.Console.WriteLine(value);
                              }
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportHasValueAlone() =>
        VerifyAsync($$"""
                      {{RoslynPolyfill}}

                      public class C {
                          bool M(Microsoft.CodeAnalysis.Operations.IOperation operation) {
                              return operation.ConstantValue.HasValue;
                          }
                      }
                      """);
}
