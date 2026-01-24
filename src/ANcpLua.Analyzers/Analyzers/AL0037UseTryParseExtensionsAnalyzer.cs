using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0037: Suggests using TryParse extension methods instead of verbose patterns.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><c>int.TryParse(s, out var v) ? v : null</c> → <c>s.TryParseInt32()</c></item>
///         <item><c>int.TryParse(s, out var v) ? v : 0</c> → <c>s.TryParseInt32(0)</c></item>
///         <item><c>Guid.TryParse(s, out var v) ? v : default</c> → <c>s.TryParseGuid()</c></item>
///     </list>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0037UseTryParseExtensionsAnalyzer : AlAnalyzer {
    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0037AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0037AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0037AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.UseTryParseExtensions,
        Title, MessageFormat, DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverities.Suggestion, true, Description,
        HelpLinkBase);

    // Mapping from type name to extension method name
    private static readonly Dictionary<string, string> TryParseMapping = new(StringComparer.Ordinal) {
        ["System.Int32"] = "TryParseInt32",
        ["int"] = "TryParseInt32",
        ["System.Int64"] = "TryParseInt64",
        ["long"] = "TryParseInt64",
        ["System.Double"] = "TryParseDouble",
        ["double"] = "TryParseDouble",
        ["System.Decimal"] = "TryParseDecimal",
        ["decimal"] = "TryParseDecimal",
        ["System.Boolean"] = "TryParseBool",
        ["bool"] = "TryParseBool",
        ["System.Guid"] = "TryParseGuid",
        ["System.DateTime"] = "TryParseDateTime",
        ["System.DateTimeOffset"] = "TryParseDateTimeOffset",
        ["System.TimeSpan"] = "TryParseTimeSpan",
        ["System.Byte"] = "TryParseByte",
        ["byte"] = "TryParseByte",
        ["System.Int16"] = "TryParseInt16",
        ["short"] = "TryParseInt16",
        ["System.Single"] = "TryParseSingle",
        ["float"] = "TryParseSingle"
    };

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics.</summary>

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <summary>Registers syntax or operation actions for analysis.</summary>

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterOperationAction(AnalyzeConditional, OperationKind.Conditional);

    private static void AnalyzeConditional(OperationAnalysisContext context) {
        if (context.Operation is not IConditionalOperation conditional) {
            return;
        }

        // Check if condition is a TryParse invocation
        var condition = conditional.Condition;

        // Unwrap parentheses and conversions
        while (condition is IParenthesizedOperation paren) {
            condition = paren.Operand;
        }

        if (condition is not IInvocationOperation invocation) {
            return;
        }

        var method = invocation.TargetMethod;

        // Check if it's a TryParse method
        if (method.Name != "TryParse" || !method.IsStatic || method.Parameters.Length < 2) {
            return;
        }

        // Get the containing type to determine which extension to suggest
        if (method.ContainingType is not { } containingType) {
            return;
        }

        var typeName = containingType.ToDisplayString();
        if (!TryParseMapping.TryGetValue(typeName, out var extensionName)) {
            return;
        }

        // Check if the WhenTrue branch returns the out parameter
        // and the WhenFalse returns null/default
        if (!IsTryParseResultPattern(conditional, invocation)) {
            return;
        }

        // Get the string argument name for the suggestion
        var stringArg = GetStringArgumentName(invocation);
        var suggestion = $"{stringArg}.{extensionName}()";

        context.ReportDiagnostic(Diagnostic.Create(Rule, conditional.Syntax.GetLocation(), suggestion));
    }

    private static bool IsTryParseResultPattern(IConditionalOperation conditional, IInvocationOperation tryParse) {
        // The out parameter should be the second argument
        if (tryParse.Arguments.Length < 2) {
            return false;
        }

        // Get the out argument
        var outArg = tryParse.Arguments[1];
        if (outArg.Parameter?.RefKind != RefKind.Out) {
            return false;
        }

        // The WhenTrue should reference the out variable
        var whenTrue = UnwrapConversions(conditional.WhenTrue);

        // Check if WhenTrue is referencing a local that was declared in the out argument
        if (whenTrue is not ILocalReferenceOperation) {
            return false;
        }

        // The WhenFalse should be null, default, or a constant
        var whenFalse = UnwrapConversions(conditional.WhenFalse);

        return whenFalse switch {
            IDefaultValueOperation => true,
            ILiteralOperation {
                ConstantValue:
                { HasValue: true, Value: null }
            } => true,
            IConversionOperation { Operand: IDefaultValueOperation } => true,
            ILiteralOperation => true, // Any constant (including 0, false, etc.)
            _ => false
        };
    }

    private static IOperation UnwrapConversions(IOperation? operation) {
        while (operation is IConversionOperation conversion) {
            operation = conversion.Operand;
        }

        return operation!;
    }

    private static string GetStringArgumentName(IInvocationOperation invocation) {
        if (invocation.Arguments.Length is 0) {
            return "value";
        }

        var firstArg = invocation.Arguments[0].Value;

        // Unwrap conversions
        while (firstArg is IConversionOperation conversion) {
            firstArg = conversion.Operand;
        }

        return firstArg switch {
            ILocalReferenceOperation local => local.Local.Name,
            IParameterReferenceOperation param => param.Parameter.Name,
            IPropertyReferenceOperation prop => prop.Property.Name,
            IFieldReferenceOperation field => field.Field.Name,
            _ => "value"
        };
    }
}
