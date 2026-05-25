using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1107: Consider using configuration for connection strings.
///     Reports hardcoded connection strings passed to database/cache client constructors.
/// </summary>
public sealed partial class Al1107ConsiderConnectionStringTests : AnalyzerTest<Al1107ConsiderConnectionStringAnalyzer> {
    private const string ConnectionTypesPolyfill = """
                                                   namespace Npgsql {
                                                       public class NpgsqlConnection {
                                                           public NpgsqlConnection(string connectionString) { }
                                                       }
                                                       public class NpgsqlDataSource {
                                                           public static NpgsqlDataSource Create(string connectionString) => null!;
                                                       }
                                                       public class NpgsqlDataSourceBuilder {
                                                           public NpgsqlDataSourceBuilder(string connectionString) { }
                                                       }
                                                   }
                                                   namespace Microsoft.Data.SqlClient {
                                                       public class SqlConnection {
                                                           public SqlConnection(string connectionString) { }
                                                       }
                                                   }
                                                   namespace MongoDB.Driver {
                                                       public class MongoClient {
                                                           public MongoClient(string connectionString) { }
                                                       }
                                                   }
                                                   namespace StackExchange.Redis {
                                                       public class ConnectionMultiplexer {
                                                           public static ConnectionMultiplexer Connect(string configuration) => null!;
                                                           public static System.Threading.Tasks.Task<ConnectionMultiplexer> ConnectAsync(string configuration) => null!;
                                                       }
                                                   }
                                                   namespace RabbitMQ.Client {
                                                       public class ConnectionFactory {
                                                           public ConnectionFactory() { }
                                                           public ConnectionFactory(string uri) { }
                                                           public string Uri { get; set; }
                                                       }
                                                   }
                                                   """;

    [Fact]
    public Task ShouldReportNpgsqlConnectionWithServerPrefix() =>
        VerifyAsync($$"""
                      {{ConnectionTypesPolyfill}}

                      public class C {
                          void M() {
                              var conn = new Npgsql.NpgsqlConnection([|"Server=localhost;Database=test"|]);
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportNpgsqlConnectionWithHostPrefix() =>
        VerifyAsync($$"""
                      {{ConnectionTypesPolyfill}}

                      public class C {
                          void M() {
                              var conn = new Npgsql.NpgsqlConnection([|"Host=localhost;Database=test;Username=user"|]);
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportSqlConnectionWithDataSource() =>
        VerifyAsync($$"""
                      {{ConnectionTypesPolyfill}}

                      public class C {
                          void M() {
                              var conn = new Microsoft.Data.SqlClient.SqlConnection([|"Data Source=localhost;Initial Catalog=test"|]);
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportMongoClientWithMongodbUri() =>
        VerifyAsync($$"""
                      {{ConnectionTypesPolyfill}}

                      public class C {
                          void M() {
                              var client = new MongoDB.Driver.MongoClient([|"mongodb://localhost:27017/mydb"|]);
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportMongoClientWithMongodbSrvUri() =>
        VerifyAsync($$"""
                      {{ConnectionTypesPolyfill}}

                      public class C {
                          void M() {
                              var client = new MongoDB.Driver.MongoClient([|"mongodb+srv://user:pass@cluster.mongodb.net"|]);
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportRedisConnectWithRedisUri() =>
        VerifyAsync($$"""
                      {{ConnectionTypesPolyfill}}

                      public class C {
                          void M() {
                              var redis = StackExchange.Redis.ConnectionMultiplexer.Connect([|"redis://localhost:6379"|]);
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportRedisConnectAsyncWithRedisUri() =>
        VerifyAsync($$"""
                      {{ConnectionTypesPolyfill}}

                      public class C {
                          async System.Threading.Tasks.Task M() {
                              var redis = await StackExchange.Redis.ConnectionMultiplexer.ConnectAsync([|"rediss://localhost:6379"|]);
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportNpgsqlDataSourceBuilder() =>
        VerifyAsync($$"""
                      {{ConnectionTypesPolyfill}}

                      public class C {
                          void M() {
                              var builder = new Npgsql.NpgsqlDataSourceBuilder([|"Host=localhost;Database=test"|]);
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportNpgsqlDataSourceCreate() =>
        VerifyAsync($$"""
                      {{ConnectionTypesPolyfill}}

                      public class C {
                          void M() {
                              var ds = Npgsql.NpgsqlDataSource.Create([|"Host=localhost;Database=test"|]);
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportInterpolatedConnectionString() =>
        VerifyAsync($$"""
                      {{ConnectionTypesPolyfill}}

                      public class C {
                          void M() {
                              var host = "localhost";
                              var conn = new Npgsql.NpgsqlConnection([|$"Host={host};Database=test"|]);
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportAmqpUri() =>
        VerifyAsync($$"""
                      {{ConnectionTypesPolyfill}}

                      public class C {
                          void M() {
                              var factory = new RabbitMQ.Client.ConnectionFactory([|"amqp://guest:guest@localhost:5672"|]);
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportVariableConnectionString() =>
        VerifyAsync($$"""
                      {{ConnectionTypesPolyfill}}

                      public class C {
                          void M(string connectionString) {
                              var conn = new Npgsql.NpgsqlConnection(connectionString);
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportConfigurationGetConnectionString() =>
        VerifyAsync($$"""
                      {{ConnectionTypesPolyfill}}

                      namespace Microsoft.Extensions.Configuration {
                          public interface IConfiguration {
                              string GetConnectionString(string name);
                          }
                      }

                      public class C {
                          void M(Microsoft.Extensions.Configuration.IConfiguration config) {
                              var conn = new Npgsql.NpgsqlConnection(config.GetConnectionString("Default"));
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportPropertyAccess() =>
        VerifyAsync($$"""
                      {{ConnectionTypesPolyfill}}

                      public class AppSettings {
                          public string ConnectionString { get; set; }
                      }

                      public class C {
                          void M(AppSettings settings) {
                              var conn = new Npgsql.NpgsqlConnection(settings.ConnectionString);
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportNonConnectionType() =>
        VerifyAsync("""
                    public class SomeOtherClass {
                        public SomeOtherClass(string value) { }
                    }

                    public class C {
                        void M() {
                            var obj = new SomeOtherClass("Server=localhost;Database=test");
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportEmptyString() =>
        VerifyAsync($$"""
                      {{ConnectionTypesPolyfill}}

                      public class C {
                          void M() {
                              var conn = new Npgsql.NpgsqlConnection("");
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportNonConnectionStringLiteral() =>
        VerifyAsync($$"""
                      {{ConnectionTypesPolyfill}}

                      public class C {
                          void M() {
                              var conn = new Npgsql.NpgsqlConnection("some-connection-name");
                          }
                      }
                      """);
}
