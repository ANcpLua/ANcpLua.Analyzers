using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0122: [DuckDbTable] type must be partial.
/// </summary>
public sealed partial class Al0122DuckDbTableMustBePartialTests : AnalyzerTest<Al0122DuckDbTableMustBePartialAnalyzer> {
    private const string DuckDbStubs = """
                                       namespace Qyl.Collector.Storage {
                                           [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]
                                           public class DuckDbTableAttribute : System.Attribute {
                                               public DuckDbTableAttribute() { }
                                               public DuckDbTableAttribute(string tableName) { }
                                               public string TableName { get; set; }
                                           }
                                       }
                                       """;

    [Fact]
    public Task ShouldReportNonPartialClass() =>
        VerifyAsync($$"""
                      {{DuckDbStubs}}

                      [Qyl.Collector.Storage.DuckDbTable("spans")]
                      public class [|SpanRow|] {
                          public string TraceId { get; set; }
                      }
                      """);

    [Fact]
    public Task ShouldReportNonPartialStruct() =>
        VerifyAsync($$"""
                      {{DuckDbStubs}}

                      [Qyl.Collector.Storage.DuckDbTable("metrics")]
                      public struct [|MetricRow|] {
                          public double Value { get; set; }
                      }
                      """);

    [Fact]
    public Task ShouldReportNonPartialRecord() =>
        VerifyAsync($$"""
                      {{DuckDbStubs}}

                      [Qyl.Collector.Storage.DuckDbTable]
                      public record [|LogRow|](string Message);
                      """);

    [Fact]
    public Task ShouldNotReportPartialClass() =>
        VerifyAsync($$"""
                      {{DuckDbStubs}}

                      [Qyl.Collector.Storage.DuckDbTable("spans")]
                      public partial class SpanRow {
                          public string TraceId { get; set; }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportPartialStruct() =>
        VerifyAsync($$"""
                      {{DuckDbStubs}}

                      [Qyl.Collector.Storage.DuckDbTable("metrics")]
                      public partial struct MetricRow {
                          public double Value { get; set; }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportTypeWithoutAttribute() =>
        VerifyAsync($$"""
                      {{DuckDbStubs}}

                      public class PlainModel {
                          public string Name { get; set; }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenDuckDbNotReferenced() =>
        VerifyAsync("""
                    public class SpanRow {
                        public string TraceId { get; set; }
                    }
                    """);
}

/// <summary>
///     Code fix tests for AL0122: Adds partial modifier to [DuckDbTable] types.
/// </summary>
public sealed partial class Al0122CodeFixTests : CodeFixTest<Al0122DuckDbTableMustBePartialAnalyzer, Al0122DuckDbTableCodeFixProvider> {
    private const string DuckDbStubs = """
                                       namespace Qyl.Collector.Storage {
                                           [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]
                                           public class DuckDbTableAttribute : System.Attribute {
                                               public DuckDbTableAttribute() { }
                                               public DuckDbTableAttribute(string tableName) { }
                                           }
                                       }
                                       """;

    [Fact]
    public Task ShouldAddPartialToClass() =>
        VerifyAsync($$"""
                      {{DuckDbStubs}}

                      [Qyl.Collector.Storage.DuckDbTable("spans")]
                      public class [|SpanRow|] {
                          public string TraceId { get; set; }
                      }
                      """,
            $$"""
              {{DuckDbStubs}}

              [Qyl.Collector.Storage.DuckDbTable("spans")]
              public partial class SpanRow {
                  public string TraceId { get; set; }
              }
              """);

    [Fact]
    public Task ShouldAddPartialToStruct() =>
        VerifyAsync($$"""
                      {{DuckDbStubs}}

                      [Qyl.Collector.Storage.DuckDbTable]
                      public struct [|MetricRow|] {
                          public double Value { get; set; }
                      }
                      """,
            $$"""
              {{DuckDbStubs}}

              [Qyl.Collector.Storage.DuckDbTable]
              public partial struct MetricRow {
                  public double Value { get; set; }
              }
              """);
}
