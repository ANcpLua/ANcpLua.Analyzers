using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1203: Use IsMethodNamed/TryGetConstantValue instead of verbose patterns.
/// </summary>
public sealed partial class Al1203UseOperationExtensionsTests : AnalyzerTest<Al1203UseOperationExtensionsAnalyzer> {
    private const string RoslynPolyfill = """
                                          namespace Microsoft.CodeAnalysis {
                                              public interface IMethodSymbol {
                                                  string Name { get; }
                                                  INamedTypeSymbol? ContainingType { get; }
                                              }
                                              public interface INamedTypeSymbol {
                                                  string Name { get; }
                                                  string MetadataName { get; }
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
    public Task ShouldNotReportTargetMethodNameEqualsWithoutContainingTypeCheck() =>
        VerifyAsync($$"""
                      {{RoslynPolyfill}}

                      public class C {
                          bool M(Microsoft.CodeAnalysis.Operations.IInvocationOperation invocation) {
                              return invocation.TargetMethod.Name == "ToString";
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportTargetMethodNameEqualsWithContainingTypeCheck() =>
        VerifyAsync($$"""
                      {{RoslynPolyfill}}

                      public class C {
                          bool M(Microsoft.CodeAnalysis.Operations.IInvocationOperation invocation) {
                              if (invocation.TargetMethod.ContainingType.Name == "String" &&
                                  [|invocation.TargetMethod.Name == "ToString"|]) {
                                  return true;
                              }
                              return false;
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportTargetMethodNameNotEqualsWithContainingTypeCheck() =>
        VerifyAsync($$"""
                      {{RoslynPolyfill}}

                      public class C {
                          bool M(Microsoft.CodeAnalysis.Operations.IInvocationOperation invocation) {
                              if (invocation.TargetMethod.ContainingType.MetadataName == "String" &&
                                  [|invocation.TargetMethod.Name != "Dispose"|]) {
                                  return true;
                              }
                              return false;
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
    public Task ShouldNotReportConstantValueAccessFromDifferentOperations() =>
        VerifyAsync($$"""
                      {{RoslynPolyfill}}

                      public class C {
                          bool M(Microsoft.CodeAnalysis.Operations.IOperation left, Microsoft.CodeAnalysis.Operations.IOperation right) {
                              return left.ConstantValue.HasValue && right.ConstantValue.Value is int;
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
