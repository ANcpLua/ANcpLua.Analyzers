using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0050: Use Guard.NotEmpty instead of if (guid == Guid.Empty) throw.
/// </summary>
public sealed partial class Al0050UseGuardNotEmptyGuidTests : AnalyzerTest<Al0050UseGuardNotEmptyGuidAnalyzer> {
    [Fact]
    public Task ShouldReportForGuidEqualsEmpty() => VerifyAsync("""
        using System;
        public class C {
            void M(Guid id) {
                [|if (id == Guid.Empty) throw new ArgumentException("ID cannot be empty.", nameof(id));|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportForGuidEmptyEquals() => VerifyAsync("""
        using System;
        public class C {
            void M(Guid id) {
                [|if (Guid.Empty == id) throw new ArgumentException("ID cannot be empty.", nameof(id));|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportForBlockWithThrow() => VerifyAsync("""
        using System;
        public class C {
            void M(Guid id) {
                [|if (id == Guid.Empty) {
                    throw new ArgumentException("ID cannot be empty.", nameof(id));
                }|]
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForGuidEqualsDefault() => VerifyAsync("""
        using System;
        public class C {
            void M(Guid id) {
                if (id == default) throw new ArgumentException("ID cannot be empty.", nameof(id));
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForOtherTypes() => VerifyAsync("""
        using System;
        public class C {
            void M(int id) {
                if (id == 0) throw new ArgumentException("ID cannot be zero.", nameof(id));
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForOtherExceptionTypes() => VerifyAsync("""
        using System;
        public class C {
            void M(Guid id) {
                if (id == Guid.Empty) throw new InvalidOperationException("ID cannot be empty.");
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForNotEqualsPattern() => VerifyAsync("""
        using System;
        public class C {
            void M(Guid id) {
                if (id != Guid.Empty) throw new ArgumentException("ID must be empty.", nameof(id));
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForIfWithElse() => VerifyAsync("""
        using System;
        public class C {
            void M(Guid id) {
                if (id == Guid.Empty)
                    throw new ArgumentException("ID cannot be empty.", nameof(id));
                else
                    Console.WriteLine("Valid");
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForBlockWithMultipleStatements() => VerifyAsync("""
        using System;
        public class C {
            void M(Guid id) {
                if (id == Guid.Empty) {
                    Console.WriteLine("Empty!");
                    throw new ArgumentException("ID cannot be empty.", nameof(id));
                }
            }
        }
        """);

    [Fact]
    public Task ShouldReportForArgumentNullException() => VerifyAsync("""
        using System;
        public class C {
            void M(Guid id) {
                [|if (id == Guid.Empty) throw new ArgumentNullException(nameof(id));|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportForMemberAccess() => VerifyAsync("""
        using System;
        public class C {
            private Guid _id;
            void M() {
                [|if (_id == Guid.Empty) throw new ArgumentException("ID cannot be empty.");|]
            }
        }
        """);
}
