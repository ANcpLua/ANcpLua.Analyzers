using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0011: Avoid lock keyword on non-Lock types.
///     Warns on ALL lock(object) usage. In .NET 9+, lock(Lock) is preferred.
/// </summary>
/// <remarks>
///     Note: This analyzer warns on lock(object) regardless of target framework.
///     On .NET 9+, it only suppresses the warning when locking on System.Threading.Lock.
///     Tests use a polyfill to simulate the Lock type.
/// </remarks>
public sealed partial class Al0011AnalyzerTests : AnalyzerTest<Al0011LockKeywordAnalyzer> {
    [Theory]
    [InlineData("""
                namespace System.Threading {
                    public sealed class Lock {
                        public Scope EnterScope() => default;
                        public ref struct Scope : System.IDisposable {
                            public void Dispose() { }
                        }
                    }
                }

                public class C {
                    private readonly object _syncRoot = new();
                    void M() {
                        {|AL0011:lock|} (_syncRoot) { }
                    }
                }
                """)]
    [InlineData("""
                namespace System.Threading {
                    public sealed class Lock {
                        public Scope EnterScope() => default;
                        public ref struct Scope : System.IDisposable {
                            public void Dispose() { }
                        }
                    }
                }

                public class C {
                    void M() {
                        {|AL0011:lock|} (this) { }
                    }
                }
                """)]
    [InlineData("""
                namespace System.Threading {
                    public sealed class Lock {
                        public Scope EnterScope() => default;
                        public ref struct Scope : System.IDisposable {
                            public void Dispose() { }
                        }
                    }
                }

                public class C {
                    void M() {
                        object obj = new();
                        {|AL0011:lock|} (obj) { }
                    }
                }
                """)]
    [InlineData("""
                namespace System.Threading {
                    public sealed class Lock {
                        public Scope EnterScope() => default;
                        public ref struct Scope : System.IDisposable {
                            public void Dispose() { }
                        }
                    }
                }

                public class C {
                    private readonly string _s = "sync";
                    void M() {
                        {|AL0011:lock|} (_s) { }
                    }
                }
                """)]
    public Task ShouldReportLockOnNonLockType(string source) => VerifyAsync(source);

    [Theory]
    [InlineData("""
                namespace System.Threading {
                    public sealed class Lock {
                        public Scope EnterScope() => default;
                        public ref struct Scope : System.IDisposable {
                            public void Dispose() { }
                        }
                    }
                }

                public class C {
                    private readonly System.Threading.Lock _lock = new();
                    void M() {
                        lock (_lock) { }
                    }
                }
                """)]
    [InlineData("""
                namespace System.Threading {
                    public sealed class Lock {
                        public Scope EnterScope() => default;
                        public ref struct Scope : System.IDisposable {
                            public void Dispose() { }
                        }
                    }
                }

                public class C {
                    void M() {
                        var l = new System.Threading.Lock();
                        lock (l) { }
                    }
                }
                """)]
    public Task ShouldNotReportLockOnLockType(string source) => VerifyAsync(source);

    [Fact]
    public Task ShouldStillReportWhenLockTypeNotAvailable() => VerifyAsync(
        """
        public class C {
            private readonly object _syncRoot = new();
            void M() {
                {|AL0011:lock|} (_syncRoot) { }
            }
        }
        """,
        false);
}

/// <summary>
///     Code fix tests for AL0011: Changes field type to System.Threading.Lock.
/// </summary>
public sealed partial class
    Al0011CodeFixTests : CodeFixTest<Al0011LockKeywordAnalyzer, Al0011LockTypeCodeFixProvider> {
    private const string LockTypePolyfill = """
                                            namespace System.Threading {
                                                public sealed class Lock {
                                                    public Scope EnterScope() => default;
                                                    public ref struct Scope : System.IDisposable {
                                                        public void Dispose() { }
                                                    }
                                                }
                                            }
                                            """;

    [Fact]
    public Task ShouldChangeFieldTypeToLock() =>
        VerifyAsync(
            $$"""
              {{LockTypePolyfill}}

              public class C {
                  private readonly object _syncRoot = new();
                  void M() {
                      [|lock|] (_syncRoot) { }
                  }
              }
              """,
            $$"""
              {{LockTypePolyfill}}

              public class C {
                  private readonly System.Threading.Lock _syncRoot = new();
                  void M() {
                      lock (_syncRoot) { }
                  }
              }
              """);

    [Fact]
    public Task ShouldChangeFieldTypeWithExplicitObjectCreation() =>
        VerifyAsync(
            $$"""
              {{LockTypePolyfill}}

              public class C {
                  private readonly object _sync = new object();
                  void M() {
                      [|lock|] (_sync) { }
                  }
              }
              """,
            $$"""
              {{LockTypePolyfill}}

              public class C {
                  private readonly System.Threading.Lock _sync = new();
                  void M() {
                      lock (_sync) { }
                  }
              }
              """);

}
