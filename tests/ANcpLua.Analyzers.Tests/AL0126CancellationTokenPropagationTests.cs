using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Analyzers.CodeFixes.CodeFixes;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0126: forwarding available CancellationToken values to overloads that accept them.
/// </summary>
public sealed partial class Al0126CancellationTokenPropagationTests
    : AnalyzerTest<Al0126CancellationTokenPropagationAnalyzer> {
    private const string CommonStubs = """
                                      using System;
                                      using System.Linq.Expressions;
                                      using System.Threading;
                                      using System.Threading.Tasks;

                                      public interface IService {
                                          Task FetchAsync(int value);
                                          Task FetchAsync(int value, CancellationToken cancellationToken);
                                          Task SaveAsync(int value, string name);
                                          Task SaveAsync(int value, CancellationToken cancellationToken, string name);
                                      }

                                      public class ServiceBase : IService {
                                          public virtual Task FetchAsync(int value) => Task.CompletedTask;
                                          public virtual Task FetchAsync(int value, CancellationToken cancellationToken) => Task.CompletedTask;
                                          public virtual Task SaveAsync(int value, string name) => Task.CompletedTask;
                                          public virtual Task SaveAsync(int value, CancellationToken cancellationToken, string name) => Task.CompletedTask;
                                      }

                                      public sealed class Service : ServiceBase { }

                                      public sealed class DerivedService : ServiceBase {
                                          public override Task FetchAsync(int value) => Task.CompletedTask;
                                      }

                                      public sealed class OtherService {
                                          public Task PingAsync(int value) => Task.CompletedTask;
                                      }

                                      public sealed class HttpContextStub {
                                          public CancellationToken RequestAborted => default;
                                      }

                                      namespace Xunit {
                                          public sealed class TestContext {
                                              public static TestContext Current { get; } = new();
                                              public CancellationToken CancellationToken => default;
                                          }
                                      }

                                      namespace NSubstitute {
                                          public static class Substitute {
                                              public static T For<T>() where T : class => default!;
                                          }

                                          public static class SubstituteExtensions {
                                              public static T Returns<T>(this T value, T returnValue) => value;
                                          }
                                      }
                                      """;

    [Fact]
    public Task ShouldReportWhenMethodParameterProvidesToken() =>
        VerifyAsync($$"""
                      {{CommonStubs}}

                      public sealed class C {
                          private readonly Service _service = new();

                          public Task M(CancellationToken cancellationToken) =>
                              {|AL0126:_service.FetchAsync(42)|};
                      }
                      """);

    [Fact]
    public Task ShouldReportWhenLocalTokenProvidesToken() =>
        VerifyAsync($$"""
                      {{CommonStubs}}

                      public sealed class C {
                          private readonly Service _service = new();

                          public Task M() {
                              CancellationToken token = default;
                              return {|AL0126:_service.FetchAsync(42)|};
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportWhenTokenSourceProvidesToken() =>
        VerifyAsync($$"""
                      {{CommonStubs}}

                      public sealed class C {
                          private readonly Service _service = new();

                          public Task M() {
                              var cts = new CancellationTokenSource();
                              return {|AL0126:_service.FetchAsync(42)|};
                          }
                      }
                      """);

    [Fact]
    public Task ShouldReportWhenContainingMemberProvidesToken() =>
        VerifyAsync($$"""
                      {{CommonStubs}}

                      public sealed class C {
                          private readonly Service _service = new();
                          private readonly CancellationToken _cancellationToken = default;

                          public Task M() =>
                              {|AL0126:_service.FetchAsync(42)|};
                      }
                      """);

    [Fact]
    public Task ShouldReportWhenHttpContextRequestAbortedIsAvailable() =>
        VerifyAsync($$"""
                      {{CommonStubs}}

                      public sealed class C {
                          private readonly Service _service = new();

                          public HttpContextStub HttpContext { get; } = new();

                          public Task M() =>
                              {|AL0126:_service.FetchAsync(42)|};
                      }
                      """);

    [Fact]
    public Task ShouldReportWhenOverloadExistsOnBaseType() =>
        VerifyAsync($$"""
                      {{CommonStubs}}

                      public sealed class C {
                          private readonly DerivedService _service = new();

                          public Task M(CancellationToken cancellationToken) =>
                              {|AL0126:_service.FetchAsync(42)|};
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenInvocationAlreadyPassesToken() =>
        VerifyAsync($$"""
                      {{CommonStubs}}

                      public sealed class C {
                          private readonly Service _service = new();

                          public Task M(CancellationToken cancellationToken) =>
                              _service.FetchAsync(42, cancellationToken);
                      }
                      """);

    [Fact]
    public Task ShouldNotReportWhenNoCancellationOverloadExists() =>
        VerifyAsync($$"""
                      {{CommonStubs}}

                      public sealed class C {
                          private readonly OtherService _service = new();

                          public Task M(CancellationToken cancellationToken) =>
                              _service.PingAsync(42);
                      }
                      """);

    [Fact]
    public Task ShouldNotReportInsideExpressionTree() =>
        VerifyAsync($$"""
                      {{CommonStubs}}

                      public sealed class C {
                          private readonly Service _service = new();
                          private readonly CancellationToken _cancellationToken = default;

                          public Expression<Func<Task>> Build() =>
                              () => _service.FetchAsync(42);
                      }
                      """);

    [Fact]
    public Task ShouldNotReportForInterfaceImplementationWithoutToken() =>
        VerifyAsync($$"""
                      {{CommonStubs}}

                      public interface IContract {
                          Task HandleAsync(int value);
                      }

                      public sealed class C : IContract {
                          private readonly Service _service = new();
                          private readonly CancellationToken _cancellationToken = default;

                          public Task HandleAsync(int value) =>
                              _service.FetchAsync(value);
                      }
                      """);

    [Fact]
    public Task ShouldNotReportForOverrideWithoutToken() =>
        VerifyAsync($$"""
                      {{CommonStubs}}

                      public abstract class BaseHandler {
                          public abstract Task HandleAsync(int value);
                      }

                      public sealed class C : BaseHandler {
                          private readonly Service _service = new();
                          private readonly CancellationToken _cancellationToken = default;

                          public override Task HandleAsync(int value) =>
                              _service.FetchAsync(value);
                      }
                      """);

    [Fact]
    public Task ShouldNotReportInsideNSubstituteReturnsChain() =>
        VerifyAsync($$"""
                      {{CommonStubs}}

                      public sealed class C {
                          public void M() {
                              var substitute = NSubstitute.Substitute.For<IService>();
                              NSubstitute.SubstituteExtensions.Returns(substitute.FetchAsync(42), Task.CompletedTask);
                          }
                      }
                      """);

    [Fact]
    public Task ShouldNotReportForConditionalAccessInvocation() =>
        VerifyAsync($$"""
                      {{CommonStubs}}

                      public sealed class C {
                          private readonly Service? _service = new();

                          public Task? M(CancellationToken cancellationToken) =>
                              _service?.FetchAsync(42);
                      }
                      """);
}

/// <summary>
///     Code fix tests for AL0126: inserts the nearest CancellationToken argument.
/// </summary>
public sealed partial class Al0126CancellationTokenPropagationCodeFixTests
    : CodeFixTest<Al0126CancellationTokenPropagationAnalyzer, Al0126CancellationTokenPropagationCodeFixProvider> {
    private const string CommonStubs = """
                                      using System;
                                      using System.Threading;
                                      using System.Threading.Tasks;

                                      public class Service {
                                          public Task FetchAsync(int value) => Task.CompletedTask;
                                          public Task FetchAsync(int value, CancellationToken cancellationToken) => Task.CompletedTask;
                                          public Task SaveAsync(int value, string name) => Task.CompletedTask;
                                          public Task SaveAsync(int value, CancellationToken cancellationToken, string name) => Task.CompletedTask;
                                      }

                                      public sealed class HttpContextStub {
                                          public CancellationToken RequestAborted => default;
                                      }
                                      """;

    private const string XunitStub = """
                                     namespace Xunit {
                                         public sealed class TestContext {
                                             public static TestContext Current { get; } = new();
                                             public System.Threading.CancellationToken CancellationToken => default;
                                         }
                                     }
                                     """;

    [Fact]
    public Task ShouldAddMethodParameterAtEnd() =>
        VerifyAsync($$"""
                      {{CommonStubs}}

                      public sealed class C {
                          private readonly Service _service = new();

                          public Task M(CancellationToken cancellationToken) =>
                              {|AL0126:_service.FetchAsync(42)|};
                      }
                      """,
            $$"""
              {{CommonStubs}}

              public sealed class C {
                  private readonly Service _service = new();

                  public Task M(CancellationToken cancellationToken) =>
                      _service.FetchAsync(42, cancellationToken: cancellationToken);
              }
              """);

    [Fact]
    public Task ShouldAddCancellationTokenSourceToken() =>
        VerifyAsync($$"""
                      {{CommonStubs}}

                      public sealed class C {
                          private readonly Service _service = new();

                          public Task M() {
                              var cts = new CancellationTokenSource();
                              return {|AL0126:_service.FetchAsync(42)|};
                          }
                      }
                      """,
            $$"""
              {{CommonStubs}}

              public sealed class C {
                  private readonly Service _service = new();

                  public Task M() {
                      var cts = new CancellationTokenSource();
                      return _service.FetchAsync(42, cancellationToken: cts.Token);
                  }
              }
              """);

    [Fact]
    public Task ShouldInsertCancellationTokenInTheMiddle() =>
        VerifyAsync($$"""
                      {{CommonStubs}}

                      public sealed class C {
                          private readonly Service _service = new();

                          public Task M() {
                              CancellationToken token = default;
                              return {|AL0126:_service.SaveAsync(42, "demo")|};
                          }
                      }
                      """,
            $$"""
              {{CommonStubs}}

              public sealed class C {
                  private readonly Service _service = new();

                  public Task M() {
                      CancellationToken token = default;
                      return _service.SaveAsync(42, cancellationToken: token, name: "demo");
                  }
              }
              """);

    [Fact]
    public Task ShouldUseHttpContextRequestAborted() =>
        VerifyAsync($$"""
                      {{CommonStubs}}

                      public sealed class C {
                          private readonly Service _service = new();

                          public HttpContextStub HttpContext { get; } = new();

                          public Task M() =>
                              {|AL0126:_service.FetchAsync(42)|};
                      }
                      """,
            $$"""
              {{CommonStubs}}

              public sealed class C {
                  private readonly Service _service = new();

                  public HttpContextStub HttpContext { get; } = new();

                  public Task M() =>
                      _service.FetchAsync(42, cancellationToken: HttpContext.RequestAborted);
              }
              """);

    [Fact]
    public Task ShouldUseXunitTestContextWhenNothingCloserExists() =>
        VerifyAsync($$"""
                      {{CommonStubs}}
                      {{XunitStub}}

                      public sealed class C {
                          private readonly Service _service = new();

                          public Task M() =>
                              {|AL0126:_service.FetchAsync(42)|};
                      }
                      """,
            $$"""
              {{CommonStubs}}
              {{XunitStub}}

              public sealed class C {
                  private readonly Service _service = new();

                  public Task M() =>
                      _service.FetchAsync(42, cancellationToken: global::Xunit.TestContext.Current.CancellationToken);
              }
              """);

    [Fact]
    public Task ShouldFixAllInDocument() =>
        VerifyAsync($$"""
                      {{CommonStubs}}

                      public sealed class C {
                          private readonly Service _service = new();

                          public async Task First(CancellationToken cancellationToken) {
                              await {|AL0126:_service.FetchAsync(1)|};
                              await {|AL0126:_service.FetchAsync(2)|};
                          }

                          public async Task Second(CancellationToken ct) {
                              await {|AL0126:_service.FetchAsync(3)|};
                          }
                      }
                      """,
            $$"""
              {{CommonStubs}}

              public sealed class C {
                  private readonly Service _service = new();

                  public async Task First(CancellationToken cancellationToken) {
                      await _service.FetchAsync(1, cancellationToken: cancellationToken);
                      await _service.FetchAsync(2, cancellationToken: cancellationToken);
                  }

                  public async Task Second(CancellationToken ct) {
                      await _service.FetchAsync(3, cancellationToken: ct);
                  }
              }
              """);
}
