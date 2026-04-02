using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0120: Use IIncrementalGenerator instead of ISourceGenerator.
/// </summary>
public sealed partial class Al0120UseIncrementalGeneratorTests : AnalyzerTest<Al0120UseIncrementalGeneratorAnalyzer> {
    private const string GeneratorStubs = """
                                          namespace Microsoft.CodeAnalysis {
                                              public interface ISourceGenerator {
                                                  void Initialize(object context);
                                                  void Execute(object context);
                                              }
                                              public interface IIncrementalGenerator {
                                                  void Initialize(object context);
                                              }
                                              public class GeneratorAttribute : System.Attribute { }
                                          }
                                          """;

    [Fact]
    public Task ShouldReportClassImplementingISourceGenerator() =>
        VerifyAsync($$"""
                      {{GeneratorStubs}}

                      [Microsoft.CodeAnalysis.Generator]
                      public class [|MyGenerator|] : Microsoft.CodeAnalysis.ISourceGenerator {
                          public void Initialize(object context) { }
                          public void Execute(object context) { }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportClassImplementingIIncrementalGenerator() =>
        VerifyAsync($$"""
                      {{GeneratorStubs}}

                      [Microsoft.CodeAnalysis.Generator]
                      public class MyGenerator : Microsoft.CodeAnalysis.IIncrementalGenerator {
                          public void Initialize(object context) { }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportAbstractClass() =>
        VerifyAsync($$"""
                      {{GeneratorStubs}}

                      public abstract class BaseGenerator : Microsoft.CodeAnalysis.ISourceGenerator {
                          public abstract void Initialize(object context);
                          public abstract void Execute(object context);
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenRoslynNotReferenced() =>
        VerifyAsync("""
                    public class MyClass { }
                    """);
}
