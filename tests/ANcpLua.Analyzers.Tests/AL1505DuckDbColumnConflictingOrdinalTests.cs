using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1505: Conflicting [DuckDbColumn] ordinal values.
/// </summary>
public sealed partial class Al1505DuckDbColumnConflictingOrdinalTests : AnalyzerTest<Al1505DuckDbColumnConflictingOrdinalAnalyzer> {
    private const string DuckDbStubs = """
                                       namespace Qyl.Collector.Storage {
                                           [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]
                                           public class DuckDbTableAttribute : System.Attribute {
                                               public DuckDbTableAttribute() { }
                                               public DuckDbTableAttribute(string tableName) { }
                                           }

                                           [System.AttributeUsage(System.AttributeTargets.Property)]
                                           public class DuckDbColumnAttribute : System.Attribute {
                                               public string ColumnName { get; set; }
                                               public int Ordinal { get; set; }
                                               public bool ExcludeFromInsert { get; set; }
                                           }
                                       }
                                       """;

    [Fact]
    public Task ShouldReportDuplicateOrdinals() =>
        VerifyAsync($$"""
                      {{DuckDbStubs}}

                      [Qyl.Collector.Storage.DuckDbTable("spans")]
                      public partial class SpanRow {
                          [Qyl.Collector.Storage.DuckDbColumn(Ordinal = 1)]
                          public string TraceId { get; set; }

                          [Qyl.Collector.Storage.DuckDbColumn(Ordinal = 1)]
                          public string [|SpanId|] { get; set; }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportUniqueOrdinals() =>
        VerifyAsync($$"""
                      {{DuckDbStubs}}

                      [Qyl.Collector.Storage.DuckDbTable("spans")]
                      public partial class SpanRow {
                          [Qyl.Collector.Storage.DuckDbColumn(Ordinal = 1)]
                          public string TraceId { get; set; }

                          [Qyl.Collector.Storage.DuckDbColumn(Ordinal = 2)]
                          public string SpanId { get; set; }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportColumnsWithoutExplicitOrdinal() =>
        VerifyAsync($$"""
                      {{DuckDbStubs}}

                      [Qyl.Collector.Storage.DuckDbTable("spans")]
                      public partial class SpanRow {
                          [Qyl.Collector.Storage.DuckDbColumn(ColumnName = "trace_id")]
                          public string TraceId { get; set; }

                          [Qyl.Collector.Storage.DuckDbColumn(ColumnName = "span_id")]
                          public string SpanId { get; set; }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenDuckDbNotReferenced() =>
        VerifyAsync("""
                    public class SpanRow {
                        public string TraceId { get; set; }
                        public string SpanId { get; set; }
                    }
                    """);
}
