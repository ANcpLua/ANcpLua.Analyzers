# Analyzer Confidence Audit - 2026-05-23

Scope: identify rules that either enforce an equally valid alternative or depend on context the analyzer cannot reliably know. This is separate from high-confidence correctness, security, AOT, packaging, and generator-performance diagnostics.

## Decision Applied

| Rule | Action | Reason |
| --- | --- | --- |
| AL0140 | Deleted | `var` vs explicit local type has no JIT/runtime optimization effect. It is a source readability/tooling preference, and "type is not apparent" is not a stable correctness boundary for a package analyzer. |

Evidence used for AL0140:

- Microsoft Learn describes `var` as compiler inference for a local variable; it is not a runtime type choice: <https://learn.microsoft.com/dotnet/csharp/programming-guide/classes-and-structs/implicitly-typed-local-variables>.
- The C# query-expression documentation states that `var` is sometimes required for anonymous types and otherwise optional/convenience: <https://learn.microsoft.com/dotnet/csharp/programming-guide/classes-and-structs/how-to-use-implicitly-typed-local-variables-and-arrays-in-a-query-expression>.
- Roslyn discussion dotnet/roslyn#49387 confirms there is no semantic difference between explicit and implicit local declarations, while source-level IDE features such as Find References may behave differently: <https://github.com/dotnet/roslyn/discussions/49387>. That makes AL0140 a tooling/readability preference, not a correctness diagnostic.

## Equal-Alternative Or Style-Policy Rules

These rules can be useful in a tightly controlled codebase, but they should not be treated as broad correctness warnings without an explicit project policy.

| Rule(s) | Current concern | Recommendation |
| --- | --- | --- |
| AL0011 | `System.Threading.Lock` is preferable on modern TFMs, but ordinary private object locks are still a valid synchronization pattern. The rule already leaves older TFMs alone by requiring the `Lock` type to exist. | Consider Suggestion/opt-in unless the repo standard is "always use `System.Threading.Lock` on .NET 9+". |
| AL0014 | Pattern matching for `null`/zero is now style-only in this implementation because overloaded equality operators are skipped. Roslyn/IDE style settings already cover this space. | Delete, or keep only as disabled-by-default style if the package wants a local convention. |
| AL0015 | `Throw.IfNull`, BCL `ArgumentNullException.ThrowIfNull`, and portable coalesce-throw are all valid conventions depending on target frameworks and dependencies. | Keep Info/hidden only, or move to a repo-policy analyzer pack. |
| AL0016 | Combining declaration plus null check is a refactoring preference, not correctness. | Keep Info/hidden only; consider making it a refactoring instead of a diagnostic. |
| AL0139 | This is the surviving half of the `var` rule. It is less harmful than AL0140 and matches this repo's style, but explicit type is still a valid alternative. | Keep only if the package intentionally enforces `var`-when-apparent; otherwise delete with the same rationale as AL0140. |
| AL0026 | `TimeProvider` is better for testability, but `DateTime.UtcNow` and related APIs are legitimate in many code paths. | Downgrade/keep opt-in unless the consuming repo has a testability policy. |
| AL0027 | `System.Text.Json` is not a universal replacement for `Newtonsoft.Json`; feature compatibility and ecosystem constraints matter. | Downgrade/keep opt-in unless the consuming repo has a no-Newtonsoft policy. |

## Framework-Convention Rules

These are defensible inside the ANcpLua framework chain because they enforce its own utility surface, but they are not universal correctness rules.

| Rule(s) | Current concern | Recommendation |
| --- | --- | --- |
| AL0028-AL0035, AL0125 | Prefer ANcpLua.Roslyn.Utilities helpers over equivalent Roslyn/BCL patterns. | Keep as Info/hidden. |
| AL0036-AL0040, AL0045-AL0051, AL0137 | Prefer Guard/StringComparison/Attribute helpers over equivalent explicit code or BCL/MAF throw helpers. Some are currently warnings. | Recheck severity. Warning is only justified for repos that explicitly opt into ANcpLua helper conventions. |

## Context-Confidence Risk

These rules try to infer architectural intent from local syntax/operations. The risk is not that the goal is bad; the risk is that the analyzer cannot prove the premise well enough to be a warning in arbitrary consumer IDEs.

| Rule | Confidence issue | Recommendation |
| --- | --- | --- |
| AL0053 | "Unnecessary `[AotUnsafe]`" is absence-of-evidence analysis. The analyzer can find known unsafe patterns, but cannot prove an annotation is unnecessary. | Keep as Suggestion/hidden and phrase as "no known unsafe pattern detected"; do not raise to Warning. |
| AL0080 | Resilience configuration may be applied through wrappers, extension methods, named clients, builder composition, or another method. Same-method scanning is incomplete. | Opt-in only, or require recognized ASP.NET/Aspire service-defaults patterns before reporting. |
| AL0081 | Health-check requirements depend on deployment target and may be composed through wrappers. | Opt-in only. Avoid warning by default. |
| AL0082 | Hardcoded connection-string detection is heuristic, but it is already hidden by default. | Keep hidden. |
| AL0084 | Service discovery cannot be inferred from URL shape. External APIs, environment-specific endpoints, and intentional fixed URLs are common. | Opt-in only; keep as Suggestion/hidden. |
| AL0106 | `Task.Run` in ASP.NET Core is often suspicious, but deliberate CPU-bound offload exists. | Keep Suggestion/hidden; warning only with project policy. |
| AL0117 | Removing materialization can change snapshot semantics, side effects, source mutation timing, or consumer behavior. Current implementation has safeguards but cannot prove intent. | Keep hidden. |
| AL0118 | Read-modify-write transaction safety cannot be proven from method names alone. Connection/command/transaction flow and database semantics can be outside the local method. | Downgrade to hidden or redesign before Warning. |
| AL0126 | Cancellation-token propagation is useful, and the implementation has many suppressions, but token choice is still heuristic. | Keep Info; do not raise to Warning. |

## Stronger Keep List

These rules have a defensible correctness, security, compatibility, or contract basis and are not primarily equal-alternative style:

- AL0001-AL0009 except style candidates listed above
- AL0017-AL0025
- AL0041-AL0044, AL0052
- AL0054-AL0060
- AL0094-AL0095, AL0101-AL0105
- AL0111-AL0116 except context-risk rules listed above
- AL0119-AL0123, AL0127-AL0130, AL0138

## Next Suggested Changes

1. Decide whether AL0139 should stay. If the standard is "no equal-alternative style analyzers," delete AL0139 too.
2. Downgrade warning-level framework-convention rules that are only valid inside ANcpLua-owned repos.
3. Demote AL0080, AL0081, AL0084, and AL0118 before publishing broadly, unless they are made explicitly opt-in by `.editorconfig`.
