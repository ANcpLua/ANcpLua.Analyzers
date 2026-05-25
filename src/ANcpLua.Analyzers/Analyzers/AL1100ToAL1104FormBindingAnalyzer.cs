
namespace ANcpLua.Analyzers.Analyzers;

/// <summary>
///     AL1100-AL1104: Form binding analyzers for ASP.NET Core Minimal APIs.
///     Validates [FromForm], [FromBody], IFormCollection, and IFormFile usage patterns.
/// </summary>
/// <remarks>
///     <para>
///         ASP.NET Core Minimal APIs have specific constraints on form binding that differ
///         from MVC controllers. Violations of these constraints cause runtime exceptions
///         that are difficult to debug. This analyzer catches common mistakes at compile time.
///     </para>
///     <para>
///         AL1100: <c>IFormCollection</c> parameters require explicit <c>[FromForm]</c> attribute.
///         AL1101: Multiple structured form sources (DTOs or form collections with <c>[FromForm]</c>)
///         conflict with each other. AL1102: Mixing <c>IFormCollection</c> and DTO binding is
///         unsupported. AL1103: Abstract types, interfaces, and types without suitable constructors
///         cannot be form-bound. AL1104: <c>[FromForm]</c> and <c>[FromBody]</c> on the same
///         method conflict.
///     </para>
///     <para>
///         The analyzer does not flag primitive parameters with <c>[FromForm]</c> or
///         <c>IFormFile</c>/<c>IFormFileCollection</c> parameters, as these have special
///         handling in the framework.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class Al1100ToAl1104FormBindingAnalyzer : AlAnalyzer {
    /// <summary>The diagnostic identifier for AL1100.</summary>
    private const string DiagnosticIdAl1100 = "AL1100";
    /// <summary>The diagnostic identifier for AL1101.</summary>
    private const string DiagnosticIdAl1101 = "AL1101";
    /// <summary>The diagnostic identifier for AL1102.</summary>
    private const string DiagnosticIdAl1102 = "AL1102";
    /// <summary>The diagnostic identifier for AL1103.</summary>
    private const string DiagnosticIdAl1103 = "AL1103";
    /// <summary>The diagnostic identifier for AL1104.</summary>
    private const string DiagnosticIdAl1104 = "AL1104";

    private static readonly DiagnosticDescriptor s_ruleAl1100 = CreateRule(
        DiagnosticIdAl1100, "AL1100");

    private static readonly DiagnosticDescriptor s_ruleAl1101 = CreateRule(
        DiagnosticIdAl1101, "AL1101");

    private static readonly DiagnosticDescriptor s_ruleAl1102 = CreateRule(
        DiagnosticIdAl1102, "AL1102");

    private static readonly DiagnosticDescriptor s_ruleAl1103 = CreateRule(
        DiagnosticIdAl1103, "AL1103");

    private static readonly DiagnosticDescriptor s_ruleAl1104 = CreateRule(
        DiagnosticIdAl1104, "AL1104");

    /// <summary>Gets the diagnostic descriptors for the supported diagnostics (AL1100-AL1104).</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [s_ruleAl1100, s_ruleAl1101, s_ruleAl1102, s_ruleAl1103, s_ruleAl1104];

    /// <summary>Creates a diagnostic descriptor for form binding rules.</summary>
    private static DiagnosticDescriptor CreateRule(string id, string ruleNumber) => new(
        id,
        new LocalizableResourceString($"{ruleNumber}AnalyzerTitle", Resources.ResourceManager, typeof(Resources)),
        new LocalizableResourceString($"{ruleNumber}AnalyzerMessageFormat", Resources.ResourceManager,
            typeof(Resources)),
        DiagnosticCategories.AspNetCore,
        DiagnosticSeverity.Error,
        true,
        new LocalizableResourceString($"{ruleNumber}AnalyzerDescription", Resources.ResourceManager, typeof(Resources)),
        HelpLink(id));

    /// <summary>Registers method symbol actions to analyze endpoint parameter binding.</summary>
    protected override void RegisterActions(AnalysisContext context) =>
        context.RegisterCompilationStartAction(OnCompilationStart);

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        var cache = new TypeCache<WellKnownType>(
            type => context.Compilation.GetTypeByMetadataName(WellKnownTypeNames.GetName(type)));
        context.RegisterSymbolAction(ctx => AnalyzeMethod(ctx, cache), SymbolKind.Method);
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context, TypeCache<WellKnownType> cache) {
        if (context.Symbol is not IMethodSymbol { Parameters.IsEmpty: false } method) {
            return;
        }

        var hasFromBody = false;
        var hasFromForm = false;
        var fromFormDtoCount = 0;
        var hasFromFormCollection = false;
        var hasFromFormDto = false;

        IParameterSymbol? fromBodyParam = null;
        IParameterSymbol? firstFromFormParam = null;

        foreach (var param in method.Parameters) {
            var hasFromFormAttr = cache.HasAttribute(param, WellKnownType.FromFormAttribute);
            var hasFromBodyAttr = cache.HasAttribute(param, WellKnownType.FromBodyAttribute);
            var isFormCollection = cache.IsTypeDefinition(param.Type, WellKnownType.IFormCollection);
            var isFormFile = cache.IsTypeDefinition(param.Type, WellKnownType.IFormFile);
            var isFormFileCollection = cache.IsTypeDefinition(param.Type, WellKnownType.IFormFileCollection);

            if (hasFromBodyAttr) {
                hasFromBody = true;
                fromBodyParam = param;
            }

            if (isFormCollection && !hasFromFormAttr) {
                context.ReportDiagnostic(Diagnostic.Create(
                    s_ruleAl1100,
                    param.Locations.FirstOrDefault() ?? Location.None,
                    param.Name,
                    method.Name));
            }

            if (hasFromFormAttr) {
                hasFromForm = true;
                firstFromFormParam ??= param;

                if (isFormCollection) {
                    hasFromFormCollection = true;
                    fromFormDtoCount++;
                } else if (!IsPrimitive(param.Type) && !isFormFile && !isFormFileCollection) {
                    hasFromFormDto = true;
                    fromFormDtoCount++;

                    var reason = GetUnsupportedFormTypeReason(param.Type);
                    if (reason is not null) {
                        context.ReportDiagnostic(Diagnostic.Create(
                            s_ruleAl1103,
                            param.Locations.FirstOrDefault() ?? Location.None,
                            param.Name,
                            method.Name,
                            reason));
                    }
                }
            }
        }

        if (hasFromBody && hasFromForm) {
            context.ReportDiagnostic(Diagnostic.Create(
                s_ruleAl1104,
                fromBodyParam?.Locations.FirstOrDefault() ?? method.Locations.FirstOrDefault() ?? Location.None,
                method.Name));
        }

        if (fromFormDtoCount > 1) {
            context.ReportDiagnostic(Diagnostic.Create(
                s_ruleAl1101,
                firstFromFormParam?.Locations.FirstOrDefault() ?? method.Locations.FirstOrDefault() ?? Location.None,
                method.Name));
        }

        if (hasFromFormCollection && hasFromFormDto) {
            context.ReportDiagnostic(Diagnostic.Create(
                s_ruleAl1102,
                firstFromFormParam?.Locations.FirstOrDefault() ?? method.Locations.FirstOrDefault() ?? Location.None,
                method.Name));
        }
    }

    private static bool IsPrimitive(ITypeSymbol type) {
        if (type is INamedTypeSymbol {
            IsGenericType: true, ConstructedFrom.SpecialType: SpecialType.System_Nullable_T
        } namedType) {
            type = namedType.TypeArguments[0];
        }

        return type.SpecialType is
                   SpecialType.System_Boolean or SpecialType.System_Byte or SpecialType.System_SByte or
                   SpecialType.System_Int16 or SpecialType.System_UInt16 or SpecialType.System_Int32 or
                   SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64 or
                   SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal or
                   SpecialType.System_Char or SpecialType.System_String or SpecialType.System_DateTime
               || type.ToDisplayString() is "System.Guid" or "System.TimeSpan" or "System.DateTimeOffset"
                   or "System.DateOnly" or "System.TimeOnly" or "System.Uri";
    }

    private enum WellKnownType {
        FromFormAttribute,
        FromBodyAttribute,
        IFormFile,
        IFormFileCollection,
        IFormCollection,
        SystemGuid,
        TimeSpan,
        DateTimeOffset,
        DateOnly,
        TimeOnly,
        Uri,
        AttributeData,
        TypedConstant
    }

    private static partial class WellKnownTypeNames {
        private static readonly string[] s_names = [
            "Microsoft.AspNetCore.Mvc.FromFormAttribute",
            "Microsoft.AspNetCore.Mvc.FromBodyAttribute",
            "Microsoft.AspNetCore.Http.IFormFile",
            "Microsoft.AspNetCore.Http.IFormFileCollection",
            "Microsoft.AspNetCore.Http.IFormCollection",
            "System.Guid",
            "System.TimeSpan",
            "System.DateTimeOffset",
            "System.DateOnly",
            "System.TimeOnly",
            "System.Uri",
            "Microsoft.CodeAnalysis.AttributeData",
            "Microsoft.CodeAnalysis.TypedConstant"
        ];

        public static string GetName(WellKnownType type) => s_names[(int)type];
    }

    private static string? GetUnsupportedFormTypeReason(ITypeSymbol type) {
        if (type is not INamedTypeSymbol namedType) {
            return "type is not a named type";
        }

        if (namedType.TypeKind == TypeKind.Interface) {
            return "interfaces cannot be form-bound";
        }

        if (namedType.IsAbstract) {
            return "abstract types cannot be form-bound";
        }

        var publicConstructors = namedType.InstanceConstructors
            .Where(static c => c.DeclaredAccessibility == Accessibility.Public)
            .ToList();

        if (publicConstructors.Count is 0) {
            return "no public constructor available";
        }

        var hasValidConstructor = publicConstructors.Exists(static ctor =>
            ctor.Parameters.IsEmpty || ctor.Parameters.All(static p => IsPrimitive(p.Type)));

        return hasValidConstructor ? null : "no suitable constructor (needs parameterless or all-primitive parameters)";
    }
}
