// Copyright (c) Alexander Nachtmann
// SPDX-License-Identifier: Apache-2.0

namespace ANcpLua.Analyzers.DocsGenerator;

internal enum Mode
{
    Generate,
    Check,
    Audit,
    EnforceIdsCheck,
    EnforceIdsApply,
}

internal static class CliModes
{
    public static Mode Parse(string[] args)
    {
        var flat = args
            .SelectMany(a => a.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries))
            .ToArray();

        var enforce = flat.Any(a => IsFlag(a, "enforce-ids"));
        var apply = flat.Any(a => IsFlag(a, "apply"));
        if (enforce) return apply ? Mode.EnforceIdsApply : Mode.EnforceIdsCheck;

        foreach (var arg in flat)
        {
            if (IsFlag(arg, "audit")) return Mode.Audit;
            if (IsFlag(arg, "check") || Eq(arg, "validate")) return Mode.Check;
        }
        return Mode.Generate;

        static bool IsFlag(string arg, string name) => Eq(arg, name) || Eq(arg, "--" + name);
        static bool Eq(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }
}
