using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1500: exhaustive matching for closed (abstract base, sealed leaves) hierarchies.
/// </summary>
public sealed partial class Al1500ClosedTypeHierarchySwitchTests
    : AnalyzerTest<Al1500ClosedTypeHierarchySwitchAnalyzer> {
    private const string ShapeHierarchy = """
                                          public abstract class Shape { }
                                          public sealed class Circle : Shape { }
                                          public sealed class Square : Shape { }
                                          """;

    [Fact]
    public Task ShouldReportSwitchExpressionMissingCaseBehindDiscard() =>
        VerifyAsync($$"""
                      {{ShapeHierarchy}}

                      public static class C {
                          public static int M(Shape shape) => shape [|switch|] {
                              Circle => 1,
                              _ => 0
                          };
                      }
                      """);

    [Fact]
    public Task ShouldReportSwitchStatementMissingCaseBehindDefault() =>
        VerifyAsync($$"""
                      {{ShapeHierarchy}}

                      public static class C {
                          public static int M(Shape shape) {
                              [|switch|] (shape) {
                                  case Circle:
                                      return 1;
                                  default:
                                      return 0;
                              }
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportWhenOnlyGuardedArmHandlesCase() =>
        VerifyAsync($$"""
                      {{ShapeHierarchy}}

                      public static class C {
                          public static int M(Shape shape, bool flag) => shape [|switch|] {
                              Circle => 1,
                              Square when flag => 2,
                              _ => 0
                          };
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenEveryCaseIsHandled() =>
        VerifyAsync($$"""
                      {{ShapeHierarchy}}

                      public static class C {
                          public static int M(Shape shape) => shape switch {
                              Circle => 1,
                              Square => 2,
                              _ => 0
                          };
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenOrPatternHandlesEveryCase() =>
        VerifyAsync($$"""
                      {{ShapeHierarchy}}

                      public static class C {
                          public static int M(Shape shape) {
                              switch (shape) {
                                  case Circle or Square:
                                      return 1;
                                  default:
                                      return 0;
                              }
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenHierarchyHasUnsealedLeaf() =>
        VerifyAsync("""
                    public abstract class Shape { }
                    public sealed class Circle : Shape { }
                    public class Square : Shape { }

                    public static class C {
                        public static int M(Shape shape) => shape switch {
                            Circle => 1,
                            _ => 0
                        };
                    }
                    """);

    [Fact]
    public Task ShouldNotReportWhenHierarchyHasSingleSealedLeaf() =>
        VerifyAsync("""
                    public abstract class Shape { }
                    public sealed class Circle : Shape { }

                    public static class C {
                        public static int M(Shape shape) => shape switch {
                            _ => 0
                        };
                    }
                    """);
}
