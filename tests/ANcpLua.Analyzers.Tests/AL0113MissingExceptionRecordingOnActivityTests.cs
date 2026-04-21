using ANcpLua.Analyzers.Analyzers;
using ANcpLua.Roslyn.Utilities.Testing;

namespace ANcpLua.Analyzers.Tests;

/// <summary>
///     Tests for AL0113: Missing exception recording on Activity error status.
/// </summary>
public sealed partial class Al0113MissingExceptionRecordingOnActivityTests
    : AnalyzerTest<Al0113MissingExceptionRecordingOnActivityAnalyzer> {
    private const string ActivityStubs = """
        namespace System.Diagnostics {
            public enum ActivityStatusCode { Unset, Ok, Error }
            public class ActivityEvent {
                public ActivityEvent(string name) { }
            }
            public class Activity {
                public void SetStatus(ActivityStatusCode code, string? desc = null) { }
                public void AddEvent(ActivityEvent e) { }
                public void AddException(System.Exception ex) { }
            }
        }
        """;

    [Fact]
    public Task ShouldReport_SetStatusErrorWithoutExceptionRecording() =>
        VerifyAsync($$"""
            {{ActivityStubs}}

            public class C {
                public void M(System.Diagnostics.Activity activity) {
                    try { throw new System.Exception(); }
                    catch {
                        [|activity.SetStatus(System.Diagnostics.ActivityStatusCode.Error, "fail")|];
                    }
                }
            }
            """);

    [Fact]
    public Task ShouldNotReport_WhenAddEventWithExceptionCalled() =>
        VerifyAsync($$"""
            {{ActivityStubs}}

            public class C {
                public void M(System.Diagnostics.Activity activity) {
                    try { throw new System.Exception(); }
                    catch (System.Exception ex) {
                        activity.AddEvent(new System.Diagnostics.ActivityEvent("exception"));
                        activity.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
                    }
                }
            }
            """);

    [Fact]
    public Task ShouldNotReport_WhenStatusIsOk() =>
        VerifyAsync($$"""
            {{ActivityStubs}}

            public class C {
                public void M(System.Diagnostics.Activity activity) {
                    try { }
                    catch {
                        activity.SetStatus(System.Diagnostics.ActivityStatusCode.Ok);
                    }
                }
            }
            """);

    [Fact]
    public Task ShouldNotReport_WhenSetStatusOutsideCatch() =>
        VerifyAsync($$"""
            {{ActivityStubs}}

            public class C {
                public void M(System.Diagnostics.Activity activity) {
                    activity.SetStatus(System.Diagnostics.ActivityStatusCode.Error, "fail");
                }
            }
            """);

    [Fact]
    public Task ShouldNotReport_WhenAddEventAfterSetStatus() =>
        VerifyAsync($$"""
            {{ActivityStubs}}

            public class C {
                public void M(System.Diagnostics.Activity activity) {
                    try { throw new System.Exception(); }
                    catch (System.Exception ex) {
                        activity.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
                        activity.AddEvent(new System.Diagnostics.ActivityEvent("exception"));
                    }
                }
            }
            """);

    // AddException is the .NET 9+ counterpart to OTel's RecordException — it records the
    // exception as an ActivityEvent with the standard "exception" name and captures
    // type/message/stack as tags. The analyzer must treat it as exception recording.
    [Fact]
    public Task ShouldNotReport_WhenAddExceptionCalled() =>
        VerifyAsync($$"""
            {{ActivityStubs}}

            public class C {
                public void M(System.Diagnostics.Activity activity) {
                    try { throw new System.Exception(); }
                    catch (System.Exception ex) {
                        activity.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
                        activity.AddException(ex);
                    }
                }
            }
            """);
}
