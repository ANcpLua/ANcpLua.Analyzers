using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1206: Use WhereNotNull() instead of Where with null check.
/// </summary>
public sealed partial class Al1206UseWhereNotNullTests : AnalyzerTest<Al1206UseWhereNotNullAnalyzer> {
    [Fact]
    public Task ShouldReportWhereWithNotEqualsNull() =>
        VerifyAsync("""
                    using System.Collections.Generic;
                    using System.Linq;

                    public class C {
                        IEnumerable<string> M(IEnumerable<string?> items) {
                            return [|items.Where(x => x != null)|]!;
                        }
                    }
                    """);

    [Fact]
    public Task ShouldReportWhereWithIsNotNullPattern() =>
        VerifyAsync("""
                    using System.Collections.Generic;
                    using System.Linq;

                    public class C {
                        IEnumerable<object> M(IEnumerable<object?> items) {
                            return [|items.Where(x => x is not null)|]!;
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportWhereWithOtherCondition() =>
        VerifyAsync("""
                    using System.Collections.Generic;
                    using System.Linq;

                    public class C {
                        IEnumerable<string> M(IEnumerable<string> items) {
                            return items.Where(x => x.Length > 0);
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportWhereWithNullEquality() =>
        VerifyAsync("""
                    using System.Collections.Generic;
                    using System.Linq;

                    public class C {
                        IEnumerable<string?> M(IEnumerable<string?> items) {
                            // Keep only nulls - not a "where not null" pattern
                            return items.Where(x => x == null);
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportWhereWithMultipleConditions() =>
        VerifyAsync("""
                    using System.Collections.Generic;
                    using System.Linq;

                    public class C {
                        IEnumerable<string> M(IEnumerable<string?> items) {
                            // More complex than just null check
                            return items.Where(x => x != null && x.Length > 0)!;
                        }
                    }
                    """);
}
