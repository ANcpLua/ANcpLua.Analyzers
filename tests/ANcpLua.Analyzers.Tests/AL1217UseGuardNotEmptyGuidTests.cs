using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1217: Use Guard.NotEmpty instead of if (guid == Guid.Empty) throw.
/// </summary>
public sealed partial class Al1217UseGuardNotEmptyGuidTests : AnalyzerTest<Al1217UseGuardNotEmptyGuidAnalyzer> {
    // AL1217 only fires when ANcpLua.Roslyn.Utilities.Guard is present and accessible.
    // Each case appends this stub; ShouldNotReportWhenGuardNotReferenced omits it.
    private const string Stub = """
                                namespace ANcpLua.Roslyn.Utilities { internal static class Guard { } }
                                """;

    private static Task Verify(string body) => VerifyAsync($$"""
                                                            {{body}}
                                                            {{Stub}}
                                                            """);

    [Fact]
    public Task ShouldReportForGuidEqualsEmpty() => Verify("""
        using System;
        public class C {
            void M(Guid id) {
                [|if (id == Guid.Empty) throw new ArgumentException("ID cannot be empty.", nameof(id));|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportForGuidEmptyEquals() => Verify("""
        using System;
        public class C {
            void M(Guid id) {
                [|if (Guid.Empty == id) throw new ArgumentException("ID cannot be empty.", nameof(id));|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportForBlockWithThrow() => Verify("""
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
    public Task ShouldNotReportForGuidEqualsDefault() => Verify("""
        using System;
        public class C {
            void M(Guid id) {
                if (id == default) throw new ArgumentException("ID cannot be empty.", nameof(id));
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForOtherTypes() => Verify("""
        using System;
        public class C {
            void M(int id) {
                if (id == 0) throw new ArgumentException("ID cannot be zero.", nameof(id));
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForOtherExceptionTypes() => Verify("""
        using System;
        public class C {
            void M(Guid id) {
                if (id == Guid.Empty) throw new InvalidOperationException("ID cannot be empty.");
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForNotEqualsPattern() => Verify("""
        using System;
        public class C {
            void M(Guid id) {
                if (id != Guid.Empty) throw new ArgumentException("ID must be empty.", nameof(id));
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportForIfWithElse() => Verify("""
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
    public Task ShouldNotReportForBlockWithMultipleStatements() => Verify("""
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
    public Task ShouldReportForArgumentNullException() => Verify("""
        using System;
        public class C {
            void M(Guid id) {
                [|if (id == Guid.Empty) throw new ArgumentNullException(nameof(id));|]
            }
        }
        """);

    [Fact]
    public Task ShouldReportForMemberAccess() => Verify("""
        using System;
        public class C {
            private Guid _id;
            void M() {
                [|if (_id == Guid.Empty) throw new ArgumentException("ID cannot be empty.");|]
            }
        }
        """);

    // Gate regression: no Guard type in scope → no diagnostic.
    [Fact]
    public Task ShouldNotReportWhenGuardNotReferenced() =>
        VerifyAsync("""
                    using System;
                    public class C {
                        void M(Guid id) {
                            if (id == Guid.Empty) throw new ArgumentException("ID cannot be empty.", nameof(id));
                        }
                    }
                    """);
}

public sealed partial class Al1217UseGuardNotEmptyGuidCodeFixTests
    : CodeFixTest<Al1217UseGuardNotEmptyGuidAnalyzer, Al1217UseGuardNotEmptyGuidCodeFixProvider> {
    // Polyfill in the real namespace so the analyzer gate opens and Guard.NotEmpty() resolves.
    private const string GuardPolyfill = """
        using ANcpLua.Roslyn.Utilities;
        namespace ANcpLua.Roslyn.Utilities {
            public static class Guard {
                public static void NotEmpty(System.Guid value) { }
            }
        }
        """;

    [Fact]
    public Task ShouldPreserveMemberAccessReceiver() =>
        VerifyAsync(
            $$"""
            using System;
            {{GuardPolyfill}}
            public class User {
                public Guid Id { get; set; }
            }
            public class C {
                void M(User user) {
                    [|if (user.Id == Guid.Empty) throw new ArgumentException("ID cannot be empty.", nameof(user.Id));|]
                }
            }
            """,
            $$"""
            using System;
            {{GuardPolyfill}}
            public class User {
                public Guid Id { get; set; }
            }
            public class C {
                void M(User user) {
                    Guard.NotEmpty(user.Id);
                }
            }
            """);
}
