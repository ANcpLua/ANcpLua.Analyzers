using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1218: Use Guard.DefinedEnum instead of if (!Enum.IsDefined) throw patterns.
/// </summary>
public sealed partial class Al1218UseGuardDefinedEnumTests : AnalyzerTest<Al1218UseGuardDefinedEnumAnalyzer> {
    [Fact]
    public Task ShouldReportForNonGenericEnumIsDefined() => VerifyAsync("""
        using System;
        public enum MyEnum { A, B, C }
        public class C {
            void M(MyEnum value) {
                [|if (!Enum.IsDefined(typeof(MyEnum), value)) throw new ArgumentException("Invalid enum value.");|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportForGenericEnumIsDefined() => VerifyAsync("""
        using System;
        public enum MyEnum { A, B, C }
        public class C {
            void M(MyEnum value) {
                [|if (!Enum.IsDefined<MyEnum>(value)) throw new ArgumentException("Invalid enum value.");|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportForArgumentOutOfRangeException() => VerifyAsync("""
        using System;
        public enum MyEnum { A, B, C }
        public class C {
            void M(MyEnum value) {
                [|if (!Enum.IsDefined(typeof(MyEnum), value)) throw new ArgumentOutOfRangeException(nameof(value));|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportWithBlockStatement() => VerifyAsync("""
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
    public Task ShouldReportWithParenthesizedCondition() => VerifyAsync("""
        using System;
        public enum MyEnum { A, B, C }
        public class C {
            void M(MyEnum value) {
                [|if ((!Enum.IsDefined(typeof(MyEnum), value))) throw new ArgumentException("Invalid enum value.");|]
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForBlockStatementWithExtraStatements() => VerifyAsync("""
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
    public Task ShouldNotReportWhenElsePresent() => VerifyAsync("""
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
    public Task ShouldNotReportWithoutNegation() => VerifyAsync("""
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
    public Task ShouldNotReportForOtherExceptionTypes() => VerifyAsync("""
        using System;
        public enum MyEnum { A, B, C }
        public class C {
            void M(MyEnum value) {
                if (!Enum.IsDefined(typeof(MyEnum), value)) throw new InvalidOperationException("Invalid enum value.");
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForTernaryExpression() => VerifyAsync("""
        using System;
        public enum MyEnum { A, B, C }
        public class C {
            bool M(MyEnum value) => !Enum.IsDefined(typeof(MyEnum), value) ? true : false;
        }
        """);

    [Fact]
    public Task ShouldNotReportForOtherMethods() => VerifyAsync("""
        using System;
        public enum MyEnum { A, B, C }
        public class C {
            void M(string value) {
                if (!string.IsNullOrEmpty(value)) throw new ArgumentException("value");
            }
        }
        """);

    [Fact]
    public Task ShouldReportForFullyQualifiedEnumType() => VerifyAsync("""
        using System;
        public enum MyEnum { A, B, C }
        public class C {
            void M(MyEnum value) {
                [|if (!System.Enum.IsDefined(typeof(MyEnum), value)) throw new ArgumentException("Invalid enum value.");|]
            }
        }
        """);
}
