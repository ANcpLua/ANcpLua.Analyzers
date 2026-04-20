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

    [Fact]
    public Task ShouldNotReportWhenAssignedToObjectVariable() =>
        VerifyAsync("""
                    using System.Collections.Generic;
                    using System.Linq;

                    public class C {
                        public void M() {
                            var list = new List<int> { 1, 2, 3 };
                            object x = list.Where(i => i > 0).ToArray();
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportWhenBoxedInDictionaryInitializer() =>
        VerifyAsync("""
                    using System.Collections.Generic;
                    using System.Linq;

                    public class C {
                        public Dictionary<string, object?> M() {
                            var list = new List<int> { 1, 2, 3 };
                            return new Dictionary<string, object?> {
                                ["items"] = list.Select(i => i.ToString()).ToArray()
                            };
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportWhenPassedAsObjectArgument() =>
        VerifyAsync("""
                    using System.Collections.Generic;
                    using System.Linq;

                    public class C {
                        public void Consume(object value) { }
                        public void M() {
                            var list = new List<int> { 1, 2, 3 };
                            Consume(list.Where(i => i > 0).ToList());
                        }
                    }
                    """);

    [Fact]
    public Task ShouldStillReportWhenAssignedToStronglyTypedCollection() =>
        VerifyAsync("""
                    using System.Collections.Generic;
                    using System.Linq;

                    public class C {
                        public void M() {
                            var list = new List<int> { 1, 2, 3 };
                            IEnumerable<int> x = list.Where(i => i > 0).[|ToArray|]();
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportWhenLocalIsReadMultipleTimes() =>
        VerifyAsync("""
                    using System.Collections.Generic;
                    using System.Linq;

                    public class C {
                        public void M() {
                            var list = new List<int> { 1, 2, 3 };
                            var sorted = list.OrderBy(i => i).ToArray();
                            foreach (var a in sorted) { }
                            foreach (var b in sorted) { }
                        }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportWhenPassedToArrayParameter() =>
        VerifyAsync("""
                    using System.Collections.Generic;
                    using System.Linq;

                    public class C {
                        public void M() {
                            var list = new List<int> { 1, 2, 3 };
                            Consume(list.Where(i => i > 0).ToArray());
                        }
                        private static void Consume(int[] values) { }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportWhenPassedToListParameter() =>
        VerifyAsync("""
                    using System.Collections.Generic;
                    using System.Linq;

                    public class C {
                        public void M() {
                            var list = new List<int> { 1, 2, 3 };
                            Consume(list.Where(i => i > 0).ToList());
                        }
                        private static void Consume(List<int> values) { }
                    }
                    """);

    [Fact]
    public Task ShouldNotReportWhenReturnedAsArray() =>
        VerifyAsync("""
                    using System.Collections.Generic;
                    using System.Linq;

                    public class C {
                        public int[] M(List<int> list) =>
                            list.Where(i => i > 0).ToArray();
                    }
                    """);

    [Fact]
    public Task ShouldNotReportWhenReturnedAsReadOnlyList() =>
        VerifyAsync("""
                    using System.Collections.Generic;
                    using System.Linq;

                    public class C {
                        public IReadOnlyList<int> M(List<int> list) =>
                            list.Where(i => i > 0).ToList();
                    }
                    """);

    [Fact]
    public Task ShouldStillReportWhenLocalIsReadOnce() =>
        VerifyAsync("""
                    using System.Collections.Generic;
                    using System.Linq;

                    public class C {
                        public void M() {
                            var list = new List<int> { 1, 2, 3 };
                            var once = list.Where(i => i > 0).[|ToList|]();
                            foreach (var x in once) { }
                        }
                    }
                    """);

    [Fact]
    public Task ShouldStillReportWhenReturnedAsIEnumerable() =>
        VerifyAsync("""
                    using System.Collections.Generic;
                    using System.Linq;

                    public class C {
                        public IEnumerable<int> M(List<int> list) =>
                            list.Where(i => i > 0).[|ToArray|]();
                    }
                    """);
}
