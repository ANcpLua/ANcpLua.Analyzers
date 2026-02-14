namespace ANcpLua.Analyzers.Core;

/// <summary>
///     Standard severity levels for analyzers with documentation on when to use each.
/// </summary>
/// <remarks>
///     <para>
///         <b>IMPORTANT:</b> Avoid using <see cref="DiagnosticSeverity.Info"/> for analyzers
///         that should appear in normal build output. Info-level diagnostics are filtered
///         out by MSBuild by default and won't appear in build output or IDE error lists
///         unless explicitly configured.
///     </para>
///     <para>
///         Use <see cref="DiagnosticSeverity.Warning"/> for suggestions and code improvements.
///         Use <see cref="DiagnosticSeverity.Error"/> only for definite bugs or violations.
///     </para>
/// </remarks>
public static partial class DiagnosticSeverities {
    /// <summary>
    ///     Use for suggestions and code style improvements.
    ///     This appears in normal build output and IDE error lists.
    /// </summary>
    public const DiagnosticSeverity Suggestion = DiagnosticSeverity.Warning;

    /// <summary>
    ///     Use for definite bugs, security issues, or violations that must be fixed.
    /// </summary>
    public const DiagnosticSeverity RequiredFix = DiagnosticSeverity.Error;

    /// <summary>
    ///     Use only for diagnostics that should be hidden by default.
    ///     <b>WARNING:</b> Info-level diagnostics are NOT shown in normal build output!
    ///     Users must explicitly enable them via .editorconfig or MSBuild properties.
    /// </summary>
    public const DiagnosticSeverity HiddenByDefault = DiagnosticSeverity.Info;
}
