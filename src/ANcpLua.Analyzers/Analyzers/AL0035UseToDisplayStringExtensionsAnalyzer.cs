using ANcpLua.Analyzers.Core;

namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL0035: Suggests using GetFullyQualifiedName()/GetMetadataName() instead of ToDisplayString with format.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>
///             <c>type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)</c> →
///             <c>type.GetFullyQualifiedName()</c>
///         </item>
///         <item>
///             <c>type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)</c> → <c>type.GetMetadataName()</c>
///         </item>
///     </list>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al0035UseToDisplayStringExtensionsAnalyzer : AlAnalyzer {
    private const string ITypeSymbolTypeName = "Microsoft.CodeAnalysis.ITypeSymbol";
    private const string SymbolDisplayFormatTypeName = "Microsoft.CodeAnalysis.SymbolDisplayFormat";

    private static readonly LocalizableResourceString Title = new(
        nameof(Resources.AL0035AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString MessageFormat = new(
        nameof(Resources.AL0035AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableResourceString Description = new(
        nameof(Resources.AL0035AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.UseToDisplayStringExtensions,
        Title, MessageFormat, DiagnosticCategories.RoslynUtilities,
        DiagnosticSeverity.Info, true, Description,
        HelpLinkBase);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        var iTypeSymbol = context.Compilation.GetTypeByMetadataName(ITypeSymbolTypeName);
        var symbolDisplayFormat = context.Compilation.GetTypeByMetadataName(SymbolDisplayFormatTypeName);

        if (iTypeSymbol is null || symbolDisplayFormat is null) {
            return;
        }

        context.RegisterOperationAction(
            ctx => AnalyzeInvocation(ctx, iTypeSymbol, symbolDisplayFormat),
            OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(
        OperationAnalysisContext context,
        INamedTypeSymbol iTypeSymbol,
        INamedTypeSymbol symbolDisplayFormat) {
        if (context.Operation is not IInvocationOperation invocation) {
            return;
        }

        var method = invocation.TargetMethod;

        // Check for ToDisplayString method on ISymbol or derived types
        if (method.Name != "ToDisplayString") {
            return;
        }

        // Must be called on ITypeSymbol or a type that implements it
        var receiverType = GetReceiverType(invocation);
        if (receiverType is null || !IsOrImplementsITypeSymbol(receiverType, iTypeSymbol)) {
            return;
        }

        // Check the format argument
        if (GetFormatArgument(invocation) is not { } formatArg) {
            return;
        }

        // Determine which format is being used
        var (formatName, suggestion) = DetectFormatAndSuggestion(formatArg, symbolDisplayFormat);
        if (formatName is null || suggestion is null) {
            return;
        }

        var receiverName = GetReceiverDisplayName(invocation);
        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.Syntax.GetLocation(),
            $"{receiverName}.{suggestion}", $"ToDisplayString({formatName})"));
    }

    private static ITypeSymbol? GetReceiverType(IInvocationOperation invocation) {
        if (invocation.Instance is not null) {
            return invocation.Instance.Type;
        }

        // Extension method - first argument is the receiver
        if (invocation.TargetMethod.IsExtensionMethod &&
            invocation.Arguments.Length > 0) {
            return invocation.Arguments[0].Value.Type;
        }

        return null;
    }

    private static bool IsOrImplementsITypeSymbol(ITypeSymbol type, INamedTypeSymbol iTypeSymbol) =>
        type.IsEqualTo(iTypeSymbol) || type.Implements(iTypeSymbol) || type.InheritsFrom(iTypeSymbol);

    private static IOperation? GetFormatArgument(IInvocationOperation invocation) {
        // ToDisplayString has optional format parameter
        foreach (var arg in invocation.Arguments) {
            if (arg.Parameter?.Name == "format") {
                return arg.Value;
            }
        }

        // If single argument and it's a SymbolDisplayFormat, that's the format
        if (invocation.Arguments.Length == 1 ||
            invocation.TargetMethod.IsExtensionMethod && invocation.Arguments.Length == 2) {
            var formatArgIndex = invocation.TargetMethod.IsExtensionMethod ? 1 : 0;
            if (invocation.Arguments.Length > formatArgIndex) {
                return invocation.Arguments[formatArgIndex].Value;
            }
        }

        return null;
    }

    private static (string? formatName, string? suggestion) DetectFormatAndSuggestion(
        IOperation formatArg,
        INamedTypeSymbol symbolDisplayFormat) {
        // Unwrap conversions
        while (formatArg is IConversionOperation conversion) {
            formatArg = conversion.Operand;
        }

        // Check for SymbolDisplayFormat.FullyQualifiedFormat or CSharpErrorMessageFormat
        if (formatArg is IPropertyReferenceOperation propRef) {
            if (!propRef.Property.ContainingType.IsEqualTo(symbolDisplayFormat)) {
                return (null, null);
            }

            return propRef.Property.Name switch {
                "FullyQualifiedFormat" => ("FullyQualifiedFormat", "GetFullyQualifiedName()"),
                "CSharpErrorMessageFormat" => ("CSharpErrorMessageFormat", "GetMetadataName()"),
                _ => (null, null)
            };
        }

        // Check for field reference (some versions may expose as field)
        if (formatArg is IFieldReferenceOperation fieldRef) {
            if (!fieldRef.Field.ContainingType.IsEqualTo(symbolDisplayFormat)) {
                return (null, null);
            }

            return fieldRef.Field.Name switch {
                "FullyQualifiedFormat" => ("FullyQualifiedFormat", "GetFullyQualifiedName()"),
                "CSharpErrorMessageFormat" => ("CSharpErrorMessageFormat", "GetMetadataName()"),
                _ => (null, null)
            };
        }

        return (null, null);
    }

    private static string GetReceiverDisplayName(IInvocationOperation invocation) {
        var receiver = invocation.Instance;

        // Extension method - first argument is the receiver
        if (receiver is null && invocation.TargetMethod.IsExtensionMethod && invocation.Arguments.Length > 0) {
            receiver = invocation.Arguments[0].Value;
        }

        if (receiver is null) {
            return "type";
        }

        // Unwrap conversions
        while (receiver is IConversionOperation conversion) {
            receiver = conversion.Operand;
        }

        return receiver switch {
            ILocalReferenceOperation local => local.Local.Name,
            IParameterReferenceOperation param => param.Parameter.Name,
            IPropertyReferenceOperation prop => prop.Property.Name,
            IFieldReferenceOperation field => field.Field.Name,
            _ => "type"
        };
    }
}
