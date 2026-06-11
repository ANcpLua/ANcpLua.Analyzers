// Copyright (c) Alexander Nachtmann
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace ANcpLua.Analyzers.DocsGenerator;

// AL0xxx -> AL1xxx rename map from the 2.0.0 break. Documentation only: consumed by the docs
// generator under tools/ and does NOT ship in the runtime analyzer DLL. The 2.0.1 analyzer no
// longer emits AL0xxx, so no runtime code needs the old IDs; this exists purely so the generated
// migration-catalog markdown can tell consumers "your editorconfig says AL0001 -- that's now AL1000".
// If a follow-up diagnostic ever warns on stale dotnet_diagnostic.AL0xxx.severity entries, this
// catalog moves to src/ANcpLua.Analyzers/ with it.
// Validate() is covered by a mandatory AnalyzerConventionTests unit test, so hand-transcription
// drift is caught at CI rather than only when --check happens to run on a dev machine.
public sealed record AlIdRename(string OldId, string NewId, string Band, string Title);

// Public (not internal) so the test project at tests/ANcpLua.Analyzers.Tests/ can
// invoke Validate() via a plain ProjectReference, without InternalsVisibleTo —
// IVT would expose this Exe assembly's internal top-level Program and collide
// with the tests project's own Program (CS0433). Public is safe because the
// tools assembly is never packed or referenced by consumers.
public static class AlIdMigrationCatalog
{
    public static readonly ImmutableArray<AlIdRename> Entries =
    [
        new("AL0001", "AL1000", "AL1000..AL1099 Correctness", "Prohibit reassignment of primary constructor parameters"),
        new("AL0002", "AL1001", "AL1000..AL1099 Correctness", "Don't repeat negated patterns"),
        new("AL0003", "AL1002", "AL1000..AL1099 Correctness", "Don't divide by constant zero"),
        new("AL0004", "AL1003", "AL1000..AL1099 Correctness", "Use pattern matching when comparing Span with constants"),
        new("AL0005", "AL1004", "AL1000..AL1099 Correctness", "Use SequenceEqual when comparing Span with non-constants"),
        new("AL0006", "AL1005", "AL1000..AL1099 Correctness", "Field name conflicts with primary constructor parameter"),
        new("AL0007", "AL1006", "AL1000..AL1099 Correctness", "GetSchema should be explicitly implemented"),
        new("AL0008", "AL1007", "AL1000..AL1099 Correctness", "GetSchema must return null and not be abstract"),
        new("AL0009", "AL1008", "AL1000..AL1099 Correctness", "Don't call IXmlSerializable.GetSchema"),
        new("AL0011", "AL1009", "AL1000..AL1099 Correctness", "Avoid lock keyword on non-Lock types"),
        new("AL0014", "AL1010", "AL1000..AL1099 Correctness", "Prefer pattern matching for null and zero comparisons"),
        new("AL0015", "AL1011", "AL1000..AL1099 Correctness", "Normalize null-guard style"),
        new("AL0016", "AL1012", "AL1000..AL1099 Correctness", "Combine declaration with subsequent null-check"),
        new("AL0020", "AL1100", "AL1100..AL1199 ASP.NET Core", "IFormCollection requires explicit attribute"),
        new("AL0021", "AL1101", "AL1100..AL1199 ASP.NET Core", "Multiple structured form sources"),
        new("AL0022", "AL1102", "AL1100..AL1199 ASP.NET Core", "Mixed form collection and DTO"),
        new("AL0023", "AL1103", "AL1100..AL1199 ASP.NET Core", "Unsupported form type"),
        new("AL0024", "AL1104", "AL1100..AL1199 ASP.NET Core", "Form and body conflict"),
        new("AL0080", "AL1105", "AL1100..AL1199 ASP.NET Core", "Missing resilience configuration"),
        new("AL0081", "AL1106", "AL1100..AL1199 ASP.NET Core", "Missing health checks"),
        new("AL0082", "AL1107", "AL1100..AL1199 ASP.NET Core", "Consider using configuration for connection string"),
        new("AL0084", "AL1108", "AL1100..AL1199 ASP.NET Core", "Missing service discovery"),
        new("AL0106", "AL1109", "AL1100..AL1199 ASP.NET Core", "Avoid Task.Run in ASP.NET Core request handlers"),
        new("AL0028", "AL1200", "AL1200..AL1299 Roslyn Utilities", "Use IsEqualTo extension"),
        new("AL0029", "AL1201", "AL1200..AL1299 Roslyn Utilities", "Use HasAttribute extension"),
        new("AL0030", "AL1202", "AL1200..AL1299 Roslyn Utilities", "Use type hierarchy extension"),
        new("AL0031", "AL1203", "AL1200..AL1299 Roslyn Utilities", "Use operation extension"),
        new("AL0032", "AL1204", "AL1200..AL1299 Roslyn Utilities", "Use OrEmpty extension"),
        new("AL0033", "AL1205", "AL1200..AL1299 Roslyn Utilities", "Use ToImmutableArrayOrEmpty extension"),
        new("AL0034", "AL1206", "AL1200..AL1299 Roslyn Utilities", "Use WhereNotNull extension"),
        new("AL0035", "AL1207", "AL1200..AL1299 Roslyn Utilities", "Use symbol display string extension"),
        new("AL0036", "AL1208", "AL1200..AL1299 Roslyn Utilities", "Use null-guard helper"),
        new("AL0037", "AL1209", "AL1200..AL1299 Roslyn Utilities", "Use TryParse extension"),
        new("AL0039", "AL1210", "AL1200..AL1299 Roslyn Utilities", "Use StringComparison extension"),
        new("AL0040", "AL1211", "AL1200..AL1299 Roslyn Utilities", "Use attribute argument extraction extension"),
        new("AL0045", "AL1212", "AL1200..AL1299 Roslyn Utilities", "Use null-or-empty guard helper"),
        new("AL0046", "AL1213", "AL1200..AL1299 Roslyn Utilities", "Use null-or-whitespace guard helper"),
        new("AL0047", "AL1214", "AL1200..AL1299 Roslyn Utilities", "Use zero-guard helper"),
        new("AL0048", "AL1215", "AL1200..AL1299 Roslyn Utilities", "Use non-negative guard helper"),
        new("AL0049", "AL1216", "AL1200..AL1299 Roslyn Utilities", "Use positive-guard helper"),
        new("AL0050", "AL1217", "AL1200..AL1299 Roslyn Utilities", "Use empty-guid guard helper"),
        new("AL0051", "AL1218", "AL1200..AL1299 Roslyn Utilities", "Use defined-enum guard helper"),
        new("AL0125", "AL1219", "AL1200..AL1299 Roslyn Utilities", "Use *Any* string comparison extension"),
        new("AL0137", "AL1220", "AL1200..AL1299 Roslyn Utilities", "Use Guard.* helpers instead of throw helpers"),
        new("AL0057", "AL1300", "AL1300..AL1399 Async / reliability", "Avoid async void methods"),
        new("AL0058", "AL1301", "AL1300..AL1399 Async / reliability", "Avoid lock on 'this'"),
        new("AL0059", "AL1302", "AL1300..AL1399 Async / reliability", "Avoid lock on typeof(T)"),
        new("AL0060", "AL1303", "AL1300..AL1399 Async / reliability", "Avoid lock on string"),
        new("AL0104", "AL1304", "AL1300..AL1399 Async / reliability", "Prefer 'await using' for IAsyncDisposable"),
        new("AL0105", "AL1305", "AL1300..AL1399 Async / reliability", "Avoid blocking calls in async methods"),
        new("AL0111", "AL1306", "AL1300..AL1399 Async / reliability", "Avoid SQL string interpolation in CommandText"),
        new("AL0112", "AL1307", "AL1300..AL1399 Async / reliability", "Avoid fire-and-forget task discard"),
        new("AL0114", "AL1308", "AL1300..AL1399 Async / reliability", "Prefer TryParse over Parse"),
        new("AL0115", "AL1309", "AL1300..AL1399 Async / reliability", "Empty catch block swallows exceptions"),
        new("AL0116", "AL1310", "AL1300..AL1399 Async / reliability", "Exception details leaked in HTTP response"),
        new("AL0117", "AL1311", "AL1300..AL1399 Async / reliability", "Unnecessary LINQ materialization"),
        new("AL0118", "AL1312", "AL1300..AL1399 Async / reliability", "Read-modify-write without transaction"),
        new("AL0126", "AL1313", "AL1300..AL1399 Async / reliability", "Forward CancellationToken to invocations that support it"),
        new("AL0138", "AL1314", "AL1300..AL1399 Async / reliability", "Use Math.Round/MathF.Round overload with explicit MidpointRounding"),
        new("AL0041", "AL1400", "AL1400..AL1499 AOT / trim", "Method with [AotTest] or [TrimTest] must return int"),
        new("AL0042", "AL1401", "AL1400..AL1499 AOT / trim", "[AotTest]/[TrimTest] method should return 100 on success"),
        new("AL0043", "AL1402", "AL1400..AL1499 AOT / trim", "[TrimSafe] code must not call methods with [RequiresUnreferencedCode]"),
        new("AL0044", "AL1403", "AL1400..AL1499 AOT / trim", "[AotSafe] code must not call methods with [RequiresDynamicCode]"),
        new("AL0052", "AL1404", "AL1400..AL1499 AOT / trim", "[AotSafe] code must not call [AotUnsafe] code"),
        new("AL0053", "AL1405", "AL1400..AL1499 AOT / trim", "Unnecessary [AotUnsafe] attribute"),
        new("AL0094", "AL1406", "AL1400..AL1499 AOT / trim", "Avoid 'dynamic' keyword in AOT-published code"),
        new("AL0095", "AL1407", "AL1400..AL1499 AOT / trim", "Avoid Expression.Compile() in AOT context"),
        new("AL0101", "AL1408", "AL1400..AL1499 AOT / trim", "Activator.CreateInstance is not AOT-safe"),
        new("AL0102", "AL1409", "AL1400..AL1499 AOT / trim", "Type.GetType with dynamic name is not AOT-safe"),
        new("AL0103", "AL1500", "AL1500..AL1599 Roslyn-author hygiene", "Closed hierarchy match is not exhaustive"),
        new("AL0119", "AL1501", "AL1500..AL1599 Roslyn-author hygiene", "Avoid storing ISymbol in source generator models"),
        new("AL0120", "AL1502", "AL1500..AL1599 Roslyn-author hygiene", "Use IIncrementalGenerator instead of ISourceGenerator"),
        new("AL0121", "AL1503", "AL1500..AL1599 Roslyn-author hygiene", "Avoid NormalizeWhitespace in source generators"),
        new("AL0122", "AL1504", "AL1500..AL1599 Roslyn-author hygiene", "[DuckDbTable] type must be partial"),
        new("AL0123", "AL1505", "AL1500..AL1599 Roslyn-author hygiene", "Conflicting [DuckDbColumn] ordinal values"),
        new("AL0017", "AL1600", "AL1600..AL1699 Package / version", "Hardcoded package version detected"),
        new("AL0018", "AL1601", "AL1600..AL1699 Package / version", "Version.props not imported"),
        new("AL0019", "AL1602", "AL1600..AL1699 Package / version", "Undefined version variable"),
        new("AL0054", "AL1603", "AL1600..AL1699 Package / version", "Diagnostic missing from documentation"),
        new("AL0055", "AL1604", "AL1600..AL1699 Package / version", "Diagnostic missing from release notes"),
        new("AL0056", "AL1605", "AL1600..AL1699 Package / version", "Diagnostic documentation mismatch"),
        new("AL0127", "AL1606", "AL1600..AL1699 Package / version", "Outdated MAF ecosystem package version"),
        new("AL0025", "AL1700", "AL1700..AL1799 Style", "Anonymous function can be made static"),
        new("AL0026", "AL1701", "AL1700..AL1799 Style", "Avoid DateTime/DateTimeOffset time accessors"),
        new("AL0027", "AL1702", "AL1700..AL1799 Style", "Avoid legacy JSON library"),
        new("AL0139", "AL1703", "AL1700..AL1799 Style", "Use implicit type when type is apparent"),
        new("AL0128", "AL1800", "AL1800..AL1899 Agent governance", "Destructive Loom tool must require approval"),
        new("AL0129", "AL1801", "AL1800..AL1899 Agent governance", "Loom tool should declare its side effect"),
        new("AL0130", "AL1802", "AL1800..AL1899 Agent governance", "Loom tool should declare required capabilities"),
    ];

    private static readonly Regex NewIdRegex = new(@"^AL1[0-8]\d{2}$", RegexOptions.Compiled);
    private static readonly Regex OldIdRegex = new(@"^AL\d{4}$", RegexOptions.Compiled);

    // Structural invariants. No hardcoded ExpectedCount: count is a consequence of the
    // safety properties below, not a property worth asserting.
    public static void Validate()
    {
        if (Entries.IsDefaultOrEmpty)
            throw new InvalidOperationException("AlIdMigrationCatalog.Entries must be populated.");

        var dupOld = Entries.GroupBy(e => e.OldId, StringComparer.Ordinal)
                            .FirstOrDefault(g => g.Count() > 1);
        if (dupOld is not null)
            throw new InvalidOperationException($"Duplicate OldId in catalog: {dupOld.Key}");

        var dupNew = Entries.GroupBy(e => e.NewId, StringComparer.Ordinal)
                            .FirstOrDefault(g => g.Count() > 1);
        if (dupNew is not null)
            throw new InvalidOperationException($"Duplicate NewId in catalog: {dupNew.Key}");

        // Post-renumber band: AL1000..AL1899 (9 bands of 100, per renumber-plan §1).
        // Sibling packages (AotReflection, ExtensibleEnumMirror, DiscriminatedUnion)
        // own slots inside AL0xxx — a leak into NewId means the renumber regressed.
        var badNew = Entries.FirstOrDefault(e => !NewIdRegex.IsMatch(e.NewId));
        if (badNew is not null)
            throw new InvalidOperationException(
                $"NewId {badNew.NewId} (was {badNew.OldId}) violates ^AL1[0-8]\\d{{2}}$.");

        var badOld = Entries.FirstOrDefault(e => !OldIdRegex.IsMatch(e.OldId));
        if (badOld is not null)
            throw new InvalidOperationException(
                $"OldId {badOld.OldId} violates ^AL\\d{{4}}$.");
    }
}
