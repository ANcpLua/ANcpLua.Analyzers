using AnalyzerTestBase = ANcpLua.Roslyn.Utilities.Testing.AnalyzerTest<ANcpLua.Analyzers.Analyzers.Al0118ReadModifyWriteWithoutTransactionAnalyzer>;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0118: Read-modify-write without transaction.
/// </summary>
public sealed partial class Al0118ReadModifyWriteWithoutTransactionTests : AnalyzerTestBase {
    private const string DbStubs = """
                                   namespace System.Data {
                                       public interface IDbConnection {
                                           IDbTransaction BeginTransaction();
                                           IDbCommand CreateCommand();
                                       }
                                       public interface IDbTransaction { }
                                       public interface IDbCommand {
                                           string CommandText { get; set; }
                                           object ExecuteScalar();
                                           int ExecuteNonQuery();
                                       }
                                   }
                                   namespace System.Data.Common {
                                       public abstract class DbConnection : System.Data.IDbConnection {
                                           public abstract System.Data.IDbTransaction BeginTransaction();
                                           public abstract System.Data.IDbCommand CreateCommand();
                                           public System.Threading.Tasks.Task<System.Data.IDbTransaction> BeginTransactionAsync() => null!;
                                       }
                                       public abstract class DbCommand : System.Data.IDbCommand {
                                           public abstract string CommandText { get; set; }
                                           public abstract object ExecuteScalar();
                                           public abstract int ExecuteNonQuery();
                                           public System.Threading.Tasks.Task<object?> ExecuteScalarAsync(System.Threading.CancellationToken ct = default) => null!;
                                           public System.Threading.Tasks.Task<int> ExecuteNonQueryAsync(System.Threading.CancellationToken ct = default) => null!;
                                           public System.Threading.Tasks.Task<System.Data.Common.DbDataReader> ExecuteReaderAsync(System.Threading.CancellationToken ct = default) => null!;
                                       }
                                       public abstract class DbDataReader { }
                                   }
                                   """;

    [Fact]
    public Task ShouldReportReadThenWriteWithoutTransaction() =>
        VerifyAsync($$"""
                      using System.Threading;
                      using System.Threading.Tasks;
                      using System.Data.Common;

                      {{DbStubs}}

                      public class Repository {
                          public async Task [|UpdateIfExists|](DbConnection conn) {
                              var cmd = conn.CreateCommand();
                              await ((DbCommand)cmd).ExecuteScalarAsync();
                              await ((DbCommand)cmd).ExecuteNonQueryAsync();
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenTransactionPresent() =>
        VerifyAsync($$"""
                      using System.Threading;
                      using System.Threading.Tasks;
                      using System.Data.Common;

                      {{DbStubs}}

                      public class Repository {
                          public async Task UpdateIfExists(DbConnection conn) {
                              await conn.BeginTransactionAsync();
                              var cmd = conn.CreateCommand();
                              await ((DbCommand)cmd).ExecuteScalarAsync();
                              await ((DbCommand)cmd).ExecuteNonQueryAsync();
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportReadOnly() =>
        VerifyAsync($$"""
                      using System.Threading;
                      using System.Threading.Tasks;
                      using System.Data.Common;

                      {{DbStubs}}

                      public class Repository {
                          public async Task<object?> ReadData(DbConnection conn) {
                              var cmd = conn.CreateCommand();
                              return await ((DbCommand)cmd).ExecuteScalarAsync();
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWriteOnly() =>
        VerifyAsync($$"""
                      using System.Threading;
                      using System.Threading.Tasks;
                      using System.Data.Common;

                      {{DbStubs}}

                      public class Repository {
                          public async Task WriteData(DbConnection conn) {
                              var cmd = conn.CreateCommand();
                              await ((DbCommand)cmd).ExecuteNonQueryAsync();
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportSyncReadAndWrite() =>
        VerifyAsync($$"""
                      using System.Data;

                      {{DbStubs}}

                      public class Repository {
                          public void [|SyncUpdate|](IDbConnection conn) {
                              var cmd = conn.CreateCommand();
                              cmd.ExecuteScalar();
                              cmd.ExecuteNonQuery();
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportSyncWithTransaction() =>
        VerifyAsync($$"""
                      using System.Data;

                      {{DbStubs}}

                      public class Repository {
                          public void SyncUpdate(IDbConnection conn) {
                              conn.BeginTransaction();
                              var cmd = conn.CreateCommand();
                              cmd.ExecuteScalar();
                              cmd.ExecuteNonQuery();
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportWithReaderAndWrite() =>
        VerifyAsync($$"""
                      using System.Threading;
                      using System.Threading.Tasks;
                      using System.Data.Common;

                      {{DbStubs}}

                      public class Repository {
                          public async Task [|ReadAndUpdate|](DbConnection conn) {
                              var cmd = conn.CreateCommand();
                              await ((DbCommand)cmd).ExecuteReaderAsync();
                              await ((DbCommand)cmd).ExecuteNonQueryAsync();
                          }
                      }
                      """);
}
