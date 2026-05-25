using AnalyzerTestBase = ANcpLua.Roslyn.Utilities.Testing.AnalyzerTest<ANcpLua.Analyzers.Analyzers.Al1306SqlInterpolationInCommandTextAnalyzer>;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1306: Avoid SQL string interpolation in CommandText.
/// </summary>
public sealed partial class Al1306SqlInterpolationInCommandTextTests : AnalyzerTestBase {
    private const string DbCommandStub = """
                                         namespace System.Data.Common {
                                             public abstract class DbCommand {
                                                 public string CommandText { get; set; }
                                             }
                                         }

                                         namespace System.Data {
                                             public interface IDbCommand {
                                                 string CommandText { get; set; }
                                             }
                                         }
                                         """;

    [Fact]
    public Task ShouldReportInterpolatedStringInCommandText() =>
        VerifyAsync($$"""
                      {{DbCommandStub}}

                      public class C {
                          public void M(System.Data.Common.DbCommand cmd, string table) {
                              cmd.CommandText = [|$"SELECT * FROM {table}"|];
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportRawInterpolatedStringInCommandText() =>
        VerifyAsync($$"""
                      {{DbCommandStub}}

                      public class C {
                          public void M(System.Data.Common.DbCommand cmd, string value) {
                              cmd.CommandText = [|$"SELECT * FROM users WHERE name = '{value}'"|];
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportPlainStringInCommandText() =>
        VerifyAsync($$"""
                      {{DbCommandStub}}

                      public class C {
                          public void M(System.Data.Common.DbCommand cmd) {
                              cmd.CommandText = "SELECT * FROM users";
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportVariableAssignmentToCommandText() =>
        VerifyAsync($$"""
                      {{DbCommandStub}}

                      public class C {
                          public void M(System.Data.Common.DbCommand cmd, string sql) {
                              cmd.CommandText = sql;
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportInterpolatedStringOnNonCommandTextProperty() =>
        VerifyAsync("""
                    public class MyObj {
                        public string Name { get; set; }
                    }

                    public class C {
                        public void M(MyObj obj, string name) {
                            obj.Name = $"hello {name}";
                        }
                    }
                    """);

    [Fact]
    public Task ShouldReportInterpolatedStringOnIDbCommandLikeType() =>
        VerifyAsync($$"""
                      {{DbCommandStub}}

                      public class MyDbCommand : System.Data.IDbCommand {
                          public string CommandText { get; set; } = "";
                      }

                      public class C {
                          public void M(MyDbCommand cmd, string table) {
                              cmd.CommandText = [|$"SELECT * FROM {table}"|];
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportInterpolatedStringWithOnlyConstantHoles() =>
        VerifyAsync($$"""
                      {{DbCommandStub}}

                      public class C {
                          public void M(System.Data.Common.DbCommand cmd) {
                              cmd.CommandText = $"SELECT * FROM {1}";
                          }
                      }
                      """);
}
