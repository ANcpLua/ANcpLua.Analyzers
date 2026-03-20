using AnalyzerTestBase = ANcpLua.Roslyn.Utilities.Testing.AnalyzerTest<ANcpLua.Analyzers.Analyzers.Al0117UnnecessaryLinqMaterializationAnalyzer>;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0117: Unnecessary LINQ materialization.
/// </summary>
public sealed partial class Al0117UnnecessaryLinqMaterializationTests : AnalyzerTestBase {
    [Fact]
    public Task ShouldReportWhereToList() =>
        VerifyAsync("""
                    using System.Collections.Generic;
                    using System.Linq;

                    public class C {
                        public void M() {
                            var list = new List<int> { 1, 2, 3 };
                            var x = list.Where(i => i > 0).[|ToList|]();
                        }
                    }
                    """);

    [Fact]
    public Task ShouldReportSelectToArray() =>
        VerifyAsync("""
                    using System.Collections.Generic;
                    using System.Linq;

                    public class C {
                        public void M() {
                            var list = new List<int> { 1, 2, 3 };
                            var x = list.Select(i => i.ToString()).[|ToArray|]();
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportDirectToListWithoutLinqOperator() =>
        VerifyAsync("""
                    using System.Collections.Generic;
                    using System.Linq;

                    public class C {
                        public void M() {
                            var list = new List<int> { 1, 2, 3 };
                            var x = list.ToList();
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportNonEnumerableSource() =>
        VerifyAsync("""
                    using System.Collections.Generic;

                    public static class MyExtensions {
                        public static List<T> ToList<T>(this IEnumerable<T> source) => new List<T>();
                        public static IEnumerable<T> Where<T>(this IEnumerable<T> source, System.Func<T, bool> predicate) => source;
                    }

                    public class C {
                        public void M() {
                            var list = new List<int> { 1, 2, 3 };
                            var x = list.Where(i => i > 0).ToList();
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportNonLinqTerminal() =>
        VerifyAsync("""
                    using System.Collections.Generic;
                    using System.Linq;

                    public class C {
                        public void M() {
                            var list = new List<int> { 1, 2, 3 };
                            var x = list.Where(i => i > 0).Count();
                        }
                    }
                    """);
}
