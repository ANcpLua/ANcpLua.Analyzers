// Copyright (c) Alexander Nachtmann
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;

namespace ANcpLua.Analyzers.DocsGenerator;

// SARIF v2.1.0 rule manifest. runs[0].results is intentionally empty: this is a rule catalog for
// tool interop (Sonar bridges, GitHub Advanced Security, IDE rule catalogs), not an execution
// result. Indent + sort-by-id keep the output deterministic for --check drift detection.
// Spec: https://docs.oasis-open.org/sarif/sarif/v2.1.0/sarif-v2.1.0.html
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
