// Copyright (c) Alexander Nachtmann
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Analyzers.DocsGenerator;

/// <summary>
///   Emits a SARIF v2.1.0 rule manifest describing every <see cref="DiagnosticDescriptor"/>
///   this package ships. Each descriptor maps to one <c>reportingDescriptor</c> entry
///   inside <c>runs[0].tool.driver.rules</c>. The file is run-results-free —
///   <c>runs[0].results</c> is empty — because this is a <i>rule catalog</i> for tool
///   interop (Sonar bridges, GitHub Advanced Security uploads, IDE rule catalogs),
///   not an analyzer execution result. Indent + sort-by-id keeps the output
///   deterministic for <c>--check</c> drift detection.
///
///   Spec: https://docs.oasis-open.org/sarif/sarif/v2.1.0/sarif-v2.1.0.html
///
///   Extension point: future machine-readable catalogs (CodeQL pack manifest, OWASP-ASVS
///   mapping JSON) land as sibling classes next to this one with the same shape — accept
///   <c>(descriptors, idToClass)</c>, return deterministic text, ship a <c>*Path</c>
///   helper on <see cref="RepoLayout"/>.
/// </summary>
internal static class SarifRenderer
{
    public static string Render(
        IReadOnlyList<DiagnosticDescriptor> descriptors,
        Dictionary<string, string> idToClass)
    {
        var rulesArray = new JsonArray();
        foreach (var d in descriptors)
        {
            var ruleName = idToClass.TryGetValue(d.Id, out var className)
                ? SymbolicNaming.ToSymbolicName(className)
                : d.Id;

            var rule = new JsonObject
            {
                ["id"] = d.Id,
                ["name"] = ruleName,
                ["shortDescription"] = new JsonObject { ["text"] = d.Title.ToString() },
                ["fullDescription"] = new JsonObject { ["text"] = d.Description.ToString() },
                ["helpUri"] = d.HelpLinkUri,
            };

            var defaultConfig = new JsonObject { ["level"] = SarifLevel(d.DefaultSeverity) };
            if (!d.IsEnabledByDefault)
                defaultConfig["enabled"] = false;
            rule["defaultConfiguration"] = defaultConfig;

            rule["properties"] = new JsonObject { ["category"] = d.Category };
            rulesArray.Add(rule);
        }

        var doc = new JsonObject
        {
            ["$schema"] = "https://json.schemastore.org/sarif-2.1.0.json",
            ["version"] = "2.1.0",
            ["runs"] = new JsonArray(
                new JsonObject
                {
                    ["tool"] = new JsonObject
                    {
                        ["driver"] = new JsonObject
                        {
                            ["name"] = RepoLayout.PackageName,
                            ["informationUri"] = "https://github.com/ANcpLua/ANcpLua.Analyzers",
                            ["rules"] = rulesArray,
                        },
                    },
                    ["results"] = new JsonArray(),
                }),
        };

        var json = doc.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        return json.ReplaceLineEndings("\n") + "\n";
    }

    private static string SarifLevel(DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Error => "error",
        DiagnosticSeverity.Warning => "warning",
        DiagnosticSeverity.Info => "note",
        DiagnosticSeverity.Hidden => "none",
        _ => "none",
    };
}
