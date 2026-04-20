namespace ANcpLua.Analyzers.Analyzers;

using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
///     Detects whether the current compilation is targeting AOT — either via <c>PublishAot=true</c>
///     or <c>IsAotCompatible=true</c>. Rules whose advice is AOT-only (AL0094, AL0095, AL0096) should
///     gate on this; firing in non-AOT projects produces noise for legitimate dynamic / Expression.Compile
///     call sites (COM interop, DLR, test payload shaping, JIT-only services).
/// </summary>
internal static partial class AotContext
{
    internal static bool IsAotTargeting(AnalyzerConfigOptions globalOptions) =>
        IsTrue(globalOptions, "build_property.PublishAot") ||
        IsTrue(globalOptions, "build_property.IsAotCompatible");

    private static bool IsTrue(AnalyzerConfigOptions options, string key) =>
        options.TryGetValue(key, out var value)
        && string.Equals(value, "true", System.StringComparison.OrdinalIgnoreCase);
}
