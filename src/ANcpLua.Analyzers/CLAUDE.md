# ANcpLua.Analyzers - Analyzer Project

DiagnosticAnalyzer implementations (AL0001-AL0044). See root CLAUDE.md for full diagnostic list.

## Target

- **Framework:** netstandard2.0 (required for Roslyn analyzers)
- **Roslyn:** 5.0.0

## File Structure

```
Analyzers/
  AL0001*.cs              # One analyzer per file (or grouped like AL0004ToAL0005*.cs)
Core/
  ALAnalyzer.cs           # Base class + DiagnosticIds + DiagnosticCategories + DiagnosticSeverities
  WellKnownTypes.cs       # WellKnownType enum + WellKnownTypeCache
  OperationHelper.cs      # IsArgumentNullException(), IsArgumentException(), etc.
  DeprecatedOtelAttributes.cs  # OTel attribute mappings for AL0012
Resources.resx            # Localized diagnostic strings (Title, MessageFormat, Description)
```

## Adding a New Analyzer

1. Create `Analyzers/AL00XXDescriptionAnalyzer.cs`
2. Add diagnostic ID constant to `Core/ALAnalyzer.cs` in `DiagnosticIds` class
3. Add localized strings to `Resources.resx`:
   - `AL00XX_Title`
   - `AL00XX_MessageFormat`
   - `AL00XX_Description`
4. Inherit from `AlAnalyzer` base class
5. Add tests in `tests/ANcpLua.Analyzers.Tests/`

## Analyzer Base Class Pattern

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al00XXMyAnalyzer : AlAnalyzer {
    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.MyRule,                    // From Core/ALAnalyzer.cs
        new LocalizableResourceString(nameof(Resources.AL00XX_Title), Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString(nameof(Resources.AL00XX_MessageFormat), Resources.ResourceManager, typeof(Resources)),
        DiagnosticCategories.Design,             // From Core/ALAnalyzer.cs
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: new LocalizableResourceString(nameof(Resources.AL00XX_Description), Resources.ResourceManager, typeof(Resources)),
        helpLinkUri: HelpLinkBase);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    protected override void RegisterActions(AnalysisContext context) {
        // Choose appropriate registration:
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.Whatever);
        // OR
        context.RegisterOperationAction(Analyze, OperationKind.Whatever);
        // OR
        context.RegisterSymbolAction(Analyze, SymbolKind.Whatever);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context) {
        // Report diagnostic:
        context.ReportDiagnostic(Diagnostic.Create(Rule, location, args));
    }
}
```

## Core Utilities

### WellKnownTypeCache

Cache resolved types per compilation for efficient lookups:

```csharp
protected override void RegisterActions(AnalysisContext context) {
    context.RegisterCompilationStartAction(compilationContext => {
        var cache = WellKnownTypeCache.Create(compilationContext.Compilation);

        compilationContext.RegisterOperationAction(ctx => {
            if (cache.IsType(symbol, WellKnownType.IFormCollection)) { ... }
            if (cache.HasAttribute(symbol, WellKnownType.FromFormAttribute)) { ... }
        }, OperationKind.Parameter);
    });
}
```

### OperationHelper

Utility for checking argument exception types (domain-specific):

```csharp
OperationHelper.IsArgumentNullException(type)
OperationHelper.IsArgumentException(type)
OperationHelper.IsArgumentOutOfRangeException(type)
OperationHelper.IsAnyArgumentException(type)
```

### IOperation Extensions (from ANcpLua.Roslyn.Utilities)

For general IOperation utilities, use the extensions from ANcpLua.Roslyn.Utilities:

```csharp
// Unwrap implicit conversions to get actual operand
var unwrapped = operation.UnwrapAllConversions();

// Get human-readable name for diagnostic messages
var name = operation.GetOperandName("fallback");
// Returns: local name, parameter name, property name, field name, or "MethodName()"
```

## Severity Constants

Use `DiagnosticSeverities` for consistent severity naming:

```csharp
DiagnosticSeverities.Suggestion      // Warning - appears in build output
DiagnosticSeverities.RequiredFix     // Error - fails build
DiagnosticSeverities.HiddenByDefault // Info - IDE only, NOT in build output
```

## Key Dependencies

- `Microsoft.CodeAnalysis.CSharp` (5.0.0) - Roslyn APIs
- `ANcpLua.Roslyn.Utilities.Sources` - Compile-time helpers (IsEqualTo, HasAttribute, etc.)
