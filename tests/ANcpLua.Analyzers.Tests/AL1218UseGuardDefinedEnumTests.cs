using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1218: Use Guard.DefinedEnum instead of if (!Enum.IsDefined) throw patterns.
/// </summary>
public sealed partial class Al1218UseGuardDefinedEnumTests : AnalyzerTest<Al1218UseGuardDefinedEnumAnalyzer> {
    // AL1218 only fires when ANcpLua.Roslyn.Utilities.Guard is present and accessible.
    // Each case appends this stub; ShouldNotReportWhenGuardNotReferenced omits it.
    private const string Stub = """
                                namespace ANcpLua.Roslyn.Utilities { internal static class Guard { } }
                                """;

    private static Task Verify(string body) => VerifyAsync($$"""
                                                            {{body}}
                                                            {{Stub}}
                                                            """);

    [Fact]
    public Task ShouldReportForNonGenericEnumIsDefined() => Verify("""
        using System;
        public enum MyEnum { A, B, C }
        public class C {
            void M(MyEnum value) {
                [|if (!Enum.IsDefined(typeof(MyEnum), value)) throw new ArgumentException("Invalid enum value.");|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportForGenericEnumIsDefined() => Verify("""
        using System;
        public enum MyEnum { A, B, C }
        public class C {
            void M(MyEnum value) {
                [|if (!Enum.IsDefined<MyEnum>(value)) throw new ArgumentException("Invalid enum value.");|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportForArgumentOutOfRangeException() => Verify("""
        using System;
        public enum MyEnum { A, B, C }
        public class C {
            void M(MyEnum value) {
                [|if (!Enum.IsDefined(typeof(MyEnum), value)) throw new ArgumentOutOfRangeException(nameof(value));|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportWithBlockStatement() => Verify("""
        using System;
        public enum MyEnum { A, B, C }
        public class C {
            void M(MyEnum value) {
                [|if (!Enum.IsDefined(typeof(MyEnum), value))
                {
                    throw new ArgumentException("Invalid enum value.");
                }|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportWithParenthesizedCondition() => Verify("""
        using System;
        public enum MyEnum { A, B, C }
        public class C {
            void M(MyEnum value) {
                [|if ((!Enum.IsDefined(typeof(MyEnum), value))) throw new ArgumentException("Invalid enum value.");|]
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForBlockStatementWithExtraStatements() => Verify("""
        using System;
        public enum MyEnum { A, B, C }
        public class C {
            void M(MyEnum value) {
                if (!Enum.IsDefined(typeof(MyEnum), value)) {
                    throw new ArgumentException("Invalid enum value.");
                    Console.WriteLine(value);
                }
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportWhenElsePresent() => Verify("""
        using System;
        public enum MyEnum { A, B, C }
        public class C {
            void M(MyEnum value) {
                if (!Enum.IsDefined(typeof(MyEnum), value)) {
                    throw new ArgumentException("Invalid enum value.");
                } else {
                    Console.WriteLine(value);
                }
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportWithoutNegation() => Verify("""
        using System;
        public enum MyEnum { A, B, C }
        public class C {
            void M(MyEnum value) {
                if (Enum.IsDefined(typeof(MyEnum), value))
                {
                    // Do something with valid enum
                }
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForOtherExceptionTypes() => Verify("""
        using System;
        public enum MyEnum { A, B, C }
        public class C {
            void M(MyEnum value) {
                if (!Enum.IsDefined(typeof(MyEnum), value)) throw new InvalidOperationException("Invalid enum value.");
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForTernaryExpression() => Verify("""
        using System;
        public enum MyEnum { A, B, C }
        public class C {
            bool M(MyEnum value) => !Enum.IsDefined(typeof(MyEnum), value) ? true : false;
        }
        """);

    [Fact]
    public Task ShouldNotReportForOtherMethods() => Verify("""
        using System;
        public enum MyEnum { A, B, C }
        public class C {
            void M(string value) {
                if (!string.IsNullOrEmpty(value)) throw new ArgumentException("value");
            }
        }
        """);

    [Fact]
    public Task ShouldReportForFullyQualifiedEnumType() => Verify("""
        using System;
        public enum MyEnum { A, B, C }
        public class C {
            void M(MyEnum value) {
                [|if (!System.Enum.IsDefined(typeof(MyEnum), value)) throw new ArgumentException("Invalid enum value.");|]
            }
        }
        """);

    // Gate regression: no Guard type in scope → no diagnostic.
    [Fact]
    public Task ShouldNotReportWhenGuardNotReferenced() =>
        VerifyAsync("""
                    using System;
                    public enum MyEnum { A, B, C }
                    public class C {
                        void M(MyEnum value) {
                            if (!Enum.IsDefined(typeof(MyEnum), value)) throw new ArgumentException("Invalid enum value.");
                        }
                    }
                    """);
}
