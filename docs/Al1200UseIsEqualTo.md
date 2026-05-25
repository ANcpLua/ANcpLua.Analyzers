<!--
This is a generated file, please edit src/ANcpLua.Analyzers.AnalyzerDocs/<ScenarioClass>.cs
and re-run scripts/generate-docs.ps1 to refresh.
-->

# Al1200UseIsEqualTo Analyzer Docs

- [SymbolEqualityComparerDefaultEquals](#scenario-symbolequalitycomparerdefaultequals) - `//   if (symbol1.IsEqualTo(symbol2)) { }`


## Scenarios

### scenario: SymbolEqualityComparerDefaultEquals

```cs
// Rule:     AL1200 (Roslyn Utilities, Info)
// Fix:      Replaces SymbolEqualityComparer.Default.Equals(a, b) with a.IsEqualTo(b).
//
// Before (flagged):
//   if (SymbolEqualityComparer.Default.Equals(symbol1, symbol2)) { }
//
// After (clean):
//   if (symbol1.IsEqualTo(symbol2)) { }
```

#### Diagnostic

```text
AL1200: Use 'symbol1.IsEqualTo(symbol2)' instead of SymbolEqualityComparer.Default.Equals()
```


