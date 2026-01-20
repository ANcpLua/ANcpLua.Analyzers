using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0026: Avoid DateTime time accessors.
///     Warns on static DateTime time properties when TimeProvider is available.
/// </summary>
public sealed partial class Al0026AnalyzerTests : AnalyzerTest<Al0026AvoidDateTimeNowAnalyzer> {
    private const string TimeProviderPolyfill = """
                                                namespace System {
                                                    public abstract class TimeProvider {
                                                        public static TimeProvider System { get; } = null!;
                                                        public abstract DateTimeOffset GetUtcNow();
                                                    }
                                                }
                                                """;

    private const string DateTimeType = "DateTime";

    [Theory]
    [InlineData("Now")]
    [InlineData("UtcNow")]
    public Task ShouldReportDateTimeAccessor(string property) =>
        VerifyAsync($$"""
                      using System;

                      {{TimeProviderPolyfill}}

                      public class C {
                          void M() {
                              var time = [|{{DateTimeType}}.{{property}}|];
                          }
                      }
                      """);

    [Theory]
    [InlineData("Now")]
    [InlineData("UtcNow")]
    public Task ShouldReportInFieldInitializer(string property) =>
        VerifyAsync($$"""
                      using System;

                      {{TimeProviderPolyfill}}

                      public class C {
                          private {{DateTimeType}} _time = [|{{DateTimeType}}.{{property}}|];
                      }
                      """);

    [Theory]
    [InlineData("Now")]
    [InlineData("UtcNow")]
    public Task ShouldReportInPropertyInitializer(string property) =>
        VerifyAsync($$"""
                      using System;

                      {{TimeProviderPolyfill}}

                      public class C {
                          public {{DateTimeType}} Time { get; } = [|{{DateTimeType}}.{{property}}|];
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenTimeProviderNotAvailable() {
        // Test that analyzer does not report when TimeProvider type is missing
        var code = $$"""
            using System;
            public class C {
                void M() {
                    var time = {{DateTimeType}}.Now;
                }
            }
            """;
        return VerifyAsync(code, false);
    }

    [Fact]
    public Task ShouldNotReportOtherDateTimeProperties() =>
        VerifyAsync($$"""
                      using System;

                      {{TimeProviderPolyfill}}

                      public class C {
                          void M() {
                              var min = {{DateTimeType}}.MinValue;
                              var max = {{DateTimeType}}.MaxValue;
                              var today = {{DateTimeType}}.Today;
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportInstanceProperties() =>
        VerifyAsync($$"""
                      using System;

                      {{TimeProviderPolyfill}}

                      public class C {
                          void M() {
                              var dt = new {{DateTimeType}}(2024, 1, 1);
                              var year = dt.Year;
                              var month = dt.Month;
                          }
                      }
                      """);
}
