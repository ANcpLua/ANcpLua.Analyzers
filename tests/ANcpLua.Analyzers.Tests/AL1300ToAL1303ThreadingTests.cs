using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL1300: Avoid async void methods.
/// </summary>
public sealed partial class Al1300AnalyzerTests : AnalyzerTest<Al1300ToAl1303ThreadingAnalyzer> {
    [Theory]
    [InlineData("async void {|AL1300:M|}() { await Task.Delay(1); }")]
    [InlineData("private async void {|AL1300:DoWork|}() { await Task.Yield(); }")]
    [InlineData("public async void {|AL1300:Process|}() { await Task.CompletedTask; }")]
    public Task ShouldReportAsyncVoid(string method) => VerifyAsync(
        $$"""
        using System.Threading.Tasks;
        public class C {
            {{method}}
        }
        """);

    [Theory]
    [InlineData("async Task M() { await Task.Delay(1); }")]
    [InlineData("async Task<int> M() { await Task.Yield(); return 1; }")]
    [InlineData("void M() { }")]
    [InlineData("async ValueTask M() { await Task.CompletedTask; }")]
    public Task ShouldNotReportAsyncTask(string method) => VerifyAsync(
        $$"""
        using System.Threading.Tasks;
        public class C {
            {{method}}
        }
        """);

    [Fact]
    public Task ShouldNotReportEventHandler() => VerifyAsync(
        """
        using System;
        using System.Threading.Tasks;

        public class C {
            public async void OnClick(object sender, EventArgs e) {
                await Task.Delay(1);
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportDerivedEventArgs() => VerifyAsync(
        """
        using System;
        using System.Threading.Tasks;

        public class MyEventArgs : EventArgs { }

        public class C {
            public async void OnCustomEvent(object sender, MyEventArgs e) {
                await Task.Delay(1);
            }
        }
        """);
}

/// <summary>
///     Tests for AL1301: Avoid lock on 'this'.
/// </summary>
public sealed partial class Al1301AnalyzerTests : AnalyzerTest<Al1300ToAl1303ThreadingAnalyzer> {
    [Fact]
    public Task ShouldReportLockOnThis() => VerifyAsync(
        """
        public class C {
            void M() {
                lock ({|AL1301:this|}) { }
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportLockOnPrivateField() => VerifyAsync(
        """
        public class C {
            private readonly object _syncRoot = new();
            void M() {
                lock (_syncRoot) { }
            }
        }
        """);
}

/// <summary>
///     Tests for AL1302: Avoid lock on typeof(T).
/// </summary>
public sealed partial class Al1302AnalyzerTests : AnalyzerTest<Al1300ToAl1303ThreadingAnalyzer> {
    [Theory]
    [InlineData("typeof(C)")]
    [InlineData("typeof(string)")]
    [InlineData("typeof(object)")]
    public Task ShouldReportLockOnTypeOf(string typeOfExpr) => VerifyAsync(
        $$"""
        public class C {
            void M() {
                lock ({|AL1302:{{typeOfExpr}}|}) { }
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportLockOnStaticField() => VerifyAsync(
        """
        public class C {
            private static readonly object SyncRoot = new();
            void M() {
                lock (SyncRoot) { }
            }
        }
        """);
}

/// <summary>
///     Tests for AL1303: Avoid lock on string literals.
/// </summary>
public sealed partial class Al1303AnalyzerTests : AnalyzerTest<Al1300ToAl1303ThreadingAnalyzer> {
    [Theory]
    [InlineData("\"lock\"")]
    [InlineData("\"sync\"")]
    [InlineData("\"\"")]
    public Task ShouldReportLockOnStringLiteral(string stringLiteral) => VerifyAsync(
        $$"""
        public class C {
            void M() {
                lock ({|AL1303:{{stringLiteral}}|}) { }
            }
        }
        """);

    [Fact]
    public Task ShouldReportLockOnConstString() => VerifyAsync(
        """
        public class C {
            private const string LockKey = "key";
            void M() {
                lock ({|AL1303:LockKey|}) { }
            }
        }
        """);

    [Fact]
    public Task ShouldNotReportLockOnNonConstString() => VerifyAsync(
        """
        public class C {
            private static readonly string LockKey = "key";
            void M() {
                lock (LockKey) { }
            }
        }
        """);
}
